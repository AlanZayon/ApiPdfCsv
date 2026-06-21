using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Responses;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces
{
    public interface ITermoEspecialService
    {
        Task<IEnumerable<TermoEspecialDto>> BuscarPorUsuarioCnpjEBancoAsync(string userId, string cnpj, int? codigoBanco);
        Task<ResultadoAtualizacao> AtualizarCodigosAsync(
            string userId,
            string cnpj,
            int? codigoBanco,
            List<AtualizacaoCodigoDto> atualizacoes);
        Task<string> ExportarCsvAsync(string userId, string cnpj, int? codigoBanco);
        Task<int> ImportarCsvAsync(string userId, string cnpj, int? codigoBanco, Stream csvStream);
        Task<IEnumerable<TermoEspecialDto>> SugerirAsync(string userId, string cnpj, string termo);
        Task<int> CopiarMapeamentosAsync(
            string userId,
            string cnpjOrigem,
            string cnpjDestino,
            int? codigoBancoOrigem,
            int? codigoBancoDestino);
    }
}
