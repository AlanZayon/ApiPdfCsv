using UglyToad.PdfPig;
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
    private int? _clienteId;

    public PdfProcessorService(ILogger logger, IImpostoService impostoService)
    {
        _impostoService = impostoService;
        _logger = logger;
    }

    public async Task<ProcessedPdfData> Process(string filePath, string userId, int? clienteId = null)
    {
        _clienteId = clienteId;
        _logger.Info($"Processing PDF file: {filePath}");
        
        var current = InitializeCurrent();
        var descricoes = new List<string>();
        var debitos = new List<decimal>();
        var creditos = new List<decimal>();
        var totais = new List<decimal>();
        var comprovantes = new List<ComprovanteData>();
        var collectingDescricoes = false;
        var waitingFinish = false;

        using var document = PdfDocument.Open(filePath);

        foreach (var page in document.GetPages())
        {
            var lines = PdfTextExtractor.ExtractLines(page);

            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Contains("Agência Estabelecimento Valor Reservado/Restituído Referência"))
                {
                    TryExtractDataArrecadacao(current, lines, i);

                    if (waitingFinish)
                    {
                        current = FinalizarComprovante(current, comprovantes, userId, descricoes, debitos, creditos, totais);
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

                        var totalValuesLine = ResolveTotaisValuesLine(line, lines, i);
                        await ProcessarLinhaTotais(current, totalValuesLine, userId);
                        waitingFinish = true;
                    }
                    else if (Regex.IsMatch(line, @"^\d{4}(?=.*[A-Za-z]).*\d{1,3},\d{2}$"))
                    {
                        ProcessarLinhaPagamento(line, descricoes, totais);
                    }
                }
            }
        }

        current = FinalizarComprovante(current, comprovantes, userId, descricoes, debitos, creditos, totais);

        if (comprovantes.Count == 0)
        {
            throw new InvalidDataException(
                "PDF não reconhecido como Documento de Arrecadação. Verifique se o arquivo é um DARF/DAS válido.");
        }

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

    private static void TryExtractDataArrecadacao(ComprovanteData current, IReadOnlyList<string> lines, int index)
    {
        if (!string.IsNullOrEmpty(current.DataArrecadacao))
            return;

        if (index + 1 < lines.Count)
            ExtractDataArrecadacao(current, lines[index + 1]);

        if (!string.IsNullOrEmpty(current.DataArrecadacao))
            return;

        for (var j = index - 1; j >= System.Math.Max(0, index - 3); j--)
        {
            ExtractDataArrecadacao(current, lines[j]);
            if (!string.IsNullOrEmpty(current.DataArrecadacao))
                break;
        }
    }

    private static string ResolveTotaisValuesLine(string line, IReadOnlyList<string> lines, int index)
    {
        if (line.StartsWith("Totais") && line.Length > "Totais".Length)
        {
            var inlineValues = line["Totais".Length..].Trim();
            if (CountPrices(inlineValues) >= 4)
                return inlineValues;
        }

        if (index > 0 && CountPrices(lines[index - 1]) >= 4)
            return lines[index - 1];

        if (index + 1 < lines.Count && CountPrices(lines[index + 1]) >= 4)
            return lines[index + 1];

        return string.Empty;
    }

    private static int CountPrices(string line)
    {
        return Regex.Matches(line, @"\d{1,3}(?:\.\d{3})*,\d{2}").Count;
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

        var (debitosMapeados, creditosMapeados) = await _impostoService.MapearDebitoECredito(descricoesAgrupadas, userId, _clienteId);
        debitos.AddRange(debitosMapeados);
        creditos.AddRange(creditosMapeados);

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
        var (debitosMulta, creditosMulta) = await _impostoService.MapearDebitoECredito(
            new List<string> { multaDescricao },
            userId,
            _clienteId
        );
        current.Debito.AddRange(debitosMulta);
        current.Credito.AddRange(creditosMulta);
        current.Total.Add(parsedValues.SomaMultaJuros);
    }

    private static ComprovanteData FinalizarComprovante(ComprovanteData current, List<ComprovanteData> comprovantes, string userId, List<string> descricoes, List<decimal> debitos, List<decimal> creditos, List<decimal> totais)
    {
        if (string.IsNullOrEmpty(current.DataArrecadacao))
            return current;

        comprovantes.Add(new ComprovanteData(current));
        LimparArraysTemporarios(descricoes, debitos, creditos, totais);
        return InitializeCurrent();
    }

    private static void LimparArraysTemporarios(List<string> descricoes, List<decimal> debitos, List<decimal> creditos, List<decimal> totais)
    {
        descricoes.Clear();
        debitos.Clear();
        creditos.Clear();
        totais.Clear();
    }
}
