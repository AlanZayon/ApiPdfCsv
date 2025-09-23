using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ApiPdfCsv.CrossCutting.Data;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

namespace ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Implementations
{
    public class TermoEspecialRepository : ITermoEspecialRepository
    {
        private readonly AppDbContext _context;

        public TermoEspecialRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<TermoEspecial?> BuscarPorTermoEUsuarioAsync(string termo, string userId)
        {
            return await _context.TermoEspecial
                .FirstOrDefaultAsync(t =>
                    t.Termo.ToLower() == termo.ToLower() &&
                    t.UserId == userId);
        }

        public async Task<TermoEspecial> AdicionarAsync(TermoEspecial termo)
        {
            // Gera um novo ID se não foi fornecido
            if (string.IsNullOrEmpty(termo.Id))
            {
                termo.Id = Guid.NewGuid().ToString();
            }

            await _context.TermoEspecial.AddAsync(termo);
            await _context.SaveChangesAsync();

            return termo;
        }

        public async Task<bool> ExisteTermoAsync(string termo, string userId)
        {
            return await _context.TermoEspecial
                .AnyAsync(t =>
                    t.Termo.ToLower() == termo.ToLower() &&
                    t.UserId == userId);
        }

        public async Task<IEnumerable<TermoEspecial>> ObterTodosPorUsuarioAsync(string userId)
        {
            return await _context.TermoEspecial
                .Where(t => t.UserId == userId)
                .ToListAsync();
        }
        public async Task<IEnumerable<int?>> BuscarCodigosBancoPorCnpjAsync(string cnpj, string userId)
        {
            return await _context.TermoEspecial
                .Where(t => t.CNPJ == cnpj && t.UserId == userId)
                .Select(t => t.CodigoBanco)
                .Distinct()
                .ToListAsync();
        }
    
    }
}