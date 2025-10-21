using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces
{
    public interface ITermoEspecialRepository
    {
        Task<Dictionary<(string Termo, bool TipoValor), TermoEspecial>> BuscarTodosTermosRelevantesAsync(string userId, string cnpj, int? codigoBanco);
        Task AdicionarOuAtualizarEmLoteAsync(IEnumerable<TermoEspecial> termos);
        Task<TermoEspecial?> BuscarPorTermoEUsuarioAsync(string termo, string userId);
        Task<TermoEspecial> AdicionarAsync(TermoEspecial termo);
        Task<TermoEspecial> AtualizarAsync(TermoEspecial termo);
        Task<bool> ExisteTermoAsync(string termo, string userId);
        Task<IEnumerable<TermoEspecial>> ObterTodosPorUsuarioAsync(string userId);
        Task<IEnumerable<int?>> BuscarCodigosBancoPorCnpjAsync(string cnpj, string userId, string? codigoBanco = null);
        Task<TermoEspecial?> BuscarPorTermoUsuarioCnpjEBancoAsync(string termo, string userId, string cnpj, int? codigoBanco);
        Task<IEnumerable<TermoEspecial>> BuscarPorUsuarioCnpjEBancoAsync(string userId, string cnpj, int? codigoBanco);
        Task<TermoEspecial?> BuscarPorTermoUsuarioECnpjAsync(string termo, string userId, string cnpj);
        Task<TermoEspecial?> BuscarPorTermoUsuarioCnpjEBancoETipoAsync(string termo, string userId, string cnpj, int? codigoBanco, bool tipoValor);
        Task<TermoEspecial?> BuscarPorTermoUsuarioECnpjETipoAsync(string termo, string userId, string cnpj, bool tipoValor);
        Task<bool> SalvarAlteracoesAsync();
    }
}