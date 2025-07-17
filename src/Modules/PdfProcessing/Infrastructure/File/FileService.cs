using System.IO;
using Microsoft.Extensions.Options;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;

namespace ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;

public class FileService : IFileService
{
    private readonly string _outputDir;
    private readonly string _uploadDir;

    public FileService(IOptions<FileServiceOptions> options)
    {
        _outputDir = options.Value.OutputDir;
        _uploadDir = options.Value.UploadDir;
    }

    public string GetSingleFile()
    {
        EnsureDirectoryExists(_outputDir);

        var files = Directory.GetFiles(_outputDir);
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No files available for download");
        }

        return files[0];
    }

    public void ClearDirectories()
    {
        ClearDirectory(_outputDir);
        ClearDirectory(_uploadDir);
    }

    public string GetOutputDir() => _outputDir;

    public string GetUploadDir() => _uploadDir;

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private void ClearDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        foreach (var file in Directory.GetFiles(directoryPath))
        {
            try
            {
                System.IO.File.Delete(file);
            }
            catch
            {
                // Ignore errors
            }
        }
    }
}
