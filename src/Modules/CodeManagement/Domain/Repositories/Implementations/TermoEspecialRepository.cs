using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EFCore.BulkExtensions;
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

        public async Task<Dictionary<(string Termo, bool TipoValor), TermoEspecial>>
    BuscarTodosTermosRelevantesAsync(string userId, string cnpj, int? codigoBanco)
        {
            var resultados = await _context.TermoEspecial
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.CNPJ == cnpj && t.CodigoBanco == codigoBanco)
                .ToListAsync();

            return resultados.ToDictionary(
                t => (t.Termo.ToLowerInvariant(), t.TipoValor),
                t => t);
        }

        public async Task<Dictionary<(string Termo, int? CodigoBanco, bool TipoValor), TermoEspecial>>
            BuscarTodosPorUsuarioCnpjAsync(string userId, string cnpj)
        {
            var resultados = await _context.TermoEspecial
                .AsNoTracking()
                .Where(t => t.UserId == userId && t.CNPJ == cnpj)
                .ToListAsync();

            return resultados.ToDictionary(
                t => (t.Termo, t.CodigoBanco, t.TipoValor),
                t => t);
        }

        public async Task AdicionarOuAtualizarEmLoteAsync(IEnumerable<TermoEspecial> termos)
        {
            var termosList = termos.ToList();
            if (!termosList.Any()) return;

            foreach (var termo in termosList)
            {
                if (string.IsNullOrWhiteSpace(termo.Id))
                    termo.Id = Guid.NewGuid().ToString();
            }

            await _context.BulkInsertOrUpdateAsync(termosList);
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
            if (string.IsNullOrEmpty(termo.Id))
            {
                termo.Id = Guid.NewGuid().ToString();
            }

            await _context.TermoEspecial.AddAsync(termo);
            await _context.SaveChangesAsync();

            return termo;
        }

        public async Task<TermoEspecial> AtualizarAsync(TermoEspecial termo)
        {
            _context.TermoEspecial.Update(termo);
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
        public async Task<IEnumerable<int?>> BuscarCodigosBancoPorCnpjAsync(
            string cnpj,
            string userId,
            string? codigoBanco = null)
        {
            int? codigoBancoInt = null;
            if (int.TryParse(codigoBanco, out var parsed))
                codigoBancoInt = parsed;

            return await _context.TermoEspecial
                .Where(t =>
                    t.CNPJ == cnpj &&
                    t.UserId == userId &&
                    t.CodigoBanco == codigoBancoInt)
                .Select(t => t.CodigoBanco)
                .Distinct()
                .ToListAsync();
        }

        public async Task<TermoEspecial?> BuscarPorTermoUsuarioCnpjEBancoAsync(
    string termo, string userId, string cnpj, int? codigoBanco)
        {
            return await _context.TermoEspecial
                .FirstOrDefaultAsync(t =>
                    t.Termo == termo &&
                    t.UserId == userId &&
                    t.CNPJ == cnpj &&
                    t.CodigoBanco == codigoBanco);
        }
        public async Task<IEnumerable<TermoEspecial>> BuscarPorUsuarioCnpjEBancoAsync(
            string userId, string cnpj, int? codigoBanco)
        {
            return await _context.TermoEspecial
                .Where(t =>
                    t.UserId == userId &&
                    t.CNPJ == cnpj &&
                    t.CodigoBanco == codigoBanco)
                .ToListAsync();
        }


        public async Task<TermoEspecial?> BuscarPorTermoUsuarioECnpjAsync(
            string termo, string userId, string cnpj)
        {
            return await _context.TermoEspecial
                .FirstOrDefaultAsync(t =>
                    t.Termo == termo &&
                    t.UserId == userId &&
                    t.CNPJ == cnpj);
        }
        public async Task<TermoEspecial?> BuscarPorTermoUsuarioCnpjEBancoETipoAsync(
string termo,
string userId,
string cnpj,
int? codigoBanco,
bool tipoPositivo)
        {
            return await _context.TermoEspecial
                .FirstOrDefaultAsync(t =>
                    t.Termo == termo &&
                    t.UserId == userId &&
                    t.CNPJ == cnpj &&
                    t.CodigoBanco == codigoBanco &&
                    t.TipoValor == tipoPositivo);
        }

        public async Task<TermoEspecial?> BuscarPorTermoUsuarioECnpjETipoAsync(
    string termo,
    string userId,
    string cnpj,
    bool tipoPositivo)
        {
            return await _context.TermoEspecial
                .FirstOrDefaultAsync(t =>
                    t.Termo == termo &&
                    t.UserId == userId &&
                    t.CNPJ == cnpj &&
                    t.TipoValor == tipoPositivo);
        }

        public async Task<bool> SalvarAlteracoesAsync()
        {
            try
            {
                return await _context.SaveChangesAsync() > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<IEnumerable<TermoEspecial>> SugerirPorTermoAsync(
            string userId,
            string cnpj,
            string termo,
            int limit = 5)
        {
            var normalized = termo.Trim().ToLowerInvariant();
            return await _context.TermoEspecial
                .AsNoTracking()
                .Where(t =>
                    t.UserId == userId &&
                    t.CNPJ == cnpj &&
                    EF.Functions.ILike(t.Termo, $"%{normalized}%"))
                .OrderBy(t => t.Termo.Length)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<int> CopiarMapeamentosAsync(
            string userId,
            string cnpjOrigem,
            string cnpjDestino,
            int? codigoBancoOrigem,
            int? codigoBancoDestino)
        {
            var origem = await BuscarPorUsuarioCnpjEBancoAsync(userId, cnpjOrigem, codigoBancoOrigem);
            var copias = origem.Select(t => new TermoEspecial
            {
                Id = Guid.NewGuid().ToString(),
                Termo = t.Termo,
                UserId = userId,
                CodigoDebito = t.CodigoDebito,
                CodigoCredito = t.CodigoCredito,
                CodigoBanco = codigoBancoDestino ?? t.CodigoBanco,
                CNPJ = cnpjDestino,
                TipoValor = t.TipoValor
            }).ToList();

            if (copias.Count == 0) return 0;

            await AdicionarOuAtualizarEmLoteAsync(copias);
            return copias.Count;
        }

    }
}