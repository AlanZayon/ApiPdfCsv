using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface IClienteRepository
{
    Task<(IEnumerable<Cliente> Items, int Total)> ListarAsync(
        string userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Cliente?> ObterPorIdAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<Cliente?> ObterPorCnpjAsync(string userId, string cnpj, CancellationToken cancellationToken = default);
    Task<Cliente> CriarAsync(Cliente cliente, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default);
}
