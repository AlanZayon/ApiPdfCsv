using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Utils;
using System.Globalization;
using System.Text.Json;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfUseCase
{
    private readonly IPdfProcessorService _pdfProcessor;
    private readonly ILogger _logger;
    private readonly IFileService _fileService;

    public ProcessPdfUseCase(IPdfProcessorService pdfProcessor, ILogger logger, IFileService fileService)
    {
        _pdfProcessor = pdfProcessor;
        _logger = logger;
        _fileService = fileService;
    }

    public async Task<ProcessPdfResult> Execute(ProcessPdfCommand command)
    {
        _logger.Info($"Iniciando processamento do PDF: {command.FilePath}");

        var result = await _pdfProcessor.Process(command.FilePath, command.UserId);

        var outputDir = _fileService.GetUserOutputDir(command.UserSessionId);

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        var outputPath = Path.Combine(outputDir, "PGTO.csv");

        // Dados do PDF
        var dadosPdf = result.Comprovantes
            .SelectMany(comp => comp.Descricoes
                .Select((descricao, index) => new { descricao, index })
                .Where(x => ((comp.Total[x.index] as decimal?) ?? 0m) != 0m)
                .Select(x => new ExcelData
                {
                    DataDeArrecadacao = comp.DataArrecadacao,
                    Debito = comp.Debito[x.index],
                    Credito = comp.Credito[x.index],
                    Total = (comp.Total[x.index] as decimal?) ?? 0m,
                    Descricao = x.descricao,
                    Divisao = 1,
                    Tipo = "PDF"
                })
            ).ToList();

        // Ordenar apenas os dados do PDF por data
        var dadosPdfOrdenados = dadosPdf
            .OrderBy(item => DateTime.ParseExact(item.DataDeArrecadacao, "dd/MM/yyyy", CultureInfo.InvariantCulture))
            .ToList();

        _logger.Info($"Processadas {dadosPdfOrdenados.Count} linhas do PDF");

        // Lista final que conterá todos os dados
        var todosDados = new List<ExcelData>();

        // Adicionar primeiro o bloco do PDF ordenado
        todosDados.AddRange(dadosPdfOrdenados);

        // NOVO: Adicionar pro labore se foi enviado (como segundo bloco)
        if (command.ProLaboreAno.HasValue && command.ProLaboreValor.HasValue)
        {
            _logger.Info($"Adicionando pro labore - Ano: {command.ProLaboreAno}, Valor: {command.ProLaboreValor}");

            var linhasProLabore = ProLaboreGenerator.GerarLinhasProLabore(
                command.ProLaboreAno.Value,
                command.ProLaboreValor.Value,
                188,
                5
            );

            // Ordenar as linhas de pro labore por data
            var linhasProLaboreOrdenadas = linhasProLabore
                .OrderBy(l => DateTime.ParseExact(l.DataDeArrecadacao, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();

            foreach (var linha in linhasProLaboreOrdenadas)
            {
                linha.Tipo = "PROLABORE";
            }

            // Adicionar o bloco de pro labore DEPOIS do bloco do PDF
            todosDados.AddRange(linhasProLaboreOrdenadas);

            _logger.Info($"Adicionadas {linhasProLaboreOrdenadas.Count} linhas de pro labore");
        }

        // Gerar Excel mantendo a ordem dos blocos (PDF primeiro, depois pro labore)
        ExcelGenerator.Generate(todosDados, outputPath, manterOrdemOriginal: true);

        _logger.Info($"Excel gerado com sucesso: {outputPath}");

        return new ProcessPdfResult("Processamento concluído", outputPath);
    }
}