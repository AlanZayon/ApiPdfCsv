namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

public interface IFileService
{
    string GetUserFile(string userSessionId);
    void ClearUserFiles(string userSessionId);
    string SaveUserFile(IFormFile file, string userSessionId);
    string GetUserOutputDir(string userSessionId);
    string GetUserUploadDir(string userSessionId);
    Task ScheduleCleanup(string userSessionId, TimeSpan delay);
}
