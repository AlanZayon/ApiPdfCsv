using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Storage;
using ApiPdfCsv.Shared.Utils;
using System.Globalization;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfUseCase
{
    private readonly IPdfProcessorService _pdfProcessor;
    private readonly ILogger _logger;
    private readonly IBlobStorageService _blobStorage;

    public ProcessPdfUseCase(IPdfProcessorService pdfProcessor, ILogger logger, IBlobStorageService blobStorage)
    {
        _pdfProcessor = pdfProcessor;
        _logger = logger;
        _blobStorage = blobStorage;
    }

    public async Task<ProcessPdfResult> Execute(ProcessPdfCommand command)
    {
        _logger.Info($"Iniciando processamento do PDF: {command.FilePath}");

        var result = await _pdfProcessor.Process(command.FilePath, command.UserId);

        const string outputFile = "PGTO.csv";

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

        var dadosPdfOrdenados = dadosPdf
            .OrderBy(item => DateTime.ParseExact(item.DataDeArrecadacao, "dd/MM/yyyy", CultureInfo.InvariantCulture))
            .ToList();

        _logger.Info($"Processadas {dadosPdfOrdenados.Count} linhas do PDF");

        var todosDados = new List<ExcelData>();
        todosDados.AddRange(dadosPdfOrdenados);

        if (command.ProLaboreAno.HasValue && command.ProLaboreValor.HasValue)
        {
            _logger.Info($"Adicionando pro labore - Ano: {command.ProLaboreAno}, Valor: {command.ProLaboreValor}");

            var linhasProLabore = ProLaboreGenerator.GerarLinhasProLabore(
                command.ProLaboreAno.Value,
                command.ProLaboreValor.Value,
                188,
                5
            );

            var linhasProLaboreOrdenadas = linhasProLabore
                .OrderBy(l => DateTime.ParseExact(l.DataDeArrecadacao, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();

            foreach (var linha in linhasProLaboreOrdenadas)
            {
                linha.Tipo = "PROLABORE";
            }

            todosDados.AddRange(linhasProLaboreOrdenadas);
            _logger.Info($"Adicionadas {linhasProLaboreOrdenadas.Count} linhas de pro labore");
        }

        var bytes = ExcelGenerator.GenerateBytes(todosDados, manterOrdemOriginal: true);
        await _blobStorage.SaveAsync(
            command.UserId,
            command.UserSessionId,
            outputFile,
            new MemoryStream(bytes));

        _logger.Info($"CSV gerado com sucesso: {outputFile}");

        return new ProcessPdfResult("Processamento concluído", outputFile);
    }
}
