using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;

public class ImpostoRepository : IImpostoRepository
{
    private readonly AppDbContext _context;
    private readonly ILogger _logger;

    public ImpostoRepository(AppDbContext context, ILogger logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(
        string userId,
        int? perfilId = null,
        CancellationToken cancellationToken = default)
    {
        if (perfilId.HasValue)
        {
            var especificos = await _context.Imposto
                .Include(i => i.CodigoDebito)
                .Include(i => i.CodigoCredito)
                .Where(i => i.UserId == userId && i.PerfilId == perfilId)
                .ToListAsync(cancellationToken);

            if (especificos.Count > 0)
                return especificos;
        }

        // Fallback: impostos do perfil padrão do usuário, senão legado sem perfil.
        var perfilPadraoId = await _context.PerfisPlanoContas
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Id)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (perfilPadraoId.HasValue)
        {
            var doPadrao = await _context.Imposto
                .Include(i => i.CodigoDebito)
                .Include(i => i.CodigoCredito)
                .Where(i => i.UserId == userId && i.PerfilId == perfilPadraoId)
                .ToListAsync(cancellationToken);

            if (doPadrao.Count > 0)
                return doPadrao;
        }

        return await _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.UserId == userId && i.PerfilId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Imposto?> ObterPorIdAsync(
        int id,
        string userId,
        int? perfilId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.Id == id && i.UserId == userId);

        if (perfilId.HasValue)
            query = query.Where(i => i.PerfilId == perfilId);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AtualizarAsyncRepository(Imposto imposto)
    {
        _context.Imposto.Update(imposto);
        await _context.SaveChangesAsync();
    }

    public async Task CopiarImpostosParaPerfilAsync(
        string userId,
        int perfilId,
        IEnumerable<Imposto> impostosOrigem,
        CancellationToken cancellationToken = default)
    {
        foreach (var origem in impostosOrigem)
        {
            var debito = new CodigoConta
            {
                Nome = origem.CodigoDebito?.Nome ?? $"{origem.Nome} Débito",
                Codigo = origem.CodigoDebito?.Codigo ?? string.Empty,
                Tipo = "debito",
                UserId = userId
            };

            var credito = new CodigoConta
            {
                Nome = origem.CodigoCredito?.Nome ?? $"{origem.Nome} Crédito",
                Codigo = origem.CodigoCredito?.Codigo ?? string.Empty,
                Tipo = "credito",
                UserId = userId
            };

            _context.CodigoConta.Add(debito);
            _context.CodigoConta.Add(credito);
            await _context.SaveChangesAsync(cancellationToken);

            _context.Imposto.Add(new Imposto
            {
                Nome = origem.Nome,
                UserId = userId,
                PerfilId = perfilId,
                CodigoDebitoId = debito.Id,
                CodigoCreditoId = credito.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Imposto>> ObterLegadosSemPerfilAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.UserId == userId && i.PerfilId == null)
            .ToListAsync(cancellationToken);
    }
}
