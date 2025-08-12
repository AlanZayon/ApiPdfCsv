using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations;

public class CodigoContaRepository : ICodigoContaRepository
{
    private readonly AppDbContext _context;

    public CodigoContaRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<CodigoConta?> ObterPorIdAsync(int id, string userId)
    {
        return await _context.CodigoConta.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);
    }
    public async Task AtualizarAsync(CodigoConta codigoConta)
    {
        _context.CodigoConta.Update(codigoConta);
        await _context.SaveChangesAsync();
    }
}

