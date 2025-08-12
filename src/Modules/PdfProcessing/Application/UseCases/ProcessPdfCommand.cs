namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfCommand
{
    public string FilePath { get; }
    public string UserId { get; }

    public ProcessPdfCommand(string filePath, string userId)
    {
        FilePath = filePath;
        UserId = userId;
    }
}