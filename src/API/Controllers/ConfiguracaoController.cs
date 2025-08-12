using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;


namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/configuracao")]
public class ConfiguracaoController : ControllerBase
{
    private readonly ICodigoContaService _codigoContaService;
    // private readonly ITermoEspecialService _termoEspecialService;
    private readonly IImpostoService _impostoService;
    private readonly ILogger _logger;

    public ConfiguracaoController(
        ICodigoContaService codigoContaService,
        // ITermoEspecialService termoEspecialService,
        IImpostoService impostoService,
        ILogger logger)
    {
        _codigoContaService = codigoContaService;
        // _termoEspecialService = termoEspecialService;
        _impostoService = impostoService;
        _logger = logger;
    }

    #region Impostos
    [HttpGet("impostos")]
    public async Task<IActionResult> ObterImpostos()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var impostos = await _impostoService.ObterTodosAsync(userId ?? string.Empty);
        return Ok(impostos);
    }

    [HttpPut("impostos")]
    public async Task<IActionResult> AtualizarImpostos([FromBody] IEnumerable<ImpostoDto> impostos)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var resultado = await _impostoService.AtualizarAsyncService(impostos, userId ?? string.Empty);
        return Ok(resultado);
    }
    #endregion
}