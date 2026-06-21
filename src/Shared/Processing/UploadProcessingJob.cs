using System.Text.Json;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Storage;
using Hangfire;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Shared.Processing;

public class UploadProcessingJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public UploadProcessingJob(IServiceScopeFactory scopeFactory, ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessUploadAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IUploadJobService>();
        var blobStorage = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();
        var processPdfUseCase = scope.ServiceProvider.GetRequiredService<ProcessPdfUseCase>();
        var processOfxUseCase = scope.ServiceProvider.GetRequiredService<ProcessOfxUseCase>();

        var job = await jobService.GetJobEntityAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.Warn($"Upload job not found: {jobId}");
            return;
        }

        await jobService.MarkProcessingAsync(jobId, cancellationToken);

        var tempPath = Path.GetTempFileName();
        try
        {
            if (string.IsNullOrWhiteSpace(job.InputFileName))
            {
                throw new InvalidOperationException("Input file name is missing for upload job.");
            }

            await using (var inputStream = await blobStorage.OpenReadAsync(
                job.UserId,
                job.SessionId,
                job.InputFileName,
                BlobScope.Upload,
                cancellationToken))
            await using (var tempStream = System.IO.File.Create(tempPath))
            {
                await inputStream.CopyToAsync(tempStream, cancellationToken);
            }

            var metadata = DeserializeMetadata(job.MetadataJson);

            if (job.FileType == "pdf")
            {
                var command = new ProcessPdfCommand(
                    tempPath,
                    job.UserId,
                    job.SessionId,
                    metadata?.ProLaboreAno?.ToString(),
                    metadata?.ProLaboreValor?.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    metadata?.ClienteId);
                var result = await processPdfUseCase.Execute(command);

                var resultJson = JsonSerializer.Serialize(new
                {
                    type = "pdf",
                    result = new { message = result.Message, outputFile = result.OutputFile }
                }, JsonOptions);

                await jobService.MarkCompletedAsync(jobId, new UploadJobCompletion(
                    "PDF processado com sucesso",
                    result.OutputFile,
                    resultJson), cancellationToken);
            }
            else if (job.FileType == "ofx")
            {
                var command = new ProcessOfxCommand(
                    tempPath,
                    metadata?.Cnpj ?? string.Empty,
                    job.UserId,
                    metadata?.CodigoBanco,
                    job.SessionId);
                var result = await processOfxUseCase.Execute(command);

                if (result.TransacoesPendentes != null && result.TransacoesPendentes.Any())
                {
                    var resultJson = JsonSerializer.Serialize(new
                    {
                        type = "ofx",
                        status = "pending_classification",
                        transacoesClassificadas = result.TransacoesClassificadas,
                        pendingTransactions = result.TransacoesPendentes
                    }, JsonOptions);

                    await jobService.MarkCompletedAsync(jobId, new UploadJobCompletion(
                        "Classificação pendente",
                        null,
                        resultJson), cancellationToken);
                }
                else
                {
                    var resultJson = JsonSerializer.Serialize(new
                    {
                        type = "ofx",
                        status = "completed",
                        outputFile = result.OutputFile,
                        transacoesClassificadas = result.TransacoesClassificadas
                    }, JsonOptions);

                    await jobService.MarkCompletedAsync(jobId, new UploadJobCompletion(
                        "OFX processado com sucesso",
                        result.OutputFile,
                        resultJson), cancellationToken);
                }
            }
            else
            {
                throw new InvalidOperationException($"Unsupported file type: {job.FileType}");
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to process upload job {jobId}: {ex.Message}", ex);
            await jobService.MarkFailedAsync(jobId, ex.Message, cancellationToken);
            throw;
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task ProcessFinalizeAsync(string jobId, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IUploadJobService>();
        var processOfxUseCase = scope.ServiceProvider.GetRequiredService<ProcessOfxUseCase>();

        var job = await jobService.GetJobEntityAsync(jobId, cancellationToken);
        if (job == null)
        {
            _logger.Warn($"Finalize job not found: {jobId}");
            return;
        }

        await jobService.MarkProcessingAsync(jobId, cancellationToken);

        try
        {
            var request = JsonSerializer.Deserialize<FinalizacaoRequest>(job.MetadataJson ?? "{}", JsonOptions)
                ?? throw new InvalidOperationException("Finalize metadata is missing.");

            var result = await processOfxUseCase.FinalizarProcessamento(
                request.TransacoesClassificadas,
                request.Classificacoes,
                request.TransacoesPendentes,
                job.UserId,
                request.CNPJ,
                job.SessionId);

            var resultJson = JsonSerializer.Serialize(new
            {
                type = "ofx",
                status = "completed",
                outputFile = result.OutputFile
            }, JsonOptions);

            await jobService.MarkCompletedAsync(jobId, new UploadJobCompletion(
                "Processamento finalizado com sucesso",
                result.OutputFile,
                resultJson), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to finalize job {jobId}: {ex.Message}", ex);
            await jobService.MarkFailedAsync(jobId, ex.Message, cancellationToken);
            throw;
        }
    }

    private static UploadJobMetadata? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson)) return null;
        return JsonSerializer.Deserialize<UploadJobMetadata>(metadataJson, JsonOptions);
    }
}

public class UploadJobMetadata
{
    public string? Cnpj { get; set; }
    public string? CodigoBanco { get; set; }
    public int? ClienteId { get; set; }
    public string? ClienteNome { get; set; }
    public string? InputOriginalFileName { get; set; }
    public int? ProLaboreAno { get; set; }
    public decimal? ProLaboreValor { get; set; }
}
