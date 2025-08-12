using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface IImpostoRepository
{
    Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(string userId);
    Task<Imposto?> ObterPorIdAsync(int id, string userId);
    // Task AdicionarAsync(Imposto imposto);
    Task AtualizarAsyncRepository(Imposto imposto);
}
