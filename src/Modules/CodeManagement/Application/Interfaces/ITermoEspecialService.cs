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
        // Task<IEnumerable<TermoEspecialDto>> ObterTodosAsync(string userId);
        // Task<IEnumerable<TermoEspecialDto>> AtualizarAsyncService(IEnumerable<TermoEspecialDto> termoEspecialDtos, string userId);
    }
}