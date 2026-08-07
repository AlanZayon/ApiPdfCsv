using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;

public interface IPerfilPlanoContasService
{
    Task<IReadOnlyList<PerfilPlanoContasDto>> ListarAsync(string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContasDto?> ObterAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContasDto?> ObterPadraoAsync(string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContasDto> CriarAsync(string userId, CriarPerfilRequest request, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContasDto?> AtualizarAsync(int id, string userId, AtualizarPerfilRequest request, CancellationToken cancellationToken = default);
    Task<bool> RemoverAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task<PerfilPlanoContasDto> GarantirPadraoAsync(string userId, CancellationToken cancellationToken = default);
}
