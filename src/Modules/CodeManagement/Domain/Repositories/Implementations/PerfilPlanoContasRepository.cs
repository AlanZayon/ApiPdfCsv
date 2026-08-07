using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;

public class PerfilPlanoContasRepository : IPerfilPlanoContasRepository
{
    private readonly AppDbContext _context;

    public PerfilPlanoContasRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<PerfilPlanoContas>> ListarPorUsuarioAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.PerfisPlanoContas
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Nome)
            .ToListAsync(cancellationToken);
    }

    public Task<PerfilPlanoContas?> ObterPorIdAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _context.PerfisPlanoContas
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, cancellationToken);
    }

    public Task<PerfilPlanoContas?> ObterPadraoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        return _context.PerfisPlanoContas
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PerfilPlanoContas> CriarAsync(
        PerfilPlanoContas perfil,
        CancellationToken cancellationToken = default)
    {
        _context.PerfisPlanoContas.Add(perfil);
        await _context.SaveChangesAsync(cancellationToken);
        return perfil;
    }

    public async Task AtualizarAsync(PerfilPlanoContas perfil, CancellationToken cancellationToken = default)
    {
        _context.PerfisPlanoContas.Update(perfil);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoverAsync(PerfilPlanoContas perfil, CancellationToken cancellationToken = default)
    {
        _context.PerfisPlanoContas.Remove(perfil);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
