
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using AutoMapper;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CodigoConta, CodigoContaDto>();

        CreateMap<Imposto, ImpostoDto>();

        CreateMap<TermoEspecial, TermoEspecialDto>();

        CreateMap<TermoEspecialDto, TermoEspecial>();
    
    }
}
