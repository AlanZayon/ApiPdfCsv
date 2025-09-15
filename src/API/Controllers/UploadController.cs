using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using System.Security.Claims;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IPdfProcessorService _pdfProcessorService;
    private readonly IFileService _fileService;
    private readonly ProcessPdfUseCase _processPdfUseCase;


    public UploadController(ILogger logger, IPdfProcessorService pdfProcessorService, IFileService fileService, ProcessPdfUseCase processPdfUseCase)
    {
        _logger = logger;
        _pdfProcessorService = pdfProcessorService;
        _fileService = fileService;
        _processPdfUseCase = processPdfUseCase;

    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile pdfFile)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            _logger.Warn("Tentativa de upload sem envio de arquivo.");
            return BadRequest(new { message = "Arquivo não enviado." });
        }

        // Validação de extensão
        var allowedExtensions = new[] { ".pdf" };
        var fileExtension = Path.GetExtension(pdfFile.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            _logger.Warn($"Tentativa de upload com extensão inválida: {fileExtension}");
            return BadRequest(new { message = "Apenas arquivos PDF são permitidos." });
        }

        // Validação de tipo MIME
        var allowedMimeTypes = new[] { "application/pdf" };
        if (!allowedMimeTypes.Contains(pdfFile.ContentType.ToLowerInvariant()))
        {
            _logger.Warn($"Tentativa de upload com tipo MIME inválido: {pdfFile.ContentType}");
            return BadRequest(new { message = "Tipo de arquivo não permitido. Apenas arquivos PDF são aceitos." });
        }

        // Validação de tamanho (50MB máximo)
        const long maxFileSize = 50 * 1024 * 1024; // 50MB
        if (pdfFile.Length > maxFileSize)
        {
            _logger.Warn($"Tentativa de upload de arquivo muito grande: {pdfFile.Length} bytes");
            return BadRequest(new { message = "Arquivo muito grande. O tamanho máximo permitido é 50MB." });
        }

        var filePath = Path.GetTempFileName();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            await using (var stream = System.IO.File.Create(filePath))
            {
                await pdfFile.CopyToAsync(stream);
            }

            _logger.Info($"Iniciando processamento do PDF: {filePath}");

            var command = new ProcessPdfCommand(filePath, userId ?? string.Empty);
            var result = await _processPdfUseCase.Execute(command);


            _logger.Info($"Processamento concluído com sucesso: {result}");
            return Ok(new { result });
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao processar PDF: {ex.Message}", ex);
            _fileService.ClearDirectories();
            return StatusCode(500, new { message = "Erro interno no processamento do PDF. Tente novamente ou entre em contato com o suporte." });
        }
        finally
        {
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                    _logger.Info($"Arquivo temporário deletado: {Path.GetFileName(filePath)}");
                }
                catch (Exception ex)
                {
                    _logger.Error($"Erro ao deletar arquivo temporário: {ex.Message}", ex);
                }
            }
        }
    }
}