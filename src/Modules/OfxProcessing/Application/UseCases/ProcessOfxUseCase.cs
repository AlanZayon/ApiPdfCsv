using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Utils;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;
using System.Collections.Generic;
using System.Linq;

namespace ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;

public class ProcessOfxCommand
{
    public string FilePath { get; }
    public string CNPJ { get; } = string.Empty;
    public string UserId { get; }

    public ProcessOfxCommand(string filePath, string cnpj, string userId)
    {
        FilePath = filePath;
        UserId = userId;
        CNPJ = cnpj;
    }
}

public record TransacaoPendente
{
    public string Descricao { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public List<int>? CodigosBanco { get; set; }
}

public record ClassificacaoTransacao
{
    public string Descricao { get; set; } = string.Empty;
    public int CodigoDebito { get; set; }
    public int CodigoCredito { get; set; }
    public int CodigoBanco { get; set; }
}

public record ProcessOfxResult(
    string Message,
    List<ExcelData> TransacoesClassificadas,
    List<TransacaoPendente>? TransacoesPendentes = null,
    string? OutputPath = null
);

public record ProcessOfxResultFinalizado(
    string Message,
    string OutputPath
);

public class ProcessOfxUseCase
{
    private readonly IOfxProcessorService _ofxProcessor;
    private readonly ILogger _logger;
    private readonly IFileService _fileService;
    private readonly ITermoEspecialRepository _termoRepository;

    public ProcessOfxUseCase(
        IOfxProcessorService ofxProcessor,
        ILogger logger,
        IFileService fileService,
        ITermoEspecialRepository termoRepository)
    {
        _ofxProcessor = ofxProcessor;
        _logger = logger;
        _fileService = fileService;
        _termoRepository = termoRepository;
    }

    public async Task<ProcessOfxResult> Execute(ProcessOfxCommand command)
    {
        _logger.Info($"Iniciando processamento do OFX: {command.FilePath}");
        _logger.Info($"CNPJ informado: {command.CNPJ}");

        List<int?> codigosBanco = new List<int?>();
        if (!string.IsNullOrEmpty(command.CNPJ))
        {
           codigosBanco = (await _termoRepository.BuscarCodigosBancoPorCnpjAsync(command.CNPJ, command.UserId)).ToList();
            _logger.Info($"Encontrados {codigosBanco.Count} código(s) de banco para o CNPJ: {command.CNPJ}");
        }

        var result = await _ofxProcessor.Process(command.FilePath, command.UserId);
        var outputDir = _fileService.GetOutputDir();
        var transacoesClassificadas = new List<ExcelData>();
        var transacoesPendentes = new List<TransacaoPendente>();

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, "EXTRATO.csv");

        var descricoesProcessadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var descricoesPendentes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var trans in result.Transacoes)
        {
            if (descricoesProcessadas.Contains(trans.Descricao))
            {
                continue;
            }

            var termoEspecial = await _termoRepository.BuscarPorTermoEUsuarioAsync(trans.Descricao, command.UserId);

            if (termoEspecial != null)
            {
                transacoesClassificadas.Add(new ExcelData
                {
                    DataDeArrecadacao = trans.DataTransacao,
                    Debito = termoEspecial.CodigoDebito,
                    Credito = termoEspecial.CodigoCredito,
                    Total = trans.Valor,
                    Descricao = trans.Descricao,
                    Divisao = 1,
                    CodigoBanco = termoEspecial.CodigoBanco,

                });

                descricoesProcessadas.Add(trans.Descricao);
            }
            else
            {
                if (!descricoesPendentes.Contains(trans.Descricao))
                {
                    var transacaoPendente = new TransacaoPendente
                    {
                        Descricao = trans.Descricao,
                        Data = trans.DataTransacao,
                        Valor = trans.Valor
                    };

                    if (codigosBanco != null && codigosBanco.Any())
                    {
                        transacaoPendente.CodigosBanco = codigosBanco
                            .Where(x => x.HasValue)
                            .Select(x => x.GetValueOrDefault())
                            .ToList();
                    }

                    transacoesPendentes.Add(transacaoPendente);
                    descricoesPendentes.Add(trans.Descricao);
                }
            }
        }

        if (transacoesPendentes.Any())
        {

            return new ProcessOfxResult(
                "Processamento parcial - há transações pendentes de classificação",
                transacoesClassificadas,
                transacoesPendentes
            );
        }

        ExcelGenerator.Generate(transacoesClassificadas, outputPath);


        return new ProcessOfxResult(
            "Processamento OFX concluído",
            transacoesClassificadas,
            null,
            outputPath
        );
    }

    public async Task<ProcessOfxResultFinalizado> FinalizarProcessamento(
        List<ExcelData> transacoesClassificadas,
        List<ClassificacaoTransacao> classificacoes,
        List<TransacaoPendente> transacoesPendentes,
        string userId,
        string cnpj)
    {
        _logger.Info($"CNPJ para salvamento: {cnpj}");

        var todasTransacoes = new List<ExcelData>(transacoesClassificadas);
        var outputDir = _fileService.GetOutputDir();

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, "EXTRATO.csv");

        var classificacoesProcessadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var classificacao in classificacoes)
        {

            var transacaoPendente = transacoesPendentes.FirstOrDefault(
                tp => tp.Descricao.Equals(classificacao.Descricao, StringComparison.OrdinalIgnoreCase));

            if (transacaoPendente != null)
            {
                var termoExistente = await _termoRepository.BuscarPorTermoEUsuarioAsync(
                    classificacao.Descricao, userId);

                if (termoExistente == null)
                {
                    var novoTermo = new TermoEspecial
                    {
                        Termo = classificacao.Descricao,
                        UserId = userId,
                        CodigoDebito = classificacao.CodigoDebito,
                        CodigoCredito = classificacao.CodigoCredito,
                        CNPJ = cnpj,
                        CodigoBanco = classificacao.CodigoBanco
                    };

                    await _termoRepository.AdicionarAsync(novoTermo);
                    _logger.Info($"Novo termo salvo no banco para o usuário {userId} e CNPJ {cnpj}: {classificacao.Descricao}");
                }

                todasTransacoes.Add(new ExcelData
                {
                    DataDeArrecadacao = transacaoPendente.Data,
                    Debito = classificacao.CodigoDebito,
                    Credito = classificacao.CodigoCredito,
                    Total = transacaoPendente.Valor,
                    Descricao = classificacao.Descricao,
                    Divisao = 1
                });

                classificacoesProcessadas.Add(classificacao.Descricao);
            }
        }

        ExcelGenerator.Generate(todasTransacoes, outputPath);

        return new ProcessOfxResultFinalizado(
            "Processamento finalizado com sucesso",
            outputPath
        );
    }
}