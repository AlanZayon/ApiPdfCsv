using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces
{
    public interface ITermoEspecialRepository
    {
        Task<TermoEspecial?> BuscarPorTermoEUsuarioAsync(string termo, string userId);
        Task<TermoEspecial> AdicionarAsync(TermoEspecial termo);
        Task<bool> ExisteTermoAsync(string termo, string userId);
        Task<IEnumerable<TermoEspecial>> ObterTodosPorUsuarioAsync(string userId);
        Task<IEnumerable<int?>> BuscarCodigosBancoPorCnpjAsync(string cnpj, string userId);

    }
}