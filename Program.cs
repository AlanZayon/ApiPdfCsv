using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.FileProviders;
using ApiPdfCsv.API.Controllers;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Shared.Logging;
using Serilog;
using Serilog.Extensions.Hosting;
using Serilog.AspNetCore;  

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();


builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 104857600;
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ApiPdfCsv.Shared.Logging.ILogger, ApiPdfCsv.Shared.Logging.Logger>();
builder.Services.AddScoped<IPdfProcessorService, PdfProcessorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseHttpsRedirection();
}

app.UseCors("AllowedOrigins");
app.UseAuthorization();

app.MapControllers();

app.MapGet("/teste", () => 
{
    Log.Information("Você acessou /teste");
    return "ok";
});

var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
var uploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "uploads")),
    RequestPath = "/uploads"
});



app.Run();