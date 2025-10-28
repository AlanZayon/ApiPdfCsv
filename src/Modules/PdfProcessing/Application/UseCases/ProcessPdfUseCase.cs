using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Utils;
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

        var formattedData = result.Comprovantes
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
                    Divisao = 1
                })
            ).ToList();


        ExcelGenerator.Generate(formattedData, outputPath);

        _logger.Info($"Excel gerado com sucesso: {outputPath}");

        return new ProcessPdfResult("Processamento concluído", outputPath);
    }
}