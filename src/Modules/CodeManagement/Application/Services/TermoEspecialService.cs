using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Responses;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;

namespace ApiPdfCsv.Modules.CodeManagement.Application.Services
{

    public class TermoEspecialService : ITermoEspecialService
    {
        private readonly ITermoEspecialRepository _termoEspecialRepository;
        private readonly IMapper _mapper;

        public TermoEspecialService(ITermoEspecialRepository termoEspecialRepository, IMapper mapper)
        {
            _termoEspecialRepository = termoEspecialRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<TermoEspecialDto>> BuscarPorUsuarioCnpjEBancoAsync(
            string userId, string cnpj, int? codigoBanco)
        {
            var termoEspeciais = await _termoEspecialRepository.BuscarPorUsuarioCnpjEBancoAsync(
                userId, cnpj, codigoBanco);

            return _mapper.Map<IEnumerable<TermoEspecialDto>>(termoEspeciais);
        }

        public async Task<ResultadoAtualizacao> AtualizarCodigosAsync(
            string userId,
            string cnpj,
            int? codigoBanco,
            List<AtualizacaoCodigoDto> atualizacoes)
        {
            if (!atualizacoes.Any())
            {
                return new ResultadoAtualizacao { Sucesso = false, Mensagem = "Nenhuma atualização fornecida" };
            }

            var registrosExistentes = await _termoEspecialRepository
                .BuscarPorUsuarioCnpjEBancoAsync(userId, cnpj, codigoBanco);

            if (!registrosExistentes.Any())
            {
                return new ResultadoAtualizacao { Sucesso = false, Mensagem = "Nenhum registro encontrado para atualização" };
            }

            var idsExistentes = registrosExistentes.Select(r => r.Id).ToHashSet();
            var registrosAtualizados = 0;

            foreach (var atualizacao in atualizacoes)
            {
                if (!idsExistentes.Contains(atualizacao.TermoEspecialId.ToString()))
                {
                    continue;
                }

                var registro = registrosExistentes.First(r => r.Id == atualizacao.TermoEspecialId.ToString());

                if (atualizacao.NovoCodigoDebito.HasValue)
                {
                    registro.CodigoDebito = atualizacao.NovoCodigoDebito.Value;
                }

                if (atualizacao.NovoCodigoCredito.HasValue)
                {
                    registro.CodigoCredito = atualizacao.NovoCodigoCredito.Value;
                }

                registrosAtualizados++;
            }

            var sucesso = await _termoEspecialRepository.SalvarAlteracoesAsync();

            return new ResultadoAtualizacao
            {
                Sucesso = sucesso,
                RegistrosAtualizados = registrosAtualizados,
                Mensagem = sucesso ? "Atualização realizada com sucesso" : "Erro ao salvar as alterações"
            };
        }
    }
}