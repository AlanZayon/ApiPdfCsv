namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

public interface IFileService
{
    string GetUserFile(string userId, string userSessionId, string? fileName = null);
    void ClearUserFiles(string userId, string userSessionId);
    string SaveUserFile(IFormFile file, string userId, string userSessionId);
    string GetUserOutputDir(string userId, string userSessionId);
    string GetUserUploadDir(string userId, string userSessionId);
    Task ScheduleCleanup(string userId, string userSessionId, TimeSpan delay);
}
