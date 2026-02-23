using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services;

public class ImpostoService : IImpostoService
{
    private readonly IImpostoRepository _impostoRepository;
    private readonly ICodigoContaRepository _codigoContaRepository;
    private readonly ICodigoContaService _codigoContaService;

    private readonly IMapper _mapper;

    public ImpostoService(
        IImpostoRepository impostoRepository,
        ICodigoContaRepository codigoContaRepository,
        ICodigoContaService codigoContaService,
        IMapper mapper)
    {
        _impostoRepository = impostoRepository;
        _codigoContaRepository = codigoContaRepository;
        _codigoContaService = codigoContaService;

        _mapper = mapper;
    }

    public async Task<ImpostoDto?> ObterPorIdAsync(int id, string userId)
    {
        var imposto = await _impostoRepository.ObterPorIdAsync(id, userId);
        if (imposto == null)
            return null;

        return _mapper.Map<ImpostoDto>(imposto);
    }

    public async Task<IEnumerable<ImpostoDto>> ObterTodosAsync(string userId)
    {
        var impostos = await _impostoRepository.ObterTodosComCodigosAsync(userId);
        return _mapper.Map<IEnumerable<ImpostoDto>>(impostos);
    }


    public async Task<IEnumerable<ImpostoDto>> AtualizarAsyncService(IEnumerable<ImpostoDto> impostos, string userId)
    {
        foreach (var dto in impostos)
        {
            var imposto = await _impostoRepository.ObterPorIdAsync(dto.Id, userId);
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

        return await ObterTodosAsync(userId);
    }

public async Task<List<decimal>> MapearDebito(List<string> historico, string userId)
{
    var mapeamento = await ConstruirMapeamento(
        userId, 
        (imposto) => imposto.CodigoDebito?.Codigo
    );

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

public async Task<List<decimal>> MapearCredito(List<string> historico, string userId)
{
    var mapeamento = await ConstruirMapeamento(
        userId, 
        (imposto) => imposto.CodigoCredito?.Codigo
    );

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
    Func<Imposto, string?> obterCodigo)
{
    var impostos = await _impostoRepository.ObterTodosComCodigosAsync(userId);
    var mapeamento = new Dictionary<string, decimal>();

    // Mapeamentos de sinônimos
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
        
        // Mapeamento do nome original
        mapeamento[nomePadrao] = codigo;

        // Aplica sinônimos
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


    // public async Task<ImpostoDto> AdicionarAsync(ImpostoDto dto, string userId)
    // {
    //     // Validar códigos de conta
    //     if (dto.CodigoDebitoId.HasValue && 
    //         !await _codigoContaRepository.ExisteAsync(dto.CodigoDebitoId.Value, userId))
    //     {
    //         throw new KeyNotFoundException("Código de débito não encontrado");
    //     }

    //     if (dto.CodigoCreditoId.HasValue && 
    //         !await _codigoContaRepository.ExisteAsync(dto.CodigoCreditoId.Value, userId))
    //     {
    //         throw new KeyNotFoundException("Código de crédito não encontrado");
    //     }

    //     var imposto = _mapper.Map<Imposto>(dto);
    //     imposto.UserId = userId;

    //     await _impostoRepository.AdicionarAsync(imposto);

    //     return _mapper.Map<ImpostoDto>(imposto);
    // }

    // public async Task<bool> RemoverAsync(int id, string userId)
    // {
    //     var imposto = await _impostoRepository.ObterPorIdAsync(id, userId);
    //     if (imposto == null)
    //         return false;

    //     await _impostoRepository.RemoverAsync(imposto);
    //     return true;
    // }
}