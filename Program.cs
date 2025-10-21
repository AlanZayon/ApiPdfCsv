using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Serilog;
using DotNetEnv;
using System.Text;

using ApiPdfCsv.API.Controllers;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.OfxProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.CrossCutting.Identity.Configurations;
using ApiPdfCsv.Modules.Authentication.Infrastructure.Services;
using ApiPdfCsv.Modules.Authentication.Application.Services;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Application.Services;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Mappings;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.None);
builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

Env.Load();

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
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

var allowedOrigins = new[]
{
    "http://localhost:5173",
    "https://front-pdf-to-excel.vercel.app",
    "https://admin.meusite.com",
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

builder.Services.AddHttpContextAccessor();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddSingleton<ApiPdfCsv.Shared.Logging.ILogger, ApiPdfCsv.Shared.Logging.Logger>();
builder.Services.AddScoped<IPdfProcessorService, PdfProcessorService>();
builder.Services.Configure<FileServiceOptions>(config =>
{
    config.OutputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
    config.UploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ProcessPdfUseCase>();
builder.Services.AddScoped<IOfxProcessorService, OfxProcessorService>();
builder.Services.AddScoped<ProcessOfxUseCase>();

builder.Services.AddScoped<ICodigoContaRepository, CodigoContaRepository>();
builder.Services.AddScoped<IImpostoRepository, ImpostoRepository>();
builder.Services.AddScoped<ITermoEspecialRepository, TermoEspecialRepository>();
builder.Services.AddScoped<ITermoEspecialService, TermoEspecialService>();
builder.Services.AddScoped<ICodigoContaService, CodigoContaService>();
builder.Services.AddScoped<IImpostoService, ImpostoService>();

var connStr = builder.Configuration.GetConnectionString("DefaultConnection");

if (builder.Environment.IsDevelopment())
{
    Console.WriteLine($"Connection String configured: {!string.IsNullOrEmpty(connStr)}");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connStr, npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()));

builder.Services.AddIdentityConfiguration(builder.Configuration);
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowedOrigins");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () =>
{
    Log.Information("Você acessou /");
    return "ok";
});

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads"
});

app.MapControllers();
app.Run();

public partial class Program { }
