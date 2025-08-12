using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
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
            var filePath = _fileService.GetSingleFile();
            var fileStream = System.IO.File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);

            _logger.Info($"Download realizado com sucesso: {fileName}");

            Response.OnCompleted(() =>
            {
                try
                {
                    _fileService.ClearDirectories();
                    _logger.Info("Diretórios limpos com sucesso após download");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao limpar diretórios: {ex.Message}", ex);
                }
                return Task.CompletedTask;
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
}