using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using DotNetEnv;
using System.Text;
using System.Threading.RateLimiting;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.CrossCutting.Identity.Configurations;
using ApiPdfCsv.Shared.Extensions;
using ApiPdfCsv.Shared.Middleware;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using Hangfire;
using Hangfire.PostgreSql;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

Env.Load();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: false)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104_857_600;
});

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[]
    {
        "http://localhost:5173",
        "https://front-pdf-to-excel.vercel.app",
        "https://pdftoexcel.netlify.app",
    };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0
            }));
    options.AddPolicy("upload", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.User?.Identity?.Name
                ?? context.Connection.RemoteIpAddress?.ToString()
                ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,
                QueueLimit = 0
            }));
});

builder.Services.AddMemoryCache();

var redisConnection = builder.Configuration["Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
    });
}

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSharedInfrastructure();
builder.Services.AddPdfProcessingModule(builder.Configuration);
builder.Services.AddOfxProcessingModule();
builder.Services.AddCodeManagementModule();
builder.Services.AddAuthenticationModule();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");
var storageProvider = builder.Configuration.GetSection(StorageOptions.SectionName)["Provider"] ?? "Local";

if (!string.IsNullOrWhiteSpace(connStr))
{
    builder.Services.AddHangfire(config =>
        config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connStr)));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", Environment.ProcessorCount);
    });
}

var healthChecksBuilder = builder.Services.AddHealthChecks();

if (string.Equals(storageProvider, "S3", StringComparison.OrdinalIgnoreCase))
{
    healthChecksBuilder.AddCheck<S3StorageHealthCheck>("s3_storage", tags: new[] { "ready" });
}
else
{
    healthChecksBuilder.AddCheck("disk_outputs", () =>
    {
        var dir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
        if (!Directory.Exists(dir))
        {
            return Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Unhealthy("outputs directory missing");
        }

        var drive = new DriveInfo(Path.GetPathRoot(dir) ?? dir);
        var freeGb = drive.AvailableFreeSpace / (1024d * 1024 * 1024);
        return freeGb < 1
            ? Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Degraded($"Low disk space: {freeGb:F1} GB")
            : Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy();
    }, tags: new[] { "ready" });
}

if (!builder.Environment.IsEnvironment("Testing") && !string.IsNullOrWhiteSpace(connStr))
{
    healthChecksBuilder.AddNpgSql(connStr, name: "postgresql", tags: new[] { "ready" });
}

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"Connection String configured: {!string.IsNullOrEmpty(connStr)}");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddIdentityConfiguration(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment() && !string.IsNullOrEmpty(connStr))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHangfireDashboard("/hangfire");
}

app.UseHttpsRedirection();
app.UseCors("AllowedOrigins");
app.UseRateLimiter();
app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
{
    Log.Information("Você acessou /");
    return "ok";
});

app.MapHealthChecks("/health");
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

app.MapControllers();

var storageOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
if (!string.IsNullOrWhiteSpace(connStr))
{
    JobsRetentionJob.RegisterRecurring(Math.Max(1, storageOptions.RetentionDays));
}

app.Run();

public partial class Program { }
