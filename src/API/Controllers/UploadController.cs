using Microsoft.AspNetCore.Mvc;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IPdfProcessorService _pdfProcessorService;

    public UploadController(ILogger logger, IPdfProcessorService pdfProcessorService)
    {
        _logger = logger;
        _pdfProcessorService = pdfProcessorService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            _logger.Warn("Tentativa de upload sem envio de arquivo.");
            return BadRequest(new { message = "Arquivo não enviado." });
        }

        var filePath = Path.GetTempFileName();
        
        try
        {
            await using (var stream = System.IO.File.Create(filePath))
            {
                await pdfFile.CopyToAsync(stream);
            }

            _logger.Info($"Iniciando processamento do PDF: {filePath}");

            var command = new ProcessPdfCommand(filePath);
            var useCase = new ProcessPdfUseCase(_pdfProcessorService, _logger);
            var result = await useCase.Execute(command);

            _logger.Info($"Processamento concluído com sucesso: {result}");
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao processar PDF: {ex.Message}", ex);
            FileService.ClearDirectories();
            return StatusCode(500, new { message = "Erro ao processar PDF", error = ex.Message });
        }
        finally
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }
}