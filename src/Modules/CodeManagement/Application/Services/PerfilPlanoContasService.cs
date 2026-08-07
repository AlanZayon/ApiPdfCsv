using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services;

public class PerfilPlanoContasService : IPerfilPlanoContasService
{
    private readonly IPerfilPlanoContasRepository _perfilRepository;
    private readonly IImpostoRepository _impostoRepository;

    public PerfilPlanoContasService(
        IPerfilPlanoContasRepository perfilRepository,
        IImpostoRepository impostoRepository)
    {
        _perfilRepository = perfilRepository;
        _impostoRepository = impostoRepository;
    }

    public async Task<IReadOnlyList<PerfilPlanoContasDto>> ListarAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        await GarantirPadraoAsync(userId, cancellationToken);
        var perfis = await _perfilRepository.ListarPorUsuarioAsync(userId, cancellationToken);
        return perfis.Select(ToDto).ToList();
    }

    public async Task<PerfilPlanoContasDto?> ObterAsync(
        int id,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var perfil = await _perfilRepository.ObterPorIdAsync(id, userId, cancellationToken);
        return perfil == null ? null : ToDto(perfil);
    }

    public async Task<PerfilPlanoContasDto?> ObterPadraoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var padrao = await GarantirPadraoAsync(userId, cancellationToken);
        return padrao;
    }

    public async Task<PerfilPlanoContasDto> CriarAsync(
        string userId,
        CriarPerfilRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Nome))
            throw new ArgumentException("Nome do perfil é obrigatório.");

        var existentes = await _perfilRepository.ListarPorUsuarioAsync(userId, cancellationToken);
        var origemId = request.CopiarDePerfilId
            ?? existentes.FirstOrDefault(p => p.IsDefault)?.Id
            ?? existentes.FirstOrDefault()?.Id;

        var perfil = await _perfilRepository.CriarAsync(new PerfilPlanoContas
        {
            UserId = userId,
            Nome = request.Nome.Trim(),
            IsDefault = existentes.Count == 0,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        if (origemId.HasValue)
        {
            var impostosOrigem = (await _impostoRepository.ObterTodosComCodigosAsync(userId, origemId, cancellationToken))
                .Where(i => i != null)
                .Cast<Imposto>()
                .ToList();

            if (impostosOrigem.Count > 0)
                await _impostoRepository.CopiarImpostosParaPerfilAsync(userId, perfil.Id, impostosOrigem, cancellationToken);

            var origem = await _perfilRepository.ObterPorIdAsync(origemId.Value, userId, cancellationToken);
            if (origem != null)
            {
                perfil.CodigoGenericoEntrada = origem.CodigoGenericoEntrada;
                perfil.CodigoGenericoSaida = origem.CodigoGenericoSaida;
                await _perfilRepository.AtualizarAsync(perfil, cancellationToken);
            }
        }

        return ToDto(perfil);
    }

    public async Task<PerfilPlanoContasDto?> AtualizarAsync(
        int id,
        string userId,
        AtualizarPerfilRequest request,
        CancellationToken cancellationToken = default)
    {
        var perfil = await _perfilRepository.ObterPorIdAsync(id, userId, cancellationToken);
        if (perfil == null) return null;

        if (!string.IsNullOrWhiteSpace(request.Nome))
            perfil.Nome = request.Nome.Trim();

        if (request.CodigoGenericoEntrada != null)
            perfil.CodigoGenericoEntrada = NormalizeCodigo(request.CodigoGenericoEntrada);

        if (request.CodigoGenericoSaida != null)
            perfil.CodigoGenericoSaida = NormalizeCodigo(request.CodigoGenericoSaida);

        if (request.IsDefault == true && !perfil.IsDefault)
        {
            var todos = await _perfilRepository.ListarPorUsuarioAsync(userId, cancellationToken);
            foreach (var outro in todos.Where(p => p.Id != perfil.Id && p.IsDefault))
            {
                outro.IsDefault = false;
                await _perfilRepository.AtualizarAsync(outro, cancellationToken);
            }
            perfil.IsDefault = true;
        }

        await _perfilRepository.AtualizarAsync(perfil, cancellationToken);
        return ToDto(perfil);
    }

    public async Task<bool> RemoverAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var perfil = await _perfilRepository.ObterPorIdAsync(id, userId, cancellationToken);
        if (perfil == null) return false;

        var todos = await _perfilRepository.ListarPorUsuarioAsync(userId, cancellationToken);
        if (todos.Count <= 1)
            throw new InvalidOperationException("Não é possível remover o único perfil de plano de contas.");

        if (perfil.IsDefault)
            throw new InvalidOperationException("Defina outro perfil como padrão antes de remover este.");

        var impostos = (await _impostoRepository.ObterTodosComCodigosAsync(userId, id, cancellationToken))
            .Where(i => i != null)
            .Cast<Imposto>()
            .ToList();

        // Soft approach: impostos orphaned by cascade delete via FK.
        await _perfilRepository.RemoverAsync(perfil, cancellationToken);
        return true;
    }

    public async Task<PerfilPlanoContasDto> GarantirPadraoAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var existentes = await _perfilRepository.ListarPorUsuarioAsync(userId, cancellationToken);
        if (existentes.Count > 0)
        {
            var padrao = existentes.FirstOrDefault(p => p.IsDefault) ?? existentes[0];
            if (!padrao.IsDefault)
            {
                padrao.IsDefault = true;
                await _perfilRepository.AtualizarAsync(padrao, cancellationToken);
            }

            // Attach legacy impostos (PerfilId null) to default profile.
            var legados = await _impostoRepository.ObterLegadosSemPerfilAsync(userId, cancellationToken);
            foreach (var imposto in legados)
            {
                imposto.PerfilId = padrao.Id;
                await _impostoRepository.AtualizarAsyncRepository(imposto);
            }

            return ToDto(padrao);
        }

        var criado = await _perfilRepository.CriarAsync(new PerfilPlanoContas
        {
            UserId = userId,
            Nome = "Padrão",
            IsDefault = true,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        var impostosSemPerfil = await _impostoRepository.ObterLegadosSemPerfilAsync(userId, cancellationToken);
        foreach (var imposto in impostosSemPerfil)
        {
            imposto.PerfilId = criado.Id;
            await _impostoRepository.AtualizarAsyncRepository(imposto);
        }

        return ToDto(criado);
    }

    private static string? NormalizeCodigo(string? codigo)
    {
        var trimmed = codigo?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed == "_")
            return null;
        return trimmed;
    }

    private static PerfilPlanoContasDto ToDto(PerfilPlanoContas perfil) => new()
    {
        Id = perfil.Id,
        Nome = perfil.Nome,
        IsDefault = perfil.IsDefault,
        CodigoGenericoEntrada = perfil.CodigoGenericoEntrada,
        CodigoGenericoSaida = perfil.CodigoGenericoSaida
    };
}
