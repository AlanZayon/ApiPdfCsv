using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface IImpostoRepository
{
    Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(string userId, int? perfilId = null, CancellationToken cancellationToken = default);
    Task<Imposto?> ObterPorIdAsync(int id, string userId, int? perfilId = null, CancellationToken cancellationToken = default);
    Task AtualizarAsyncRepository(Imposto imposto);
    Task CopiarImpostosParaPerfilAsync(string userId, int perfilId, IEnumerable<Imposto> impostosOrigem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Imposto>> ObterLegadosSemPerfilAsync(string userId, CancellationToken cancellationToken = default);
}
