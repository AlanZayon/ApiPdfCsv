using Microsoft.AspNetCore.Mvc;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly ILogger _logger;

    public DownloadController(ILogger logger)
    {
        _logger = logger;
    }

    [HttpGet("download")]
    public IActionResult DownloadFile()
    {
        try
        {
            var filePath = FileService.GetSingleFile();
            var fileStream = System.IO.File.OpenRead(filePath);
            var fileName = Path.GetFileName(filePath);
            
            _logger.Info($"Download realizado com sucesso: {fileName}");
            
            Response.OnCompleted(() =>
            {
                try
                {
                    FileService.ClearDirectories();
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