using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

public interface ICodigoContaRepository
{
    Task<CodigoConta?> ObterPorIdAsync(int id, string userId);
    Task AtualizarAsync(CodigoConta codigoConta);
}