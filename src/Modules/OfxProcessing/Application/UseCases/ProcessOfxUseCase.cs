using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Shared.Storage;
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
        public string UserSessionId { get; }


    public ProcessOfxCommand(string filePath, string cnpj, string userId, string? codigoBanco = null, string userSessionId = "")
    {
        FilePath = filePath;
        UserId = userId;
        CNPJ = cnpj;
        CodigoBanco = codigoBanco;
        UserSessionId = userSessionId;
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
    string? OutputFile = null
);

public record ProcessOfxResultFinalizado(
    string Message,
    string OutputFile
);

public class ProcessOfxUseCase
{
    private readonly IOfxProcessorService _ofxProcessor;
    private readonly ILogger _logger;
    private readonly IBlobStorageService _blobStorage;
    private readonly ITermoEspecialRepository _termoRepository;

    public ProcessOfxUseCase(
        IOfxProcessorService ofxProcessor,
        ILogger logger,
        IBlobStorageService blobStorage,
        ITermoEspecialRepository termoRepository)
    {
        _ofxProcessor = ofxProcessor;
        _logger = logger;
        _blobStorage = blobStorage;
        _termoRepository = termoRepository;
    }

    public async Task<ProcessOfxResult> Execute(ProcessOfxCommand command)
    {
        var codigosBanco = await ObterCodigosBanco(command);
        var codigoBancoPadrao = codigosBanco?.FirstOrDefault();

        var result = await _ofxProcessor.Process(command.FilePath, command.UserId);
        const string outputFile = "EXTRATO.csv";

        var termosCache = await CarregarTermosEspeciaisEmCache(
            result.Transacoes,
            command.UserId,
            command.CNPJ,
            codigoBancoPadrao);

        var (transacoesClassificadas, transacoesPendentes) = ProcessarTransacoes(
            result.Transacoes,
            termosCache,
            codigosBanco ?? new List<int?>());

        if (transacoesPendentes.Any())
        {
            return new ProcessOfxResult(
                "Processamento parcial - há transações pendentes de classificação",
                transacoesClassificadas,
                transacoesPendentes
            );
        }

        await SaveOfxCsvAsync(transacoesClassificadas, command.UserId, command.UserSessionId);

        return new ProcessOfxResult(
            "Processamento OFX concluído",
            transacoesClassificadas,
            null,
            outputFile
        );
    }

    private async Task<List<int?>?> ObterCodigosBanco(ProcessOfxCommand command)
    {
        if (string.IsNullOrEmpty(command.CNPJ))
            return new List<int?>();

        var codigosBanco = (await _termoRepository.BuscarCodigosBancoPorCnpjAsync(
            command.CNPJ,
            command.UserId,
            command.CodigoBanco))?.ToList() ?? new List<int?>();

        if (!codigosBanco.Any() && int.TryParse(command.CodigoBanco, out var codigoBancoUsuario))
        {
            codigosBanco.Add(codigoBancoUsuario);
            _logger.Warn($"Nenhum código de banco encontrado para o CNPJ: {command.CNPJ}. Usando o código informado: {codigoBancoUsuario}");
        }

        return codigosBanco.Any() ? codigosBanco : null;
    }

    private async Task<Dictionary<string, TermoEspecial>> CarregarTermosEspeciaisEmCache(
        IEnumerable<OfxTransactionData> transacoes,
        string userId,
        string cnpj,
        int? codigoBanco)
    {
        var todosTermos = await _termoRepository.BuscarTodosTermosRelevantesAsync(
            userId, cnpj, codigoBanco);

        var cache = new Dictionary<string, TermoEspecial>();
        var descricoes = transacoes.Select(t => t.Descricao).Distinct();

        foreach (var descricao in descricoes)
        {
            var descricaoLower = descricao.ToLowerInvariant();

            if (todosTermos.TryGetValue((descricaoLower, true), out var positivo))
            {
                cache[CriarChaveCache(descricao, true)] = positivo;
            }
            if (todosTermos.TryGetValue((descricaoLower, false), out var negativo))
            {
                cache[CriarChaveCache(descricao, false)] = negativo;
            }
        }

        return cache;
    }

    private static string CriarChaveCache(string descricao, bool isPositivo)
        => $"{descricao}|{(isPositivo ? "POS" : "NEG")}";

    private (List<Transacao>, List<Transacao>) ProcessarTransacoes(
        IEnumerable<OfxTransactionData> transacoes,
        Dictionary<string, TermoEspecial> termosCache,
        List<int?> codigosBanco)
    {
        var transacoesPendentesAgrupadas = new Dictionary<string, Transacao>(StringComparer.InvariantCultureIgnoreCase);
        var transacoesClassificadasAgrupadas = new Dictionary<string, Transacao>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var trans in transacoes)
        {
            var isPositivo = trans.Valor >= 0;
            string chaveCache = CriarChaveCache(trans.Descricao, isPositivo);
            termosCache.TryGetValue(chaveCache, out var termoEspecial);

            var tipoValor = isPositivo ? "POSITIVO" : "NEGATIVO";
            string chave = $"{trans.Descricao}|{tipoValor}";

            if (termoEspecial != null)
            {
                if (!transacoesClassificadasAgrupadas.TryGetValue(chave, out var transClassificada))
                {
                    transClassificada = new Transacao
                    {
                        Descricao = trans.Descricao,
                        TipoValor = tipoValor,
                        CodigosBanco = new List<int>()
                    };
                    transacoesClassificadasAgrupadas[chave] = transClassificada;
                }

                transClassificada.Datas.Add(trans.DataTransacao);
                transClassificada.Valores.Add(trans.Valor);

                if (termoEspecial.CodigoDebito != 0)
                    transClassificada.CodigosDebito.Add(termoEspecial.CodigoDebito);
                if (termoEspecial.CodigoCredito != 0)
                    transClassificada.CodigosCredito.Add(termoEspecial.CodigoCredito);

                if (transClassificada.CodigosBanco == null)
                {
                    transClassificada.CodigosBanco = new List<int>();
                }

                if (termoEspecial.CodigoBanco.HasValue && !transClassificada.CodigosBanco.Contains(termoEspecial.CodigoBanco.Value))
                {
                    transClassificada.CodigosBanco.Add(termoEspecial.CodigoBanco.Value);
                }
            }
            else
            {
                if (!transacoesPendentesAgrupadas.TryGetValue(chave, out var transPendente))
                {
                    var codigosBancoLista = codigosBanco?
                        .Where(x => x.HasValue)
                        .Select(x => x!.Value)
                        .Distinct()
                        .ToList() ?? new List<int>();

                    transPendente = new Transacao
                    {
                        Descricao = trans.Descricao,
                        CodigosBanco = codigosBancoLista,
                        TipoValor = tipoValor
                    };
                    transacoesPendentesAgrupadas[chave] = transPendente;
                }

                transPendente.Datas.Add(trans.DataTransacao);
                transPendente.Valores.Add(trans.Valor);
            }
        }

        return (transacoesClassificadasAgrupadas.Values.ToList(),
                transacoesPendentesAgrupadas.Values.ToList());
    }

    public async Task<ProcessOfxResultFinalizado> FinalizarProcessamento(
        List<Transacao> transacoesClassificadas,
        List<ClassificacaoTransacao> classificacoes,
        List<Transacao> transacoesPendentes,
        string userId,
        string cnpj,
        string userSessionId)
    {
        await ProcessarESalvarClassificacoes(classificacoes, userId, cnpj);

        var classificacoesAtualizadas = await BuscarClassificacoesAtualizadas(
            classificacoes, userId, cnpj);

        var todasTransacoes = ConverterTransacoesComClassificacoesAtualizadas(
            transacoesClassificadas, classificacoesAtualizadas);

        if (transacoesPendentes?.Count > 0)
        {
            ProcessarTransacoesPendentes(transacoesPendentes, classificacoesAtualizadas, todasTransacoes);
        }

        if (todasTransacoes.Count == 0 && classificacoesAtualizadas.Count > 0)
        {
            _logger.Warn("Nenhuma transação encontrada via merge — gerando CSV diretamente das classificações enviadas.");
            todasTransacoes = ConverterClassificacoesParaExcel(classificacoesAtualizadas);
        }

        if (todasTransacoes.Count == 0)
        {
            throw new InvalidDataException(
                "Nenhuma transação classificada para gerar o CSV. Verifique se todas as descrições foram classificadas.");
        }

        await SaveOfxCsvFromExcelDataAsync(todasTransacoes, userId, userSessionId);

        return new ProcessOfxResultFinalizado(
            "Processamento finalizado com sucesso",
            "EXTRATO.csv"
        );
    }

    private async Task SaveOfxCsvAsync(List<Transacao> transacoesClassificadas, string userId, string sessionId)
    {
        var excelData = ConverterTransacoesClassificadasParaExcel(transacoesClassificadas);
        await SaveOfxCsvFromExcelDataAsync(excelData, userId, sessionId);
    }

    private async Task SaveOfxCsvFromExcelDataAsync(List<ExcelData> excelData, string userId, string sessionId)
    {
        var bytes = ExcelGenerator.GenerateBytes(excelData);
        await _blobStorage.SaveAsync(userId, sessionId, "EXTRATO.csv", new MemoryStream(bytes));
    }

    private static List<ExcelData> ConverterTransacoesClassificadasParaExcel(List<Transacao> transacoesClassificadas)
    {
        var todasTransacoes = new List<ExcelData>(transacoesClassificadas.Sum(t => t.Datas.Count));

        foreach (var transacao in transacoesClassificadas)
        {
            for (int i = 0; i < transacao.Datas.Count; i++)
            {
                todasTransacoes.Add(CriarExcelDataDaTransacao(
                    transacao,
                    i,
                    transacao.Datas[i],
                    transacao.Valores[i]));
            }
        }

        return todasTransacoes;
    }

    private async Task ProcessarESalvarClassificacoes(
        List<ClassificacaoTransacao> classificacoes,
        string userId,
        string cnpj)
    {
        var classificacoesParaSalvar = classificacoes
            .Where(c => !c.IsClassificacaoIndividual)
            .ToList();

        var classificacoesUnicas = classificacoesParaSalvar
            .GroupBy(c => $"{c.Descricao}|{(c.Valor >= 0 ? "POSITIVO" : "NEGATIVO")}")
            .Select(g => g.First())
            .ToList();

        var termosParaAdicionar = new List<TermoEspecial>();
        var termosParaAtualizar = new List<TermoEspecial>();

        var termosExistentes = await _termoRepository.BuscarTodosPorUsuarioCnpjAsync(userId, cnpj);

        foreach (var classificacao in classificacoesUnicas)
        {
            var chave = (classificacao.Descricao, (int?)classificacao.CodigoBanco, classificacao.Valor >= 0);
            termosExistentes.TryGetValue(chave, out var termoExistente);

            if (termoExistente == null)
            {
                termosParaAdicionar.Add(new TermoEspecial
                {
                    Termo = classificacao.Descricao,
                    UserId = userId,
                    CodigoDebito = classificacao.CodigoDebito,
                    CodigoCredito = classificacao.CodigoCredito,
                    CNPJ = cnpj,
                    CodigoBanco = classificacao.CodigoBanco,
                    TipoValor = classificacao.Valor >= 0
                });
            }
            else if (PrecisaAtualizar(termoExistente, classificacao))
            {
                termoExistente.CodigoDebito = classificacao.CodigoDebito;
                termoExistente.CodigoCredito = classificacao.CodigoCredito;
                termoExistente.CodigoBanco = classificacao.CodigoBanco;
                termosParaAtualizar.Add(termoExistente);
            }
        }

        var todosTermos = termosParaAdicionar.Concat(termosParaAtualizar).ToList();
        if (todosTermos.Any())
        {
            await _termoRepository.AdicionarOuAtualizarEmLoteAsync(todosTermos);
        }

    }

    private static bool PrecisaAtualizar(TermoEspecial termo, ClassificacaoTransacao classificacao)
    {
        return termo.CodigoDebito != classificacao.CodigoDebito ||
               termo.CodigoCredito != classificacao.CodigoCredito ||
               termo.CodigoBanco != classificacao.CodigoBanco;
    }

    private static string CriarChaveTransacao(string descricao, string data, decimal valor)
        => $"{descricao}|{data}|{valor.ToString("F2", CultureInfo.InvariantCulture)}";

    private static List<ExcelData> ConverterClassificacoesParaExcel(List<ClassificacaoTransacao> classificacoes)
    {
        return classificacoes
            .Where(c => !string.IsNullOrWhiteSpace(c.Data) && !string.IsNullOrWhiteSpace(c.Descricao))
            .Select(c => CriarExcelData(c.Data, c.Valor, c))
            .ToList();
    }

    private List<ExcelData> ConverterTransacoesComClassificacoesAtualizadas(
        List<Transacao> transacoesClassificadas,
        List<ClassificacaoTransacao> classificacoesAtualizadas)
    {
        var todasTransacoes = new List<ExcelData>(transacoesClassificadas.Sum(t => t.Datas.Count));

        var classificacaoDict = classificacoesAtualizadas
            .GroupBy(c => CriarChaveTransacao(c.Descricao, c.Data, c.Valor))
            .ToDictionary(g => g.Key, g => new Queue<ClassificacaoTransacao>(g));

        var classificacaoFallbackDict = classificacoesAtualizadas
            .Where(c => !c.IsClassificacaoIndividual)
            .GroupBy(c => c.Descricao.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var transacao in transacoesClassificadas)
        {
            for (int i = 0; i < transacao.Datas.Count; i++)
            {
                var data = transacao.Datas[i];
                var valor = transacao.Valores[i];
                var chave = CriarChaveTransacao(transacao.Descricao, data, valor);

                ClassificacaoTransacao? classificacao = null;

                if (classificacaoDict.TryGetValue(chave, out var queue) && queue.Count > 0)
                {
                    classificacao = queue.Dequeue();
                }
                else if (classificacaoFallbackDict.TryGetValue(transacao.Descricao.ToLowerInvariant(), out var fallbacks))
                {
                    classificacao = fallbacks.FirstOrDefault(c =>
                        (valor >= 0 && c.Valor >= 0) || (valor < 0 && c.Valor < 0))
                        ?? fallbacks.FirstOrDefault();
                }

                if (classificacao != null)
                {
                    todasTransacoes.Add(CriarExcelData(data, valor, classificacao));
                }
                else
                {
                    todasTransacoes.Add(CriarExcelDataDaTransacao(transacao, i, data, valor));
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
            .GroupBy(c => CriarChaveTransacao(c.Descricao, c.Data, c.Valor))
            .ToDictionary(g => g.Key, g => new Queue<ClassificacaoTransacao>(g));

        var classificacaoFallbackDict = classificacoesAtualizadas
            .GroupBy(c => c.Descricao.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var transacaoPendente in transacoesPendentes)
        {
            for (int i = 0; i < transacaoPendente.Datas.Count; i++)
            {
                var data = transacaoPendente.Datas[i];
                var valor = transacaoPendente.Valores[i];
                var chave = CriarChaveTransacao(transacaoPendente.Descricao, data, valor);

                ClassificacaoTransacao? classificacao = null;

                if (classificacaoDict.TryGetValue(chave, out var queue) && queue.Count > 0)
                {
                    classificacao = queue.Dequeue();
                }
                else if (classificacaoFallbackDict.TryGetValue(transacaoPendente.Descricao.ToLowerInvariant(), out var fallback))
                {
                    classificacao = fallback;
                }

                if (classificacao != null)
                {
                    todasTransacoes.Add(CriarExcelData(data, valor, classificacao));
                }
            }
        }
    }

    private static ExcelData CriarExcelData(string data, decimal valor, ClassificacaoTransacao classificacao)
    {
        return new ExcelData
        {
            DataDeArrecadacao = data,
            Debito = classificacao.CodigoDebito,
            Credito = classificacao.CodigoCredito,
            Total = valor,
            Descricao = classificacao.Descricao,
            Divisao = 1,
            CodigoBanco = classificacao.CodigoBanco
        };
    }

    private static ExcelData CriarExcelDataDaTransacao(Transacao transacao, int index, string data, decimal valor)
    {
        var codigosBanco = transacao.CodigosBanco ?? new List<int>();
        var codigoBanco = (codigosBanco.Count > index)
            ? codigosBanco[index]
            : codigosBanco.FirstOrDefault();

        return new ExcelData
        {
            DataDeArrecadacao = data,
            Debito = transacao.CodigosDebito.ElementAtOrDefault(index),
            Credito = transacao.CodigosCredito.ElementAtOrDefault(index),
            Total = valor,
            Descricao = transacao.Descricao,
            Divisao = 1,
            CodigoBanco = codigoBanco
        };
    }

    private async Task<List<ClassificacaoTransacao>> BuscarClassificacoesAtualizadas(
        List<ClassificacaoTransacao> classificacoesOriginais,
        string userId,
        string cnpj)
    {
        var classificacoesAtualizadas = new List<ClassificacaoTransacao>(classificacoesOriginais.Count);

        var individuais = classificacoesOriginais.Where(c => c.IsClassificacaoIndividual).ToList();
        var naoIndividuais = classificacoesOriginais.Where(c => !c.IsClassificacaoIndividual).ToList();

        classificacoesAtualizadas.AddRange(individuais);

        if (naoIndividuais.Count == 0)
            return classificacoesAtualizadas;

        var termosExistentes = await _termoRepository.BuscarTodosPorUsuarioCnpjAsync(userId, cnpj);

        foreach (var classificacao in naoIndividuais)
        {
            var chave = (classificacao.Descricao, (int?)classificacao.CodigoBanco, classificacao.Valor >= 0);
            termosExistentes.TryGetValue(chave, out var termoAtualizado);

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