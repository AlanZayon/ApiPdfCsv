using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace ApiPdfCsv.Tests.Unit;

public class FileServiceTests : IDisposable
{
    private readonly string _baseDir;

    public FileServiceTests()
    {
        _baseDir = Path.Combine(Path.GetTempPath(), $"fileservice-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_baseDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_baseDir))
        {
            Directory.Delete(_baseDir, recursive: true);
        }
    }

    [Fact]
    public void GetUserFile_PrefersExtratoCsvOverOtherFiles()
    {
        var options = Options.Create(new FileServiceOptions
        {
            OutputDir = Path.Combine(_baseDir, "outputs"),
            UploadDir = Path.Combine(_baseDir, "uploads")
        });

        var service = new FileService(options);
        var userId = "user-1";
        var sessionId = "session-1";
        var outputDir = service.GetUserOutputDir(userId, sessionId);
        Directory.CreateDirectory(outputDir);

        File.WriteAllText(Path.Combine(outputDir, "PGTO.csv"), "pdf");
        File.WriteAllText(Path.Combine(outputDir, "EXTRATO.csv"), "ofx");

        var filePath = service.GetUserFile(userId, sessionId);

        Assert.Equal("EXTRATO.csv", Path.GetFileName(filePath));
    }
}
