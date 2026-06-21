using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;


namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/configuracao")]
public class ConfiguracaoController : ControllerBase
{
    private readonly ICodigoContaService _codigoContaService;
    private readonly ITermoEspecialService _termoEspecialService;
    private readonly IImpostoService _impostoService;
    private readonly ILogger _logger;

    public ConfiguracaoController(
        ICodigoContaService codigoContaService,
        ITermoEspecialService termoEspecialService,
        IImpostoService impostoService,
        ILogger logger)
    {
        _codigoContaService = codigoContaService;
        _termoEspecialService = termoEspecialService;
        _impostoService = impostoService;
        _logger = logger;
    }

    #region Impostos
    [HttpGet("impostos")]
    public async Task<IActionResult> ObterImpostos([FromQuery] int? clienteId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var impostos = await _impostoService.ObterTodosAsync(userId ?? string.Empty, clienteId);
        return Ok(impostos);
    }

    [HttpPut("impostos")]
    public async Task<IActionResult> AtualizarImpostos(
        [FromBody] IEnumerable<ImpostoDto> impostos,
        [FromQuery] int? clienteId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var resultado = await _impostoService.AtualizarAsyncService(impostos, userId ?? string.Empty, clienteId);
        return Ok(resultado);
    }
    #endregion

    #region descrições
    [HttpGet("descricoes")]
    public async Task<IActionResult> ObterDescricoes(
        [FromQuery] string cnpj,
        [FromQuery] int? codigoBanco)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        _logger.Info($"CNPJ: {cnpj}, Código do Banco: {codigoBanco}, Usuário: {userId}");

        if (string.IsNullOrEmpty(cnpj))
        {
            return BadRequest("Termo e CNPJ são obrigatórios");
        }

        var descricao = await _termoEspecialService.BuscarPorUsuarioCnpjEBancoAsync(userId ?? string.Empty, cnpj, codigoBanco);

        return Ok(descricao);
    }

    [HttpPut("descricoes")]
    public async Task<IActionResult> AtualizarDescricoes(
        [FromBody] AtualizarTermoEspecialRequest request)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            _logger.Info($"CNPJ: {request.CNPJ}, Código do Banco: {request.CodigoBanco}, Atualizações: {request.Atualizacoes?.Count}, Usuário: {userId}");

            if (string.IsNullOrEmpty(request.CNPJ))
            {
                return BadRequest("CNPJ é obrigatório");
            }

            var resultado = await _termoEspecialService.AtualizarCodigosAsync(
                userId ?? string.Empty,
                request.CNPJ,
                request.CodigoBanco,
                request.Atualizacoes ?? new List<AtualizacaoCodigoDto>());

            if (!resultado.Sucesso)
            {
                return BadRequest(resultado.Mensagem);
            }

            return Ok(new
            {
                Mensagem = "Registros atualizados com sucesso",
                RegistrosAtualizados = resultado.RegistrosAtualizados
            });
        }
        catch (Exception ex)
        {
            _logger.Info(ex + "Erro ao atualizar descrições");
            return StatusCode(500, "Erro interno do servidor");
        }
    }

    [HttpGet("descricoes/export")]
    public async Task<IActionResult> ExportarDescricoes(
        [FromQuery] string cnpj,
        [FromQuery] int? codigoBanco)
    {
        if (string.IsNullOrEmpty(cnpj))
            return BadRequest("CNPJ é obrigatório");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var csv = await _termoEspecialService.ExportarCsvAsync(userId, cnpj, codigoBanco);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "mapeamentos-ofx.csv");
    }

    [HttpPost("descricoes/import")]
    public async Task<IActionResult> ImportarDescricoes(
        [FromQuery] string cnpj,
        [FromQuery] int? codigoBanco,
        IFormFile file)
    {
        if (string.IsNullOrEmpty(cnpj))
            return BadRequest("CNPJ é obrigatório");

        if (file == null || file.Length == 0)
            return BadRequest("Arquivo CSV é obrigatório");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        await using var stream = file.OpenReadStream();
        var count = await _termoEspecialService.ImportarCsvAsync(userId, cnpj, codigoBanco, stream);

        return Ok(new { message = $"{count} mapeamentos importados.", count });
    }

    [HttpGet("descricoes/suggest")]
    public async Task<IActionResult> SugerirDescricoes(
        [FromQuery] string cnpj,
        [FromQuery] string termo)
    {
        if (string.IsNullOrEmpty(cnpj) || string.IsNullOrEmpty(termo))
            return BadRequest("CNPJ e termo são obrigatórios");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var sugestoes = await _termoEspecialService.SugerirAsync(userId, cnpj, termo);
        return Ok(sugestoes);
    }

    [HttpPost("descricoes/copiar")]
    public async Task<IActionResult> CopiarDescricoes([FromBody] CopiarMapeamentosRequest request)
    {
        if (string.IsNullOrEmpty(request.CnpjOrigem) || string.IsNullOrEmpty(request.CnpjDestino))
            return BadRequest("CNPJs de origem e destino são obrigatórios");

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var count = await _termoEspecialService.CopiarMapeamentosAsync(
            userId,
            request.CnpjOrigem,
            request.CnpjDestino,
            request.CodigoBancoOrigem,
            request.CodigoBancoDestino);

        return Ok(new { message = $"{count} mapeamentos copiados.", count });
    }
    #endregion
}