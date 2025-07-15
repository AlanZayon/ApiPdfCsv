namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;

public class ProcessedPdfData
{
    public List<ComprovanteData> Comprovantes { get; }

    public ProcessedPdfData(List<ComprovanteData> comprovantes)
    {
        Comprovantes = comprovantes;
    }
}