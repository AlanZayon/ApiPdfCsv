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
using System.Globalization;
using System.Text.Json;

namespace ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;

public class ProcessOfxCommand
{
    public string FilePath { get; }
    public string CNPJ { get; } = string.Empty;
    public string? CodigoBanco { get; set; }
    public string UserId { get; }

    public ProcessOfxCommand(string filePath, string cnpj, string userId, string? codigoBanco = null)
    {
        FilePath = filePath;
        UserId = userId;
        CNPJ = cnpj;
        CodigoBanco = codigoBanco;
    }
}

public record Transacao
{
    public string Descricao { get; set; } = string.Empty;
    public List<string> Datas { get; set; } = new List<string>();
    public List<decimal> Valores { get; set; } = new List<decimal>();
    public List<int>? CodigosBanco { get; set; }
    public string CreditoError { get; set; } = "";
    public bool CreditoLocked { get; set; } = false;
    public string DebitoError { get; set; } = "";
    public bool DebitoLocked { get; set; } = false;
    public List<int> CodigosDebito { get; set; } = new List<int>();
    public List<int> CodigosCredito { get; set; } = new List<int>();
    public string? TipoValor { get; set; }

}

public record ClassificacaoTransacao
{
    public string Descricao { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public int CodigoDebito { get; set; }
    public int CodigoCredito { get; set; }
    public int CodigoBanco { get; set; }
    public bool IsClassificacaoIndividual { get; set; }
}

public record ProcessOfxResult(
    string Message,
    List<Transacao> TransacoesClassificadas,
    List<Transacao>? TransacoesPendentes,
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
            codigosBanco = (await _termoRepository.BuscarCodigosBancoPorCnpjAsync(
                command.CNPJ,
                command.UserId,
                command.CodigoBanco)).ToList();

            _logger.Info($"Códigos de banco encontrados para o CNPJ {command.CNPJ}: código(s) de banco {codigosBanco.Count}, valor recebido do CodigoBanco: '{command.CodigoBanco}'");

            if (!codigosBanco.Any())
            {
                if (int.TryParse(command.CodigoBanco, out var codigoBancoUsuario))
                {
                    codigosBanco.Add(codigoBancoUsuario);
                    _logger.Warn($"Nenhum código de banco encontrado para o CNPJ: {command.CNPJ}. Usando o código informado: {codigoBancoUsuario}");
                }
                else
                {
                    _logger.Error($"Falha ao converter CodigoBanco '{command.CodigoBanco}' para inteiro");
                }
            }

            _logger.Info($"Encontrados {codigosBanco.Count} código(s) de banco para o CNPJ: {command.CNPJ}");
        }

        var result = await _ofxProcessor.Process(command.FilePath, command.UserId);
        var outputDir = _fileService.GetOutputDir();
        var transacoesClassificadas = new List<Transacao>();
        var transacoesPendentes = new List<Transacao>();

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, "EXTRATO.csv");

        var transacoesPendentesAgrupadas = new Dictionary<string, Transacao>(StringComparer.InvariantCultureIgnoreCase);
        var transacoesClassificadasAgrupadas = new Dictionary<string, Transacao>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var trans in result.Transacoes)
        {
            var termoEspecial = await _termoRepository.BuscarPorTermoUsuarioCnpjEBancoETipoAsync(
                trans.Descricao,
                command.UserId,
                command.CNPJ,
                (codigosBanco ?? new List<int?>()).FirstOrDefault(),
                trans.Valor >= 0);

            if (termoEspecial != null)
            {
                var chaveClassificada = $"{trans.Descricao}|{(trans.Valor >= 0 ? "POSITIVO" : "NEGATIVO")}";

                if (!transacoesClassificadasAgrupadas.ContainsKey(chaveClassificada))
                {
                    transacoesClassificadasAgrupadas[chaveClassificada] = new Transacao
                    {
                        Descricao = trans.Descricao,
                        TipoValor = trans.Valor >= 0 ? "POSITIVO" : "NEGATIVO"
                    };
                }

                var transClassificada = transacoesClassificadasAgrupadas[chaveClassificada];

                transClassificada.Datas.Add(trans.DataTransacao);
                transClassificada.Valores.Add(trans.Valor);

                if (termoEspecial.CodigoDebito != 0) transClassificada.CodigosDebito.Add(termoEspecial.CodigoDebito);
                if (termoEspecial.CodigoCredito != 0) transClassificada.CodigosCredito.Add(termoEspecial.CodigoCredito);
                transClassificada.CodigosBanco ??= new List<int>();
                if (termoEspecial.CodigoBanco.HasValue
                    && !transClassificada.CodigosBanco.Contains(termoEspecial.CodigoBanco.Value))
                {
                    transClassificada.CodigosBanco.Add(termoEspecial.CodigoBanco.Value);
                }
            }
            else
            {
                var chavePendente = $"{trans.Descricao}|{(trans.Valor >= 0 ? "POSITIVO" : "NEGATIVO")}";

                if (!transacoesPendentesAgrupadas.ContainsKey(chavePendente))
                {
                    transacoesPendentesAgrupadas[chavePendente] = new Transacao
                    {
                        Descricao = trans.Descricao,
                        CodigosBanco = codigosBanco?
                            .Where(x => x.HasValue)
                            .Select(x => x.GetValueOrDefault())
                            .ToList() ?? new List<int>(),
                        TipoValor = trans.Valor >= 0 ? "POSITIVO" : "NEGATIVO"
                    };
                }

                transacoesPendentesAgrupadas[chavePendente].Datas.Add(trans.DataTransacao);
                transacoesPendentesAgrupadas[chavePendente].Valores.Add(trans.Valor);
                var transacao = transacoesPendentesAgrupadas[chavePendente];
                transacao.CodigosBanco ??= new List<int>();

                if (codigosBanco != null && codigosBanco.Any())
                {
                    foreach (var codigo in codigosBanco.Distinct())
                    {
                        if (codigo.HasValue && !transacao.CodigosBanco.Contains(codigo.Value))
                            transacao.CodigosBanco.Add(codigo.Value);
                    }
                }
            }
        }

        transacoesPendentes = transacoesPendentesAgrupadas.Values.ToList();
        transacoesClassificadas = transacoesClassificadasAgrupadas.Values.ToList();

        if (transacoesPendentes.Any())
        {
            return new ProcessOfxResult(
                "Processamento parcial - há transações pendentes de classificação",
                transacoesClassificadas,
                transacoesPendentes
            );
        }

        return new ProcessOfxResult(
            "Processamento OFX concluído",
            transacoesClassificadas,
            null,
            outputPath
        );
    }

    public async Task<ProcessOfxResultFinalizado> FinalizarProcessamento(
        List<Transacao> transacoesClassificadas,
        List<ClassificacaoTransacao> classificacoes,
        List<Transacao> transacoesPendentes,
        string userId,
        string cnpj)
    {
        var classificacoesParaSalvar = classificacoes
            .Where(c => !c.IsClassificacaoIndividual)
            .ToList();

        var descricoesProcessadas = new HashSet<string>();
        foreach (var classificacao in classificacoesParaSalvar)
        {
            var chaveClassificacao = $"{classificacao.Descricao}|{(classificacao.Valor >= 0 ? "POSITIVO" : "NEGATIVO")}";
            if (descricoesProcessadas.Add(chaveClassificacao))
            {
                var termoExistente = await _termoRepository.BuscarPorTermoUsuarioCnpjEBancoETipoAsync(
                    classificacao.Descricao,
                    userId,
                    cnpj,
                    classificacao.CodigoBanco,
                    classificacao.Valor >= 0);

                if (termoExistente == null)
                {
                    var novoTermo = new TermoEspecial
                    {
                        Termo = classificacao.Descricao,
                        UserId = userId,
                        CodigoDebito = classificacao.CodigoDebito,
                        CodigoCredito = classificacao.CodigoCredito,
                        CNPJ = cnpj,
                        CodigoBanco = classificacao.CodigoBanco,
                        TipoValor = classificacao.Valor >= 0
                    };

                    await _termoRepository.AdicionarAsync(novoTermo);
                }
                else
                {
                    bool precisaAtualizar = false;

                    if (termoExistente.CodigoDebito != classificacao.CodigoDebito)
                    {
                        termoExistente.CodigoDebito = classificacao.CodigoDebito;
                        precisaAtualizar = true;
                    }

                    if (termoExistente.CodigoCredito != classificacao.CodigoCredito)
                    {
                        termoExistente.CodigoCredito = classificacao.CodigoCredito;
                        precisaAtualizar = true;
                    }

                    if (termoExistente.CodigoBanco != classificacao.CodigoBanco)
                    {
                        termoExistente.CodigoBanco = classificacao.CodigoBanco;
                        precisaAtualizar = true;
                    }

                    if (precisaAtualizar)
                    {
                        await _termoRepository.AtualizarAsync(termoExistente);
                    }
                }
            }
        }

        var classificacoesAtualizadas = await BuscarClassificacoesAtualizadas(
            classificacoes, userId, cnpj);

        var todasTransacoes = ConverterTransacoesComClassificacoesAtualizadas(
            transacoesClassificadas, classificacoesAtualizadas, userId);

        if (transacoesPendentes?.Count > 0)
        {
            ProcessarTransacoesPendentes(transacoesPendentes, classificacoesAtualizadas, todasTransacoes);
        }

        var outputDir = _fileService.GetOutputDir();
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        var outputPath = Path.Combine(outputDir, "EXTRATO.csv");

        ExcelGenerator.Generate(todasTransacoes, outputPath);

        return new ProcessOfxResultFinalizado(
            "Processamento finalizado com sucesso",
            outputPath
        );
    }

    private List<ExcelData> ConverterTransacoesComClassificacoesAtualizadas(
            List<Transacao> transacoesClassificadas,
            List<ClassificacaoTransacao> classificacoesAtualizadas,
            string userId)
    {
        var todasTransacoes = new List<ExcelData>();

        var classificacaoDict = classificacoesAtualizadas
            .GroupBy(c => $"{c.Descricao}|{c.Data}|{c.Valor}")
            .ToDictionary(g => g.Key, g => g.ToList());


        foreach (var transacao in transacoesClassificadas)
        {
            for (int i = 0; i < transacao.Datas.Count; i++)
            {
                var data = transacao.Datas[i];
                var valor = transacao.Valores[i];
                var chave = $"{transacao.Descricao}|{data}|{valor}";

                if (classificacaoDict.TryGetValue(chave, out var listaClassificacoes) && listaClassificacoes.Any())
                {
                    var classificacaoAtualizada = listaClassificacoes.First();
                    listaClassificacoes.RemoveAt(0);

                    todasTransacoes.Add(new ExcelData
                    {
                        DataDeArrecadacao = data,
                        Debito = classificacaoAtualizada.CodigoDebito,
                        Credito = classificacaoAtualizada.CodigoCredito,
                        Total = valor,
                        Descricao = classificacaoAtualizada.Descricao,
                        Divisao = 1,
                        CodigoBanco = classificacaoAtualizada.CodigoBanco
                    });
                }
                else
                {
                    var classificacaoFallback = classificacoesAtualizadas
                        .Where(c => !c.IsClassificacaoIndividual)
                        .FirstOrDefault(c =>
                            string.Equals(c.Descricao, transacao.Descricao, StringComparison.InvariantCultureIgnoreCase) &&
                            ((valor >= 0 && c.Valor >= 0) || (valor < 0 && c.Valor < 0)));

                    if (classificacaoFallback == null)
                    {
                        classificacaoFallback = classificacoesAtualizadas
                            .Where(c => !c.IsClassificacaoIndividual)
                            .FirstOrDefault(c => string.Equals(c.Descricao, transacao.Descricao, StringComparison.InvariantCultureIgnoreCase));
                    }

                    if (classificacaoFallback != null)
                    {
                        todasTransacoes.Add(new ExcelData
                        {
                            DataDeArrecadacao = data,
                            Debito = classificacaoFallback.CodigoDebito,
                            Credito = classificacaoFallback.CodigoCredito,
                            Total = valor,
                            Descricao = classificacaoFallback.Descricao,
                            Divisao = 1,
                            CodigoBanco = classificacaoFallback.CodigoBanco
                        });
                    }
                    else
                    {
                        todasTransacoes.Add(new ExcelData
                        {
                            DataDeArrecadacao = data,
                            Debito = transacao.CodigosDebito[i],
                            Credito = transacao.CodigosCredito[i],
                            Total = valor,
                            Descricao = transacao.Descricao,
                            Divisao = 1,
                            CodigoBanco = (transacao.CodigosBanco != null && transacao.CodigosBanco.Count > i)
                                ? transacao.CodigosBanco[i]
                                : (transacao.CodigosBanco != null && transacao.CodigosBanco.Count > 0
                                    ? transacao.CodigosBanco[0]
                                    : 0)
                        });
                    }
                }
            }
        }

        return todasTransacoes;
    }

    private void ProcessarTransacoesPendentes(
            List<Transacao> transacoesPendentes,
            List<ClassificacaoTransacao> classificacoesAtualizadas,
            List<ExcelData> todasTransacoes)
    {
        var classificacaoDict = classificacoesAtualizadas
            .GroupBy(c => $"{c.Descricao}|{c.Data}|{c.Valor}")
            .ToDictionary(g => g.Key, g => g.ToList());


        foreach (var transacaoPendente in transacoesPendentes)
        {
            for (int i = 0; i < transacaoPendente.Datas.Count; i++)
            {
                var data = transacaoPendente.Datas[i];
                var valor = transacaoPendente.Valores[i];
                var chave = $"{transacaoPendente.Descricao}|{data}|{valor}";

                if (classificacaoDict.TryGetValue(chave, out var listaClassificacoes))
                {
                    var classificacaoAtualizada = listaClassificacoes.First();
                    listaClassificacoes.RemoveAt(0);

                    todasTransacoes.Add(new ExcelData
                    {
                        DataDeArrecadacao = data,
                        Debito = classificacaoAtualizada.CodigoDebito,
                        Credito = classificacaoAtualizada.CodigoCredito,
                        Total = valor,
                        Descricao = classificacaoAtualizada.Descricao,
                        Divisao = 1,
                        CodigoBanco = classificacaoAtualizada.CodigoBanco
                    });
                }

                else
                {
                    var classificacaoFallback = classificacoesAtualizadas.FirstOrDefault(
                        c => string.Equals(c.Descricao, transacaoPendente.Descricao, StringComparison.InvariantCultureIgnoreCase));

                    if (classificacaoFallback != null)
                    {
                        todasTransacoes.Add(new ExcelData
                        {
                            DataDeArrecadacao = data,
                            Debito = classificacaoFallback.CodigoDebito,
                            Credito = classificacaoFallback.CodigoCredito,
                            Total = valor,
                            Descricao = classificacaoFallback.Descricao,
                            Divisao = 1,
                            CodigoBanco = classificacaoFallback.CodigoBanco
                        });
                    }
                }
            }
        }
    }

    private async Task<List<ClassificacaoTransacao>> BuscarClassificacoesAtualizadas(
        List<ClassificacaoTransacao> classificacoesOriginais,
        string userId,
        string cnpj)
    {
        var classificacoesAtualizadas = new List<ClassificacaoTransacao>();

        foreach (var classificacao in classificacoesOriginais)
        {
            if (classificacao.IsClassificacaoIndividual)
            {
                classificacoesAtualizadas.Add(classificacao);
                continue;
            }

            var termoAtualizado = await _termoRepository.BuscarPorTermoUsuarioCnpjEBancoETipoAsync(
                classificacao.Descricao,
                userId,
                cnpj,
                classificacao.CodigoBanco,
                classificacao.Valor >= 0);

            if (termoAtualizado != null)
            {
                classificacoesAtualizadas.Add(new ClassificacaoTransacao
                {
                    Descricao = classificacao.Descricao,
                    Data = classificacao.Data,
                    Valor = classificacao.Valor,
                    CodigoDebito = termoAtualizado.CodigoDebito,
                    CodigoCredito = termoAtualizado.CodigoCredito,
                    CodigoBanco = termoAtualizado.CodigoBanco ?? 0,
                    IsClassificacaoIndividual = false
                });
            }
            else
            {
                classificacoesAtualizadas.Add(classificacao);
            }
        }

        return classificacoesAtualizadas;
    }
}