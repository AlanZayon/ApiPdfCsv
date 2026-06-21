using ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;
using ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Responses;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using AutoMapper;
using System.Globalization;
using System.Text;

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

        public async Task<string> ExportarCsvAsync(string userId, string cnpj, int? codigoBanco)
        {
            var termos = await _termoEspecialRepository.BuscarPorUsuarioCnpjEBancoAsync(userId, cnpj, codigoBanco);
            var sb = new StringBuilder();
            sb.AppendLine("termo,codigo_debito,codigo_credito,tipo_valor");

            foreach (var termo in termos)
            {
                sb.AppendLine(string.Join(',',
                    EscapeCsv(termo.Termo),
                    termo.CodigoDebito.ToString(CultureInfo.InvariantCulture),
                    termo.CodigoCredito.ToString(CultureInfo.InvariantCulture),
                    termo.TipoValor ? "positivo" : "negativo"));
            }

            return sb.ToString();
        }

        public async Task<int> ImportarCsvAsync(string userId, string cnpj, int? codigoBanco, Stream csvStream)
        {
            using var reader = new StreamReader(csvStream, Encoding.UTF8);
            var headerSkipped = false;
            var termos = new List<TermoEspecial>();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (line.StartsWith("termo", StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                var parts = ParseCsvLine(line);
                if (parts.Count < 4) continue;

                if (!int.TryParse(parts[1], out var debito) || !int.TryParse(parts[2], out var credito))
                    continue;

                var tipoValor = parts[3].Trim().Equals("positivo", StringComparison.OrdinalIgnoreCase);

                termos.Add(new TermoEspecial
                {
                    Id = Guid.NewGuid().ToString(),
                    Termo = parts[0].Trim(),
                    UserId = userId,
                    CNPJ = cnpj,
                    CodigoBanco = codigoBanco,
                    CodigoDebito = debito,
                    CodigoCredito = credito,
                    TipoValor = tipoValor
                });
            }

            if (termos.Count == 0) return 0;

            await _termoEspecialRepository.AdicionarOuAtualizarEmLoteAsync(termos);
            return termos.Count;
        }

        public async Task<IEnumerable<TermoEspecialDto>> SugerirAsync(string userId, string cnpj, string termo)
        {
            var sugestoes = await _termoEspecialRepository.SugerirPorTermoAsync(userId, cnpj, termo);
            return _mapper.Map<IEnumerable<TermoEspecialDto>>(sugestoes);
        }

        public Task<int> CopiarMapeamentosAsync(
            string userId,
            string cnpjOrigem,
            string cnpjDestino,
            int? codigoBancoOrigem,
            int? codigoBancoDestino)
        {
            return _termoEspecialRepository.CopiarMapeamentosAsync(
                userId, cnpjOrigem, cnpjDestino, codigoBancoOrigem, codigoBancoDestino);
        }

        private static string EscapeCsv(string value)
        {
            if (value.Contains('"') || value.Contains(',') || value.Contains('\n'))
                return $"\"{value.Replace("\"", "\"\"")}\"";
            return value;
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            result.Add(current.ToString());
            return result;
        }
    }
}