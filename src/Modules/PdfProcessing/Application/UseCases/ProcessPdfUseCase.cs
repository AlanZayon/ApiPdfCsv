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

        var outputDir = _fileService.GetOutputDir();
        
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // Gerar nome único usando userId e timestamp
        var fileName = $"PGTO_{command.UserId}_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N[..8]}.csv";
        var outputPath = Path.Combine(outputDir, fileName);

        var formattedData = result.Comprovantes
            .SelectMany(comp => comp.Descricoes
                .Select((descricao, index) => new { descricao, index })
                .Where(x => x.index < comp.Total.Count && 
                           x.index < comp.Debito.Count && 
                           x.index < comp.Credito.Count &&
                           ((comp.Total[x.index] as decimal?) ?? 0m) != 0m)
                .Select(x => new ExcelData
                {
                    DataDeArrecadacao = comp.DataArrecadacao,
                    Debito = x.index < comp.Debito.Count ? comp.Debito[x.index] : 0m,
                    Credito = x.index < comp.Credito.Count ? comp.Credito[x.index] : 0m,
                    Total = x.index < comp.Total.Count ? (comp.Total[x.index] as decimal?) ?? 0m : 0m,
                    Descricao = x.descricao,
                    Divisao = 1
                })
            ).ToList();


        ExcelGenerator.Generate(formattedData, outputPath);

        _logger.Info($"Excel gerado com sucesso: {outputPath}");

        return new ProcessPdfResult("Processamento concluído", outputPath);
    }
}