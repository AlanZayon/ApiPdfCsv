using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using ApiPdfCsv.Shared.Helpers;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;
using System.Linq;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IBlobStorageService _blobStorage;
    private readonly IUploadJobService _uploadJobService;
    private readonly StorageOptions _storageOptions;

    public DownloadController(
        ILogger logger,
        IBlobStorageService blobStorage,
        IUploadJobService uploadJobService,
        IOptions<StorageOptions> storageOptions)
    {
        _logger = logger;
        _blobStorage = blobStorage;
        _uploadJobService = uploadJobService;
        _storageOptions = storageOptions.Value;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string? file = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserSessionHelper.GetUserId(User);
            var userSessionId = UserSessionHelper.ResolveSessionId(HttpContext);

            return await StreamDownloadAsync(userId, userSessionId, file, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.Warn(ex.Message);
            return NotFound(new { message = "Nenhum arquivo disponível para download" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao realizar download: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao realizar download." });
        }
    }

    [HttpGet("{jobId}")]
    public async Task<IActionResult> DownloadByJobId(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserSessionHelper.GetUserId(User);
            var job = await _uploadJobService.GetJobForDownloadAsync(jobId, userId, cancellationToken);

            if (job == null || string.IsNullOrWhiteSpace(job.OutputFile))
                return NotFound(new { message = "Arquivo não encontrado para este job." });

            return await StreamDownloadAsync(userId, job.SessionId, job.OutputFile, cancellationToken);
        }
        catch (FileNotFoundException ex)
        {
            _logger.Warn(ex.Message);
            return NotFound(new { message = "Nenhum arquivo disponível para download" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao realizar download por job: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao realizar download." });
        }
    }

    public class MergeDownloadRequest
    {
        public List<string> JobIds { get; set; } = new();
    }

    [HttpPost("merge")]
    public async Task<IActionResult> MergeDownloads(
        [FromBody] MergeDownloadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (request.JobIds == null || request.JobIds.Count == 0)
                return BadRequest(new { message = "Informe ao menos um jobId." });

            var userId = UserSessionHelper.GetUserId(User);
            var lines = new List<(DateTime? Date, string Line)>();

            foreach (var jobId in request.JobIds.Distinct())
            {
                var job = await _uploadJobService.GetJobForDownloadAsync(jobId, userId, cancellationToken);
                if (job == null || string.IsNullOrWhiteSpace(job.OutputFile))
                    continue;

                await using var stream = await _blobStorage.OpenReadAsync(
                    userId, job.SessionId, job.OutputFile, BlobScope.Output, cancellationToken);
                using var reader = new StreamReader(stream);
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync(cancellationToken);
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    lines.Add((TryParseDominioDate(line), line.TrimEnd()));
                }
            }

            if (lines.Count == 0)
                return NotFound(new { message = "Nenhum lançamento encontrado para mesclar." });

            var ordered = lines
                .OrderBy(l => l.Date ?? DateTime.MaxValue)
                .ThenBy(l => l.Line)
                .Select(l => l.Line);

            var content = string.Join("\n", ordered) + "\n";
            var bytes = System.Text.Encoding.UTF8.GetPreamble()
                .Concat(System.Text.Encoding.UTF8.GetBytes(content))
                .ToArray();

            _logger.Info($"Merge de {request.JobIds.Count} jobs gerou {lines.Count} linhas para usuário {userId}");
            return File(bytes, "text/csv", "LANCAMENTOS.csv");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao mesclar downloads: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao mesclar arquivos." });
        }
    }

    private static DateTime? TryParseDominioDate(string line)
    {
        var parts = line.Split(',');
        if (parts.Length < 2) return null;
        var raw = parts[1].Trim().Trim('"');
        if (raw.Length == 8 && raw.All(char.IsDigit)
            && DateTime.TryParseExact(raw, "ddMMyyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var digitos))
            return digitos;
        if (DateTime.TryParseExact(raw, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var br))
            return br;
        return null;
    }

    private async Task<IActionResult> StreamDownloadAsync(
        string userId,
        string sessionId,
        string? file,
        CancellationToken cancellationToken)
    {
        var fileName = await _blobStorage.ResolveOutputFileNameAsync(userId, sessionId, file, cancellationToken);
        var fileStream = await _blobStorage.OpenReadAsync(userId, sessionId, fileName, BlobScope.Output, cancellationToken);

        _logger.Info($"Download realizado com sucesso: {fileName} para sessão {sessionId}");

        Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

        var retention = TimeSpan.FromDays(Math.Max(1, _storageOptions.RetentionDays));
        Response.OnCompleted(() =>
        {
            SessionCleanupJob.Schedule(userId, sessionId, retention);
            _logger.Info($"Limpeza agendada para sessão {sessionId} em {retention.TotalDays} dias");
            return Task.CompletedTask;
        });

        return File(fileStream, "text/csv", fileName);
    }
}
