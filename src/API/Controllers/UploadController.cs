using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Shared.Helpers;
using ApiPdfCsv.Shared.Validation;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using Hangfire;
using System.Text.Json;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUploadJobService _uploadJobService;

    public UploadController(
        ILogger logger,
        IBlobStorageService blobStorage,
        IUploadJobService uploadJobService)
    {
        _logger = logger;
        _blobStorage = blobStorage;
        _uploadJobService = uploadJobService;
    }

    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetJobStatus(string jobId, CancellationToken cancellationToken)
    {
        var userId = UserSessionHelper.GetUserId(User);
        var status = await _uploadJobService.GetStatusAsync(jobId, userId, cancellationToken);

        if (status == null)
        {
            return NotFound(new { message = "Job não encontrado." });
        }

        if (status.State == UploadJobState.Completed && status.Result.HasValue)
        {
            return Ok(new
            {
                status.JobId,
                state = status.State.ToString(),
                status.Message,
                status.CreatedAtUtc,
                status.CompletedAtUtc,
                status.OutputFile,
                result = status.Result.Value
            });
        }

        return Ok(new
        {
            status.JobId,
            state = status.State.ToString(),
            status.Message,
            status.CreatedAtUtc,
            status.CompletedAtUtc,
            status.OutputFile
        });
    }

    [HttpPost("upload")]
    [EnableRateLimiting("upload")]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            _logger.Warn("Tentativa de upload sem envio de arquivo.");
            return BadRequest(new { message = "Arquivo não enviado." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var cnpj = Request.Headers["CNPJ"].ToString() ?? string.Empty;
        var codigoBanco = Request.Headers["CodigoBanco"].ToString() ?? string.Empty;
        var proLaboreAno = Request.Headers["ProLabore-Ano"].ToString();
        var proLaboreValor = Request.Headers["ProLabore-Valor"].ToString();

        var userId = UserSessionHelper.GetUserId(User);
        var userSessionId = UserSessionHelper.ResolveSessionId(HttpContext);

        try
        {
            UploadFileValidator.ValidateFile(file, extension);

            if (extension == ".ofx")
            {
                UploadFileValidator.ValidateCnpj(cnpj);
            }

            if (extension is not (".pdf" or ".ofx"))
            {
                _logger.Warn($"Extensão de arquivo não suportada: {extension}");
                return BadRequest(new { message = "Tipo de arquivo não suportado. Use apenas PDF ou OFX." });
            }

            var inputFileName = $"input{extension}";
            await using (var uploadStream = file.OpenReadStream())
            {
                await _blobStorage.SaveAsync(
                    userId,
                    userSessionId,
                    inputFileName,
                    uploadStream,
                    BlobScope.Upload,
                    cancellationToken);
            }

            await using (var validationStream = await _blobStorage.OpenReadAsync(
                userId, userSessionId, inputFileName, BlobScope.Upload, cancellationToken))
            {
                var tempPath = Path.GetTempFileName();
                try
                {
                    await using (var tempStream = System.IO.File.Create(tempPath))
                    {
                        await validationStream.CopyToAsync(tempStream, cancellationToken);
                    }

                    await UploadFileValidator.ValidateContentAsync(tempPath, extension);
                }
                finally
                {
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }

            var metadata = new UploadJobMetadata
            {
                Cnpj = cnpj,
                CodigoBanco = codigoBanco,
                ProLaboreAno = int.TryParse(proLaboreAno, out var ano) ? ano : null,
                ProLaboreValor = decimal.TryParse(proLaboreValor, out var valor) ? valor : null
            };

            var jobId = await _uploadJobService.CreateJobAsync(userId, new CreateUploadJobRequest(
                userSessionId,
                "upload",
                extension.TrimStart('.'),
                inputFileName,
                JsonSerializer.Serialize(metadata)), cancellationToken);

            BackgroundJob.Enqueue<UploadProcessingJob>(job => job.ProcessUploadAsync(jobId, CancellationToken.None));

            _logger.Info($"Arquivo enfileirado: {file.FileName}, jobId: {jobId}");

            return Accepted(new
            {
                jobId,
                state = UploadJobState.Processing.ToString(),
                message = "Arquivo recebido e enfileirado para processamento."
            });
        }
        catch (InvalidDataException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao enfileirar arquivo {file.FileName}: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao processar arquivo." });
        }
    }

    [HttpPost("finalizar-processamento")]
    public async Task<IActionResult> FinalizarProcessamento([FromBody] FinalizacaoRequest request, CancellationToken cancellationToken)
    {
        var userId = UserSessionHelper.GetUserId(User);
        var userSessionId = UserSessionHelper.ResolveSessionId(HttpContext);

        var jobId = await _uploadJobService.CreateJobAsync(userId, new CreateUploadJobRequest(
            userSessionId,
            "finalize",
            "ofx",
            null,
            JsonSerializer.Serialize(request)), cancellationToken);

        BackgroundJob.Enqueue<UploadProcessingJob>(job => job.ProcessFinalizeAsync(jobId, CancellationToken.None));

        return Accepted(new
        {
            jobId,
            state = UploadJobState.Processing.ToString(),
            message = "Finalização enfileirada para processamento."
        });
    }
}
