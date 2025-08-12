using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;


namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;

public class ImpostoRepository : IImpostoRepository
{
    private readonly AppDbContext _context;
    
    private readonly ILogger _logger;

    public ImpostoRepository(AppDbContext context , ILogger logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(string userId)
    {
        return await _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.UserId == userId)
            .ToListAsync();
    }

    public async Task<Imposto?> ObterPorIdAsync(int id, string userId)
    {
        return await _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
    }

    // public async Task CriarImpostoComCodigosAsync(ImpostoDto dto, string userId)
    // {
    //     var codigoDebito = new CodigoConta
    //     {
    //         Nome = $"{dto.Nome} Débito",
    //         Codigo = dto.CodigoDebito,
    //         Tipo = "debito",
    //         UserId = userId
    //     };

    //     var codigoCredito = new CodigoConta
    //     {
    //         Nome = $"{dto.Nome} Crédito",
    //         Codigo = dto.CodigoCredito,
    //         Tipo = "credito",
    //         UserId = userId
    //     };

    //     await _codigoContaRepository.AdicionarAsync(codigoDebito);
    //     await _codigoContaRepository.AdicionarAsync(codigoCredito);

    //     var imposto = new Imposto
    //     {
    //         Nome = dto.Nome,
    //         UserId = userId,
    //         CodigoDebitoId = codigoDebito.Id,
    //         CodigoCreditoId = codigoCredito.Id
    //     };

    //     await _impostoRepository.AdicionarAsync(imposto);
    // }

    public async Task AtualizarAsyncRepository(Imposto imposto)
    {
        _context.Imposto.Update(imposto);
        await _context.SaveChangesAsync();
    }
}
