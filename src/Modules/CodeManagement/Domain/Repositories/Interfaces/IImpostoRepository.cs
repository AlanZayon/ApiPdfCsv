using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface IImpostoRepository
{
    Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(string userId, int? clienteId = null, CancellationToken cancellationToken = default);
    Task<Imposto?> ObterPorIdAsync(int id, string userId, int? clienteId = null, CancellationToken cancellationToken = default);
    Task AtualizarAsyncRepository(Imposto imposto);
    Task CopiarImpostosParaClienteAsync(string userId, int clienteId, IEnumerable<Imposto> impostosPadrao, CancellationToken cancellationToken = default);
}
