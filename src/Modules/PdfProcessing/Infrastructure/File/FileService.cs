using System.IO;

namespace ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;

public static class FileService
{
    private static readonly string OutputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
    private static readonly string UploadDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");

    public static string GetSingleFile()
    {
        EnsureDirectoryExists(OutputDir);
        
        var files = Directory.GetFiles(OutputDir);
        if (files.Length == 0)
        {
            throw new FileNotFoundException("No files available for download");
        }

        return files[0];
    }

    public static void ClearDirectories()
    {
        ClearDirectory(OutputDir);
        ClearDirectory(UploadDir);
    }

    private static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    private static void ClearDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath)) return;

        try
        {
            foreach (var file in Directory.GetFiles(directoryPath))
            {
                try
                {
                    System.IO.File.Delete(file);
                }
                catch
                {
                    // Ignore errors when deleting individual files
                }
            }
        }
        catch
        {
            // Ignore errors when accessing directory
        }
    }
}