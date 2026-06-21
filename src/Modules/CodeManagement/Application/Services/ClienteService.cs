using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services;

public class ClienteService : IClienteService
{
    private readonly IClienteRepository _clienteRepository;
    private readonly IImpostoRepository _impostoRepository;
    private readonly IMapper _mapper;

    public ClienteService(
        IClienteRepository clienteRepository,
        IImpostoRepository impostoRepository,
        IMapper mapper)
    {
        _clienteRepository = clienteRepository;
        _impostoRepository = impostoRepository;
        _mapper = mapper;
    }

    public async Task<(IEnumerable<ClienteDto> Items, int Total)> ListarAsync(
        string userId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, total) = await _clienteRepository.ListarAsync(userId, search, page, pageSize, cancellationToken);
        return (_mapper.Map<IEnumerable<ClienteDto>>(items), total);
    }

    public async Task<ClienteDto?> ObterPorIdAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id, userId, cancellationToken);
        return cliente == null ? null : _mapper.Map<ClienteDto>(cliente);
    }

    public async Task<ClienteDto> CriarAsync(
        string userId,
        CreateClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cnpj = NormalizarCnpj(request.Cnpj);
        if (cnpj.Length != 14)
            throw new ArgumentException("CNPJ deve conter 14 dígitos.");

        if (string.IsNullOrWhiteSpace(request.RazaoSocial))
            throw new ArgumentException("Razão social é obrigatória.");

        var existente = await _clienteRepository.ObterPorCnpjAsync(userId, cnpj, cancellationToken);
        if (existente != null)
            throw new InvalidOperationException("Já existe um cliente ativo com este CNPJ.");

        var cliente = new Cliente
        {
            UserId = userId,
            Cnpj = cnpj,
            RazaoSocial = request.RazaoSocial.Trim(),
            CodigoBancoPadrao = request.CodigoBancoPadrao,
            Ativo = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var criado = await _clienteRepository.CriarAsync(cliente, cancellationToken);
        await CopiarImpostosPadraoParaClienteAsync(userId, criado.Id, cancellationToken);

        return _mapper.Map<ClienteDto>(criado);
    }

    public async Task<ClienteDto?> AtualizarAsync(
        int id,
        string userId,
        UpdateClienteRequest request,
        CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id, userId, cancellationToken);
        if (cliente == null) return null;

        if (!string.IsNullOrWhiteSpace(request.RazaoSocial))
            cliente.RazaoSocial = request.RazaoSocial.Trim();

        cliente.CodigoBancoPadrao = request.CodigoBancoPadrao;
        cliente.Ativo = request.Ativo;

        await _clienteRepository.AtualizarAsync(cliente, cancellationToken);
        return _mapper.Map<ClienteDto>(cliente);
    }

    public async Task<bool> DesativarAsync(int id, string userId, CancellationToken cancellationToken = default)
    {
        var cliente = await _clienteRepository.ObterPorIdAsync(id, userId, cancellationToken);
        if (cliente == null) return false;

        cliente.Ativo = false;
        await _clienteRepository.AtualizarAsync(cliente, cancellationToken);
        return true;
    }

    public async Task<(string Cnpj, int? CodigoBanco, string? RazaoSocial)?> ResolverParaUploadAsync(
        string userId,
        int? clienteId,
        string? cnpjHeader,
        string? codigoBancoHeader,
        CancellationToken cancellationToken = default)
    {
        if (clienteId.HasValue)
        {
            var cliente = await _clienteRepository.ObterPorIdAsync(clienteId.Value, userId, cancellationToken);
            if (cliente == null || !cliente.Ativo)
                throw new UnauthorizedAccessException("Cliente não encontrado.");

            int? banco = cliente.CodigoBancoPadrao;
            if (int.TryParse(codigoBancoHeader, out var bancoHeader))
                banco = bancoHeader;

            return (cliente.Cnpj, banco, cliente.RazaoSocial);
        }

        var cnpj = NormalizarCnpj(cnpjHeader ?? string.Empty);
        if (string.IsNullOrEmpty(cnpj))
            return null;

        int? codigoBanco = int.TryParse(codigoBancoHeader, out var parsedBanco) ? parsedBanco : null;

        var existente = await _clienteRepository.ObterPorCnpjAsync(userId, cnpj, cancellationToken);
        if (existente != null)
        {
            if (!codigoBanco.HasValue && existente.CodigoBancoPadrao.HasValue)
                codigoBanco = existente.CodigoBancoPadrao;

            return (existente.Cnpj, codigoBanco, existente.RazaoSocial);
        }

        return (cnpj, codigoBanco, null);
    }

    private async Task CopiarImpostosPadraoParaClienteAsync(
        string userId,
        int clienteId,
        CancellationToken cancellationToken)
    {
        var padrao = (await _impostoRepository.ObterTodosComCodigosAsync(userId, null, cancellationToken))
            .Where(i => i != null)
            .ToList();

        if (padrao.Count == 0) return;

        await _impostoRepository.CopiarImpostosParaClienteAsync(userId, clienteId, padrao!, cancellationToken);
    }

    private static string NormalizarCnpj(string cnpj)
        => new string(cnpj.Where(char.IsDigit).ToArray());
}
