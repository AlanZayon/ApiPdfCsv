using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface IPerfilPlanoContasRepository
{
    Task<IReadOnlyList<PerfilPlanoContas>> ListarPorUsuarioAsync(string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContas?> ObterPorIdAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContas?> ObterPadraoAsync(string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContas> CriarAsync(PerfilPlanoContas perfil, CancellationToken cancellationToken = default);
    Task AtualizarAsync(PerfilPlanoContas perfil, CancellationToken cancellationToken = default);
    Task RemoverAsync(PerfilPlanoContas perfil, CancellationToken cancellationToken = default);
}
