using System.Security.Claims;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiPdfCsv.API.Controllers;

[Authorize]
[ApiController]
[Route("api/clientes")]
public class ClientesController : ControllerBase
{
    private readonly IClienteService _clienteService;

    public ClientesController(IClienteService clienteService)
    {
        _clienteService = clienteService;
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var (items, total) = await _clienteService.ListarAsync(userId, search, page, pageSize, cancellationToken);

        return Ok(new { items, total, page, pageSize });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var cliente = await _clienteService.ObterPorIdAsync(id, userId, cancellationToken);

        if (cliente == null)
            return NotFound(new { message = "Cliente não encontrado." });

        return Ok(cliente);
    }

    [HttpPost]
    public async Task<IActionResult> Criar(
        [FromBody] CreateClienteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var cliente = await _clienteService.CriarAsync(userId, request, cancellationToken);
            return CreatedAtAction(nameof(Obter), new { id = cliente.Id }, cliente);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(
        int id,
        [FromBody] UpdateClienteRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var cliente = await _clienteService.AtualizarAsync(id, userId, request, cancellationToken);

        if (cliente == null)
            return NotFound(new { message = "Cliente não encontrado." });

        return Ok(cliente);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Desativar(int id, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var ok = await _clienteService.DesativarAsync(id, userId, cancellationToken);

        if (!ok)
            return NotFound(new { message = "Cliente não encontrado." });

        return NoContent();
    }
}
