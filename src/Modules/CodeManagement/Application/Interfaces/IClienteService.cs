using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;

public interface IClienteService
{
    Task<(IEnumerable<ClienteDto> Items, int Total)> ListarAsync(
        string userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ClienteDto?> ObterPorIdAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<ClienteDto> CriarAsync(string userId, CreateClienteRequest request, CancellationToken cancellationToken = default);
    Task<ClienteDto?> AtualizarAsync(int id, string userId, UpdateClienteRequest request, CancellationToken cancellationToken = default);
    Task<bool> DesativarAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<(string Cnpj, int? CodigoBanco, string? RazaoSocial)?> ResolverParaUploadAsync(
        string userId,
        int? clienteId,
        string? cnpjHeader,
        string? codigoBancoHeader,
        CancellationToken cancellationToken = default);
}
