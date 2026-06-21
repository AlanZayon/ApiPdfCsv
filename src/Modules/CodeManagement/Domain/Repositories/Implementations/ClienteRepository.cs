using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;

public class ClienteRepository : IClienteRepository
{
    private readonly AppDbContext _context;

    public ClienteRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(IEnumerable<Cliente> Items, int Total)> ListarAsync(
        string userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Clientes
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Ativo);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var digits = new string(term.Where(char.IsDigit).ToArray());
            query = query.Where(c =>
                c.RazaoSocial.Contains(term) ||
                (!string.IsNullOrEmpty(digits) && c.Cnpj.Contains(digits)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.RazaoSocial)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<Cliente?> ObterPorIdAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        return _context.Clientes
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);
    }

    public Task<Cliente?> ObterPorCnpjAsync(string userId, string cnpj, CancellationToken cancellationToken = default)
    {
        return _context.Clientes
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Cnpj == cnpj && c.Ativo, cancellationToken);
    }

    public async Task<Cliente> CriarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _context.Clientes.Add(cliente);
        await _context.SaveChangesAsync(cancellationToken);
        return cliente;
    }

    public async Task AtualizarAsync(Cliente cliente, CancellationToken cancellationToken = default)
    {
        _context.Clientes.Update(cliente);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
