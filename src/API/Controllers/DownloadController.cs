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
            var fileName = Path.GetFileName(filePath);

            _logger.Info($"Download iniciado: {fileName}");

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

            // Usando PhysicalFile que gerencia o stream automaticamente
            return PhysicalFile(filePath, "application/octet-stream", fileName);
        }
        catch (FileNotFoundException ex)
        {
            _logger.Warn(ex.Message);
            return NotFound(new { message = "Nenhum arquivo disponível para download" });
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao realizar download: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro interno durante o download. Tente novamente ou entre em contato com o suporte." });
        }
    }
}