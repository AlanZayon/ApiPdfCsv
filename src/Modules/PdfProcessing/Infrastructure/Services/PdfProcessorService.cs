using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Text.Json;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Shared.Utils;
using System.Security.Claims;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;


public class PdfProcessorService : IPdfProcessorService
{
    private readonly ILogger _logger;
    private readonly IImpostoService _impostoService;

    // Classe para encapsular o estado do processamento de forma thread-safe
    private class ProcessingState
    {
        public ComprovanteData Current { get; set; } = InitializeCurrent();
        public List<string> Descricoes { get; } = new();
        public List<decimal> Debitos { get; } = new();
        public List<decimal> Creditos { get; } = new();
        public List<decimal> Totais { get; } = new();

        public void Reset()
        {
            Current = InitializeCurrent();
            ClearArrays();
        }

        public void ClearArrays()
        {
            Descricoes.Clear();
            Debitos.Clear();
            Creditos.Clear();
            Totais.Clear();
        }
    }

    public PdfProcessorService(ILogger logger, IImpostoService impostoService)
    {
        _impostoService = impostoService;
        _logger = logger;
    }

    public async Task<ProcessedPdfData> Process(string filePath, string userId)
    {
        var state = new ProcessingState();
        var comprovantes = new List<ComprovanteData>();
        var collectingDescricoes = false;
        var waitingFinish = false;

        using var reader = new PdfReader(filePath);

            _logger.Info($"Processing PDF file: {filePath}");

            for (var page = 1; page <= reader.NumberOfPages; page++)
            {

                var strategy = new SimpleTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(reader, page, strategy);
                var lines = text.Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

                for (var i = 0; i < lines.Count; i++)
                {
                    var line = lines[i];

                    if (line.Contains("Agência Estabelecimento Valor Reservado/Restituído Referência"))
                    {
                        ExtractDataArrecadacao(state, lines[i + 1]);
                        if (waitingFinish)
                        {
                            FinalizarComprovante(state, comprovantes);
                            waitingFinish = false;
                        }
                        continue;
                    }

                    if (line.Contains("Composição do Documento de Arrecadação"))
                    {
                        collectingDescricoes = true;
                        continue;
                    }

                    if (collectingDescricoes)
                    {
                        if (line.StartsWith("Totais"))
                        {
                            collectingDescricoes = false;
                            await ProcessarTotais(state, userId);
                        }
                        else if (Regex.IsMatch(line, @"^\d{4}(?=.*[A-Za-z]).*\d{1,3},\d{2}$"))
                        {
                            ProcessarLinhaPagamento(state, line);
                        }
                    }

                    if (line == "Totais")
                    {
                        await ProcessarLinhaTotais(state, lines[i + 1], userId);
                        waitingFinish = true;
                    }
                }
            }

        FinalizarComprovante(state, comprovantes);
        return new ProcessedPdfData(comprovantes);
    }

    private static ComprovanteData InitializeCurrent()
    {
        return new ComprovanteData
        {
            DataArrecadacao = string.Empty,
            Debito = new List<decimal>(),
            Credito = new List<decimal>(),
            Total = new List<decimal>(),
            Descricoes = new List<string>()
        };
    }

    private void ExtractDataArrecadacao(ProcessingState state, string line)
    {
        var dateRegex = new Regex(@"\d{2}/\d{2}/\d{4}");
        var match = dateRegex.Match(line);
        if (match.Success)
        {
            state.Current.DataArrecadacao = match.Value;
        }
    }

    private void ProcessarLinhaPagamento(ProcessingState state, string line)
    {
        var valoresHistorico = PdfUtils.ParseLinhaHistorico(line);
        if (valoresHistorico == null) return;

        state.Totais.Add(valoresHistorico.Principal);
        var historico = PdfUtils.ExtrairHistorico(line);
        state.Descricoes.Add(historico);
    }

    private async Task ProcessarTotais(ProcessingState state, string userId)
    {
        var (descricoes, totais) = PdfUtils.AgruparDescricoesEValores(
            new List<string>(state.Descricoes),
            new List<decimal>(state.Totais)
        );

        state.Debitos.AddRange(await _impostoService.MapearDebito(descricoes, userId));
        state.Creditos.AddRange(await _impostoService.MapearCredito(descricoes, userId));

        state.Current = new ComprovanteData
        {
            DataArrecadacao = state.Current.DataArrecadacao,
            Descricoes = descricoes,
            Total = totais,
            Debito = new List<decimal>(state.Debitos),
            Credito = new List<decimal>(state.Creditos)
        };

        state.ClearArrays();
    }

    private async Task ProcessarLinhaTotais(ProcessingState state, string totalLine, string userId)
    {
        if (string.IsNullOrWhiteSpace(totalLine)) return;

        var totalLineTrim = totalLine.Trim();
        var priceMatches = Regex.Matches(totalLineTrim, @"\d{1,3}(?:\.\d{3})*,\d{2}");
        if (priceMatches.Count == 0) return;

        await ProcessarMultaEJuros(state, totalLineTrim, userId);
    }

    private async Task ProcessarMultaEJuros(ProcessingState state, string totalLineTrim, string userId)
    {
        var parsedValues = PdfUtils.ParseTotaisLinha(totalLineTrim);
        if (parsedValues?.SomaMultaJuros == null) return;

        state.Current.Descricoes.Add("PG. MULTA E JUROS XX");
        state.Current.Debito.AddRange(await _impostoService.MapearDebito(new List<string> { "PG. MULTA E JUROS XX" }, userId));
        state.Current.Credito.AddRange(await _impostoService.MapearCredito(new List<string> { "PG. MULTA E JUROS XX" }, userId));
        state.Current.Total.Add(parsedValues.SomaMultaJuros);
    }

    private void FinalizarComprovante(ProcessingState state, List<ComprovanteData> comprovantes)
    {
        if (string.IsNullOrEmpty(state.Current.DataArrecadacao)) return;

        comprovantes.Add(new ComprovanteData(state.Current));
        state.Reset();
    }

}