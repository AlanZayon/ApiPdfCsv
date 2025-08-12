using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces
{
    public interface IImpostoService
    {
        Task<ImpostoDto?> ObterPorIdAsync(int id, string userId);
        Task<IEnumerable<ImpostoDto>> ObterTodosAsync(string userId);
        // Task<ImpostoDto> AdicionarAsync(ImpostoDto dto, string userId);
        Task<IEnumerable<ImpostoDto>> AtualizarAsyncService(IEnumerable<ImpostoDto> impostos, string userId);
        // Task<bool> RemoverAsync(int id, string userId);
        Task<List<decimal>> MapearDebito(List<string> historico, string userId);
        Task<List<decimal>> MapearCredito(List<string> historico, string userId);


    }
}