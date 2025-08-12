using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services;

public class CodigoContaService : ICodigoContaService
{
    private readonly ICodigoContaRepository _repository;
    private readonly IMapper _mapper;

    public CodigoContaService(ICodigoContaRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CodigoConta> ObterPorIdAsync(int id, string userId)
    {
        return await _repository.ObterPorIdAsync(id, userId)
            ?? throw new KeyNotFoundException($"Código de conta com ID {id} não encontrado para o usuário {userId}.");
    }

    public async Task AtualizarAsync(CodigoConta codigo)
    {
        await _repository.AtualizarAsync(codigo);
    }

}