namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

public interface IFileService
{
    string GetSingleFile();
    void ClearDirectories();

    
    string GetOutputDir();   
    string GetUploadDir();
}
