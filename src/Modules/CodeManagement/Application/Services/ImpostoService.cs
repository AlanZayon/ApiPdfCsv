using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services;

public class ImpostoService : IImpostoService
{
    private readonly IImpostoRepository _impostoRepository;
    private readonly ICodigoContaRepository _codigoContaRepository;
    private readonly ICodigoContaService _codigoContaService;

    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    private static string CacheKey(string userId, int? clienteId)
        => clienteId.HasValue ? $"impostos:{userId}:cliente:{clienteId}" : $"impostos:{userId}";

    public ImpostoService(
        IImpostoRepository impostoRepository,
        ICodigoContaRepository codigoContaRepository,
        ICodigoContaService codigoContaService,
        IMapper mapper,
        IMemoryCache cache)
    {
        _impostoRepository = impostoRepository;
        _codigoContaRepository = codigoContaRepository;
        _codigoContaService = codigoContaService;

        _mapper = mapper;
        _cache = cache;
    }

    public async Task<ImpostoDto?> ObterPorIdAsync(int id, string userId, int? clienteId = null)
    {
        var imposto = await _impostoRepository.ObterPorIdAsync(id, userId, clienteId);
        if (imposto == null)
            return null;

        return _mapper.Map<ImpostoDto>(imposto);
    }

    public async Task<IEnumerable<ImpostoDto>> ObterTodosAsync(string userId, int? clienteId = null)
    {
        var impostos = await _impostoRepository.ObterTodosComCodigosAsync(userId, clienteId);
        return _mapper.Map<IEnumerable<ImpostoDto>>(impostos);
    }


    public async Task<IEnumerable<ImpostoDto>> AtualizarAsyncService(
        IEnumerable<ImpostoDto> impostos,
        string userId,
        int? clienteId = null)
    {
        foreach (var dto in impostos)
        {
            var imposto = await _impostoRepository.ObterPorIdAsync(dto.Id, userId, clienteId);
            if (imposto == null)
                throw new Exception($"Imposto com ID {dto.Id} não encontrado.");

            var debitoExistente = await _codigoContaService.ObterPorIdAsync(imposto.CodigoDebitoId ?? 0, userId);
            var creditoExistente = await _codigoContaService.ObterPorIdAsync(imposto.CodigoCreditoId ?? 0, userId);

            if (debitoExistente == null || creditoExistente == null)
                throw new Exception("Código débito ou crédito não encontrado para o imposto.");

            debitoExistente.Codigo = dto.CodigoDebito?.Codigo ?? "";
            debitoExistente.Tipo = "debito";
            debitoExistente.UserId = userId;

            creditoExistente.Codigo = dto.CodigoCredito?.Codigo ?? "";
            creditoExistente.Tipo = "credito";
            creditoExistente.UserId = userId;

            await _codigoContaService.AtualizarAsync(debitoExistente);
            await _codigoContaService.AtualizarAsync(creditoExistente);

            await _impostoRepository.AtualizarAsyncRepository(imposto);
        }

        _cache.Remove(CacheKey(userId, clienteId));
        if (clienteId.HasValue)
            _cache.Remove(CacheKey(userId, null));

        return await ObterTodosAsync(userId, clienteId);
    }

public async Task<List<decimal>> MapearDebito(List<string> historico, string userId, int? clienteId = null)
{
    var mapeamento = await ConstruirMapeamento(
        userId,
        clienteId,
        (imposto) => imposto.CodigoDebito?.Codigo
    );

    return AplicarMapeamento(historico, mapeamento);
}

public async Task<List<decimal>> MapearCredito(List<string> historico, string userId, int? clienteId = null)
{
    var mapeamento = await ConstruirMapeamento(
        userId,
        clienteId,
        (imposto) => imposto.CodigoCredito?.Codigo
    );

    return AplicarMapeamento(historico, mapeamento);
}

public async Task<(List<decimal> Debitos, List<decimal> Creditos)> MapearDebitoECredito(
    List<string> historico,
    string userId,
    int? clienteId = null)
{
    var impostos = await GetImpostosCachedAsync(userId, clienteId);
    var mapeamentoDebito = ConstruirMapeamentoDeImpostos(
        impostos,
        (imposto) => imposto.CodigoDebito?.Codigo
    );
    var mapeamentoCredito = ConstruirMapeamentoDeImpostos(
        impostos,
        (imposto) => imposto.CodigoCredito?.Codigo
    );

    return (AplicarMapeamento(historico, mapeamentoDebito), AplicarMapeamento(historico, mapeamentoCredito));
}

private static List<decimal> AplicarMapeamento(
    List<string> historico,
    Dictionary<string, decimal> mapeamento)
{
    return historico.Select(item =>
    {
        var h = item.ToUpper();

        foreach (var map in mapeamento)
        {
            if (h.Contains(map.Key))
            {
                return map.Value;
            }
        }

        return 0m;
    }).ToList();
}

private async Task<Dictionary<string, decimal>> ConstruirMapeamento(
    string userId,
    int? clienteId,
    Func<Imposto, string?> obterCodigo)
{
    var impostos = await GetImpostosCachedAsync(userId, clienteId);
    return ConstruirMapeamentoDeImpostos(impostos, obterCodigo);
}

private async Task<IEnumerable<Imposto>> GetImpostosCachedAsync(string userId, int? clienteId)
{
    return await _cache.GetOrCreateAsync(CacheKey(userId, clienteId), async entry =>
    {
        entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
        return await _impostoRepository.ObterTodosComCodigosAsync(userId, clienteId);
    }) ?? Enumerable.Empty<Imposto>();
}

private static Dictionary<string, decimal> ConstruirMapeamentoDeImpostos(
    IEnumerable<Imposto> impostos,
    Func<Imposto, string?> obterCodigo)
{
    var mapeamento = new Dictionary<string, decimal>();

    var sinonimos = new Dictionary<string, List<string>>
    {
        { "INSS", new List<string> { "INSS", "DCTFWEB" } },
        { "IRRF", new List<string> { "IRRF", "DCTFWEB" } },
        { "MULTA JUROS", new List<string> { "MULTA JUROS", "MULTA E JUROS" } },
        { "MULTA", new List<string> { "MULTA", "DESCONHECIDO" } }
    };

    foreach (var imposto in impostos)
    {
        if (imposto == null || string.IsNullOrEmpty(imposto.Nome))
            continue;

        var codigoStr = obterCodigo(imposto);
        if (string.IsNullOrEmpty(codigoStr) || !decimal.TryParse(codigoStr, out var codigo))
            continue;

        var nomePadrao = imposto.Nome.ToUpper().Replace("_", " ");

        mapeamento[nomePadrao] = codigo;

        foreach (var sinonimo in sinonimos)
        {
            if (nomePadrao.Contains(sinonimo.Key))
            {
                foreach (var variacao in sinonimo.Value)
                {
                    mapeamento[variacao] = codigo;
                }
            }
        }
    }

    return mapeamento;
}
}
