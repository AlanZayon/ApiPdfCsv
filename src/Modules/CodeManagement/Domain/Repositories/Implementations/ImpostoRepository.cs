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

    public ImpostoRepository(AppDbContext context , ILogger logger)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<IEnumerable<Imposto?>> ObterTodosComCodigosAsync(
        string userId,
        int? clienteId = null,
        CancellationToken cancellationToken = default)
    {
        if (clienteId.HasValue)
        {
            var especificos = await _context.Imposto
                .Include(i => i.CodigoDebito)
                .Include(i => i.CodigoCredito)
                .Where(i => i.UserId == userId && i.ClienteId == clienteId)
                .ToListAsync(cancellationToken);

            if (especificos.Count > 0)
                return especificos;
        }

        return await _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.UserId == userId && i.ClienteId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<Imposto?> ObterPorIdAsync(int id, string userId, int? clienteId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Imposto
            .Include(i => i.CodigoDebito)
            .Include(i => i.CodigoCredito)
            .Where(i => i.Id == id && i.UserId == userId);

        if (clienteId.HasValue)
            query = query.Where(i => i.ClienteId == clienteId || i.ClienteId == null);
        else
            query = query.Where(i => i.ClienteId == null);

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task AtualizarAsyncRepository(Imposto imposto)
    {
        _context.Imposto.Update(imposto);
        await _context.SaveChangesAsync();
    }

    public async Task CopiarImpostosParaClienteAsync(
        string userId,
        int clienteId,
        IEnumerable<Imposto> impostosPadrao,
        CancellationToken cancellationToken = default)
    {
        foreach (var padrao in impostosPadrao)
        {
            if (padrao.ClienteId != null) continue;

            var debito = new CodigoConta
            {
                Nome = padrao.CodigoDebito?.Nome ?? $"{padrao.Nome} Débito",
                Codigo = padrao.CodigoDebito?.Codigo ?? string.Empty,
                Tipo = "debito",
                UserId = userId
            };

            var credito = new CodigoConta
            {
                Nome = padrao.CodigoCredito?.Nome ?? $"{padrao.Nome} Crédito",
                Codigo = padrao.CodigoCredito?.Codigo ?? string.Empty,
                Tipo = "credito",
                UserId = userId
            };

            _context.CodigoConta.Add(debito);
            _context.CodigoConta.Add(credito);
            await _context.SaveChangesAsync(cancellationToken);

            _context.Imposto.Add(new Imposto
            {
                Nome = padrao.Nome,
                UserId = userId,
                ClienteId = clienteId,
                CodigoDebitoId = debito.Id,
                CodigoCreditoId = credito.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
