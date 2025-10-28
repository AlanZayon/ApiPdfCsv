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

    public PdfProcessorService(ILogger logger, IImpostoService impostoService)
    {
        _impostoService = impostoService;
        _logger = logger;
    }

    public async Task<ProcessedPdfData> Process(string filePath, string userId)
    {
        _logger.Info($"Processing PDF file: {filePath}");
        
        var current = InitializeCurrent();
        var descricoes = new List<string>();
        var debitos = new List<decimal>();
        var creditos = new List<decimal>();
        var totais = new List<decimal>();
        var comprovantes = new List<ComprovanteData>();
        var collectingDescricoes = false;
        var waitingFinish = false;

        using var reader = new PdfReader(filePath);

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
                    ExtractDataArrecadacao(current, lines[i + 1]);
                    if (waitingFinish)
                    {
                        FinalizarComprovante(current, comprovantes, userId, descricoes, debitos, creditos, totais);
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
                        await ProcessarTotais(current, userId, descricoes, debitos, creditos, totais);
                    }
                    else if (Regex.IsMatch(line, @"^\d{4}(?=.*[A-Za-z]).*\d{1,3},\d{2}$"))
                    {
                        ProcessarLinhaPagamento(line, descricoes, totais);
                    }
                }

                if (line == "Totais")
                {
                    await ProcessarLinhaTotais(current, lines[i + 1], userId);
                    waitingFinish = true;
                }
            }
        }

        FinalizarComprovante(current, comprovantes, userId, descricoes, debitos, creditos, totais);
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

    private static void ExtractDataArrecadacao(ComprovanteData current, string line)
    {
        var dateRegex = new Regex(@"\d{2}/\d{2}/\d{4}");
        var match = dateRegex.Match(line);
        if (match.Success)
        {
            current.DataArrecadacao = match.Value;
        }
    }

    private static void ProcessarLinhaPagamento(string line, List<string> descricoes, List<decimal> totais)
    {
        var valoresHistorico = PdfUtils.ParseLinhaHistorico(line);
        if (valoresHistorico == null) return;

        totais.Add(valoresHistorico.Principal);
        var historico = PdfUtils.ExtrairHistorico(line);
        descricoes.Add(historico);
    }

    private async Task ProcessarTotais(ComprovanteData current, string userId, List<string> descricoes, List<decimal> debitos, List<decimal> creditos, List<decimal> totais)
    {
        var (descricoesAgrupadas, totaisAgrupados) = PdfUtils.AgruparDescricoesEValores(
            new List<string>(descricoes),
            new List<decimal>(totais)
        );

        debitos.AddRange(await _impostoService.MapearDebito(descricoesAgrupadas, userId));
        creditos.AddRange(await _impostoService.MapearCredito(descricoesAgrupadas, userId));

        current.Descricoes = descricoesAgrupadas;
        current.Total = totaisAgrupados;
        current.Debito = new List<decimal>(debitos);
        current.Credito = new List<decimal>(creditos);

        LimparArraysTemporarios(descricoes, debitos, creditos, totais);
    }

    private async Task ProcessarLinhaTotais(ComprovanteData current, string totalLine, string userId)
    {
        if (string.IsNullOrWhiteSpace(totalLine)) return;

        var totalLineTrim = totalLine.Trim();
        var priceMatches = Regex.Matches(totalLineTrim, @"\d{1,3}(?:\.\d{3})*,\d{2}");
        if (priceMatches.Count == 0) return;

        await ProcessarMultaEJuros(current, totalLineTrim, userId);
    }

    private async Task ProcessarMultaEJuros(ComprovanteData current, string totalLineTrim, string userId)
    {
        var parsedValues = PdfUtils.ParseTotaisLinha(totalLineTrim);
        if (parsedValues?.SomaMultaJuros == null) return;

        const string multaDescricao = "PG. MULTA E JUROS XX";
        
        current.Descricoes.Add(multaDescricao);
        current.Debito.AddRange(await _impostoService.MapearDebito(new List<string> { multaDescricao }, userId));
        current.Credito.AddRange(await _impostoService.MapearCredito(new List<string> { multaDescricao }, userId));
        current.Total.Add(parsedValues.SomaMultaJuros);
    }

    private static void FinalizarComprovante(ComprovanteData current, List<ComprovanteData> comprovantes, string userId, List<string> descricoes, List<decimal> debitos, List<decimal> creditos, List<decimal> totais)
    {
        if (string.IsNullOrEmpty(current.DataArrecadacao)) return;

        comprovantes.Add(new ComprovanteData(current));
        
        current = InitializeCurrent();
        LimparArraysTemporarios(descricoes, debitos, creditos, totais);
    }

    private static void LimparArraysTemporarios(List<string> descricoes, List<decimal> debitos, List<decimal> creditos, List<decimal> totais)
    {
        descricoes.Clear();
        debitos.Clear();
        creditos.Clear();
        totais.Clear();
    }
}