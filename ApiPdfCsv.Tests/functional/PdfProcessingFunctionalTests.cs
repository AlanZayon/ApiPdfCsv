using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Storage;
using Moq;
using Xunit;

namespace ApiPdfCsv.Tests.Functional;

public class PdfProcessingFunctionalTests
{
    [Fact]
    public async Task FullProcessing_ValidPdf_GeneratesCsvInBlobStorage()
    {
        var outputDir = Path.Combine(Path.GetTempPath(), $"pdf-func-{Guid.NewGuid():N}", "outputs");
        var uploadDir = Path.Combine(Path.GetTempPath(), $"pdf-func-{Guid.NewGuid():N}", "uploads");

        var fileServiceMock = new Mock<IFileService>();
        fileServiceMock.Setup(f => f.GetUserOutputDir(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string userId, string sessionId) => Path.Combine(outputDir, userId, sessionId));
        fileServiceMock.Setup(f => f.GetUserUploadDir(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string userId, string sessionId) => Path.Combine(uploadDir, userId, sessionId));
        fileServiceMock.Setup(f => f.GetUserFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string userId, string sessionId, string? fileName) =>
                Path.Combine(outputDir, userId, sessionId, fileName ?? "PGTO.csv"));
        fileServiceMock.Setup(f => f.ClearUserFiles(It.IsAny<string>(), It.IsAny<string>()));

        var blobStorage = new LocalBlobStorageService(fileServiceMock.Object);
        var logger = new Logger();

        var pdfProcessorMock = new Mock<IPdfProcessorService>();
        pdfProcessorMock
            .Setup(x => x.Process(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProcessedPdfData(new List<ComprovanteData>
            {
                new()
                {
                    DataArrecadacao = "01/01/2025",
                    Descricoes = new List<string> { "PG. SIMPLES NACIONAL XX" },
                    Debito = new List<decimal> { 531m },
                    Credito = new List<decimal> { 100m },
                    Total = new List<decimal> { 100m }
                }
            }));

        var useCase = new ProcessPdfUseCase(pdfProcessorMock.Object, logger, blobStorage);
        const string userId = "test-user";
        const string sessionId = "test-session";

        var result = await useCase.Execute(new ProcessPdfCommand("dummy.pdf", userId, sessionId));

        Assert.NotNull(result);
        Assert.Equal("PGTO.csv", result.OutputFile);
        Assert.True(await blobStorage.ExistsAsync(userId, sessionId, "PGTO.csv"));
    }
}
