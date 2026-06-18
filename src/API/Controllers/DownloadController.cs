using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiPdfCsv.Shared.Helpers;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IBlobStorageService _blobStorage;

    public DownloadController(ILogger logger, IBlobStorageService blobStorage)
    {
        _logger = logger;
        _blobStorage = blobStorage;
    }

    [HttpGet("download")]
    public async Task<IActionResult> DownloadFile([FromQuery] string? file = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserSessionHelper.GetUserId(User);
            var userSessionId = UserSessionHelper.ResolveSessionId(HttpContext);

            var fileName = await _blobStorage.ResolveOutputFileNameAsync(userId, userSessionId, file, cancellationToken);
            var fileStream = await _blobStorage.OpenReadAsync(userId, userSessionId, fileName, BlobScope.Output, cancellationToken);

            _logger.Info($"Download realizado com sucesso: {fileName} para sessão {userSessionId}");

            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

            Response.OnCompleted(() =>
            {
                SessionCleanupJob.Schedule(userId, userSessionId, TimeSpan.FromMinutes(30));
                _logger.Info($"Limpeza agendada para sessão {userSessionId} em 30 minutos");
                return Task.CompletedTask;
            });

            return File(fileStream, "text/csv", fileName);
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
}
