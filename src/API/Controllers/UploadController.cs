using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
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
    private readonly IFileService _fileService;
    private readonly ProcessPdfUseCase _processPdfUseCase;
    private readonly ProcessOfxUseCase _processOfxUseCase;

    public UploadController(
        ILogger logger,
        IFileService fileService,
        ProcessPdfUseCase processPdfUseCase,
        ProcessOfxUseCase processOfxUseCase
    )
    {
        _logger = logger;
        _fileService = fileService;
        _processPdfUseCase = processPdfUseCase;
        _processOfxUseCase = processOfxUseCase;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.Warn("Tentativa de upload sem envio de arquivo.");
            return BadRequest(new { message = "Arquivo não enviado." });
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var filePath = Path.GetTempFileName();
        var cnpj = Request.Headers["CNPJ"].ToString() ?? string.Empty;
        var codigoBanco = Request.Headers["CodigoBanco"].ToString() ?? string.Empty;
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userSessionId = GetUserSessionId();

        try
        {
            await using (var stream = System.IO.File.Create(filePath))
            {
                await file.CopyToAsync(stream);
            }

            _logger.Info($"Arquivo recebido: {file.FileName}, extensão: {extension}");

            switch (extension)
            {
                case ".pdf":
                    var pdfCommand = new ProcessPdfCommand(filePath, userId, userSessionId);
                    var pdfResult = await _processPdfUseCase.Execute(pdfCommand);
                    _logger.Info($"Processamento PDF concluído com sucesso: {pdfResult}");
                    return Ok(new { type = "pdf", result = pdfResult });

                case ".ofx":
                    var ofxCommand = new ProcessOfxCommand(filePath, cnpj, userId, codigoBanco, userSessionId);
                    var ofxResult = await _processOfxUseCase.Execute(ofxCommand);
                    if (ofxResult.TransacoesPendentes != null && ofxResult.TransacoesPendentes.Any())
                    {
                        return Ok(new
                        {
                            type = "ofx",
                            status = "pending_classification",
                            transacoesClassificadas = ofxResult.TransacoesClassificadas,
                            pendingTransactions = ofxResult.TransacoesPendentes,
                            filePath
                        });
                    }

                    return Ok(new
                    {
                        type = "ofx",
                        status = "completed",
                        outputPath = ofxResult.OutputPath,
                        transacoesClassificadas = ofxResult.TransacoesClassificadas
                    });

                default:
                    _logger.Warn($"Extensão de arquivo não suportada: {extension}");
                    return BadRequest(new { message = "Tipo de arquivo não suportado. Use apenas PDF ou OFX." });
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao processar arquivo {file.FileName}: {ex.Message}", ex);
            return StatusCode(500, new { message = "Erro ao processar arquivo", error = ex.Message });
        }
        finally
        {
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
        }
    }

    [HttpPost("finalizar-processamento")]
    public async Task<IActionResult> FinalizarProcessamento([FromBody] FinalizacaoRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var userSessionId = GetUserSessionId();


        var outputPath = await _processOfxUseCase.FinalizarProcessamento(
            request.TransacoesClassificadas,
            request.Classificacoes,
            request.TransacoesPendentes,
            userId,
            request.CNPJ,
            userSessionId
        );

        return Ok(new
        {
            status = "completed",
            outputPath
        });
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