using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface ITermoEspecialRepository
{
    Task<IEnumerable<TermoEspecial>> ObterTodosPorUsuarioAsync(string userId);
    Task<TermoEspecial> ObterPorIdAsync(string id, string userId);
    Task AdicionarAsync(TermoEspecial termoEspecial);
    Task AtualizarAsync(TermoEspecial termoEspecial);
    Task RemoverAsync(string id, string userId);
}