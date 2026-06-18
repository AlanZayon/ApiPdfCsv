using Amazon;
using Amazon.S3;
using ApiPdfCsv.Modules.Authentication.Application.Services;
using ApiPdfCsv.Modules.Authentication.Infrastructure.Services;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Application.Mappings;
using ApiPdfCsv.Modules.CodeManagement.Application.Services;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.OfxProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using IAppLogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfProcessingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FileServiceOptions>(options =>
        {
            options.OutputDir = ResolveStoragePath(
                configuration["Storage:Local:OutputDir"],
                "outputs");
            options.UploadDir = ResolveStoragePath(
                configuration["Storage:Local:UploadDir"],
                "uploads");
        });

        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IFileService, FileService>();
        AddBlobStorage(services, configuration);
        services.AddScoped<IPdfProcessorService, PdfProcessorService>();
        services.AddScoped<ProcessPdfUseCase>();

        return services;
    }

    private static string ResolveStoragePath(string? configuredPath, string defaultFolder)
    {
        return string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(Directory.GetCurrentDirectory(), defaultFolder)
            : configuredPath;
    }

    private static void AddBlobStorage(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(StorageOptions.SectionName)["Provider"] ?? "Local";

        if (string.Equals(provider, "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(sp =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<StorageOptions>>().Value;
                var region = RegionEndpoint.GetBySystemName(options.S3.Region);
                return new AmazonS3Client(region);
            });
            services.AddSingleton<IBlobStorageService, S3BlobStorageService>();
        }
        else
        {
            services.AddSingleton<IBlobStorageService, LocalBlobStorageService>();
        }
    }

    public static IServiceCollection AddOfxProcessingModule(this IServiceCollection services)
    {
        services.AddScoped<IOfxProcessorService, OfxProcessorService>();
        services.AddScoped<ProcessOfxUseCase>();
        return services;
    }

    public static IServiceCollection AddCodeManagementModule(this IServiceCollection services)
    {
        services.AddScoped<ICodigoContaRepository, CodigoContaRepository>();
        services.AddScoped<IImpostoRepository, ImpostoRepository>();
        services.AddScoped<ITermoEspecialRepository, TermoEspecialRepository>();
        services.AddScoped<ITermoEspecialService, TermoEspecialService>();
        services.AddScoped<ICodigoContaService, CodigoContaService>();
        services.AddScoped<IImpostoService, ImpostoService>();
        return services;
    }

    public static IServiceCollection AddAuthenticationModule(this IServiceCollection services)
    {
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddSharedInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAppLogger, Logger>();
        services.AddScoped<IUploadJobService, DbUploadJobService>();
        services.AddScoped<UploadProcessingJob>();
        services.AddScoped<SessionCleanupJob>();
        services.AddAutoMapper(typeof(MappingProfile));
        return services;
    }
}
