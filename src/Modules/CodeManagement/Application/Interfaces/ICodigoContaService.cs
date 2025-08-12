using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;

public interface ICodigoContaService
{
    Task<CodigoConta> ObterPorIdAsync(int id, string userId);
    Task AtualizarAsync(CodigoConta CodigoConta);

}

