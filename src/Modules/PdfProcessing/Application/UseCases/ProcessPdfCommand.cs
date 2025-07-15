namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfCommand
{
    public string FilePath { get; }

    public ProcessPdfCommand(string filePath)
    {
        FilePath = filePath;
    }
}