using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces
{
    public interface IImpostoService
    {
        Task<ImpostoDto?> ObterPorIdAsync(int id, string userId, int? clienteId = null);
        Task<IEnumerable<ImpostoDto>> ObterTodosAsync(string userId, int? clienteId = null);
        Task<IEnumerable<ImpostoDto>> AtualizarAsyncService(IEnumerable<ImpostoDto> impostos, string userId, int? clienteId = null);
        Task<List<decimal>> MapearDebito(List<string> historico, string userId, int? clienteId = null);
        Task<List<decimal>> MapearCredito(List<string> historico, string userId, int? clienteId = null);
        Task<(List<decimal> Debitos, List<decimal> Creditos)> MapearDebitoECredito(
            List<string> historico,
            string userId,
            int? clienteId = null);
    }
}
