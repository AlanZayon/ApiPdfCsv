namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfCommand
{
    public string FilePath { get; }
    public string UserId { get; }
    public string UserSessionId { get; }

    public ProcessPdfCommand(string filePath, string userId, string userSessionId)
    {
        FilePath = filePath;
        UserId = userId;
        UserSessionId = userSessionId;
    }
}