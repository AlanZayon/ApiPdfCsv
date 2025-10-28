using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using System.Security.Claims;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IFileService _fileService;

    public DownloadController(ILogger logger, IFileService fileService)
    {
        _logger = logger;
        _fileService = fileService;
    }

    [HttpGet("download")]
    public IActionResult DownloadFile()
    {
        try
        {
            var userSessionId = GetUserSessionId();

            var filePath = _fileService.GetUserFile(userSessionId);
            var fileStream = System.IO.File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);

            _logger.Info($"Download realizado com sucesso: {fileName} para sessão {userSessionId}");

            Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");

            Response.OnCompleted(async () =>
            {
                try
                {
                    await _fileService.ScheduleCleanup(userSessionId, TimeSpan.FromMinutes(30));
                    _logger.Info($"Limpeza agendada para sessão {userSessionId} em 30 minutos");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao agendar limpeza da sessão {userSessionId}: {ex.Message}", ex);
                }
            });


            return File(fileStream, "application/octet-stream", fileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.Warn(ex.Message);
            return NotFound(new { message = "Nenhum arquivo disponível para download" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao realizar download: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao realizar download", error = ex.Message });
        }
    }

    private string GetUserSessionId()
    {
        var sessionId = Request.Headers["X-User-Session"].FirstOrDefault()
                     ?? Request.Query["sessionId"].FirstOrDefault();

        if (string.IsNullOrEmpty(sessionId))
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "anonymous";
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var guid = Guid.NewGuid().ToString("N").Substring(0, 8);

            sessionId = $"{userId}_{guid}_{timestamp}";
        }

        return sessionId;
    }
}