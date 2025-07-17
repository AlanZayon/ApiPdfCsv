using Moq;
using Xunit;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;

public class PdfProcessorServiceTests
{
    [Fact]
    public async Task Process_InvalidFilePath_ThrowsException()
    {
        // Arrange
        var mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
        var service = new PdfProcessorService(mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<IOException>(() => service.Process("invalid_path.pdf"));

    }

    [Fact]
    public async Task Process_ValidFile_ReturnsProcessedData()
    {
        // Arrange
        var mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
        var service = new PdfProcessorService(mockLogger.Object);
        var testPdfPath = "Resources/test.pdf";

        // Act
        var result = await service.Process(testPdfPath);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Comprovantes);
        mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.AtLeastOnce);
    }
}