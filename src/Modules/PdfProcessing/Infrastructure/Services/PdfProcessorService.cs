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
    private ComprovanteData _current = InitializeCurrent();
    private readonly List<string> _descricoes = new();
    private readonly List<decimal> _debitos = new();
    private readonly List<decimal> _creditos = new();

    private readonly List<decimal> _totais = new();

    public PdfProcessorService(ILogger logger, IImpostoService impostoService)
    {
        _impostoService = impostoService;

        _logger = logger;
    }

    public async Task<ProcessedPdfData> Process(string filePath, string userId)
    {
        return await Task.Run(async () =>
        {
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
                        ExtractDataArrecadacao(lines[i + 1]);
                        if (waitingFinish)
                        {
                            FinalizarComprovante(comprovantes);
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
                            ProcessarTotais(userId).Wait();
                        }
                        else if (Regex.IsMatch(line, @"^\d{4}(?=.*[A-Za-z]).*\d{1,3},\d{2}$"))
                        {
                            ProcessarLinhaPagamento(line);
                        }
                    }

                    if (line == "Totais")
                    {
                        await ProcessarLinhaTotais(lines[i + 1], userId);
                        waitingFinish = true;
                    }
                }
            }

            FinalizarComprovante(comprovantes);
            return new ProcessedPdfData(comprovantes);
        });
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

    private void ExtractDataArrecadacao(string line)
    {
        var dateRegex = new Regex(@"\d{2}/\d{2}/\d{4}");
        var match = dateRegex.Match(line);
        if (match.Success)
        {
            _current.DataArrecadacao = match.Value;
        }
    }

    private void ProcessarLinhaPagamento(string line)
    {
        var valoresHistorico = PdfUtils.ParseLinhaHistorico(line);
        if (valoresHistorico == null) return;

        _totais.Add(valoresHistorico.Principal);
        var historico = PdfUtils.ExtrairHistorico(line);
        _descricoes.Add(historico);
    }

    private async Task ProcessarTotais(string userId)
    {
        var (descricoes, totais) = PdfUtils.AgruparDescricoesEValores(
            new List<string>(_descricoes),
            new List<decimal>(_totais)
        );

        _debitos.AddRange(await _impostoService.MapearDebito(descricoes, userId));
        _creditos.AddRange(await _impostoService.MapearCredito(descricoes, userId));


        _current = new ComprovanteData
        {
            DataArrecadacao = _current.DataArrecadacao,
            Descricoes = descricoes,
            Total = totais,
            Debito = new List<decimal>(_debitos),
            Credito = new List<decimal>(_creditos)
        };

        LimparArraysTemporarios();
    }

    private async Task ProcessarLinhaTotais(string totalLine, string userId)
    {
        if (string.IsNullOrWhiteSpace(totalLine)) return;

        var totalLineTrim = totalLine.Trim();
        var priceMatches = Regex.Matches(totalLineTrim, @"\d{1,3}(?:\.\d{3})*,\d{2}");
        if (priceMatches.Count == 0) return;

        await ProcessarMultaEJuros(totalLineTrim, userId);
    }

    private async Task ProcessarMultaEJuros(string totalLineTrim, string userId)
    {
        var parsedValues = PdfUtils.ParseTotaisLinha(totalLineTrim);
        if (parsedValues?.SomaMultaJuros == null) return;

        _current.Descricoes.Add("PG. MULTA E JUROS XX");
        _current.Debito.AddRange(await _impostoService.MapearDebito(new List<string> { "PG. MULTA E JUROS XX" }, userId));
        _current.Credito.AddRange(await _impostoService.MapearCredito(new List<string> { "PG. MULTA E JUROS XX" }, userId));
        _current.Total.Add(parsedValues.SomaMultaJuros);
    }

    private void FinalizarComprovante(List<ComprovanteData> comprovantes)
    {
        if (string.IsNullOrEmpty(_current.DataArrecadacao)) return;

        comprovantes.Add(new ComprovanteData(_current));
        ResetarEstado();
    }

    private void ResetarEstado()
    {
        _current = InitializeCurrent();
        LimparArraysTemporarios();
    }

    private void LimparArraysTemporarios()
    {
        _descricoes.Clear();
        _debitos.Clear();
        _creditos.Clear();
        _totais.Clear();
    }

    private decimal CalcularCredito() => 5m;
}