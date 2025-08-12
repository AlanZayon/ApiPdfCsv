using Moq;
using Xunit;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using System.IO;
using System.Threading.Tasks;

public class PdfProcessorServiceTests
{
    private readonly Mock<ApiPdfCsv.Shared.Logging.ILogger> _mockLogger;
    private readonly Mock<IImpostoService> _mockImpostoService;
    private readonly PdfProcessorService _service;

    public PdfProcessorServiceTests()
    {
        
        _mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
        _mockImpostoService = new Mock<IImpostoService>();
        _service = new PdfProcessorService(_mockLogger.Object, _mockImpostoService.Object);
    }

    [Fact]
    public async Task Process_InvalidFilePath_ThrowsException()
    {
        const string invalidPath = "invalid_path.pdf";
        const string userId = "test-user";

        await Assert.ThrowsAsync<IOException>(() => _service.Process(invalidPath, userId));
    }

    [Fact]
    public async Task Process_ValidFile_ReturnsProcessedData()
    {
        var testPdfPath = "Resources/test.pdf";
        const string userId = "test-user";
        
        _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync(new List<decimal> { 0m });
        
        _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync(new List<decimal> { 0m });

        var result = await _service.Process(testPdfPath, userId);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ProcessedPdfData>(result);
        Assert.NotEmpty(result.Comprovantes);
        _mockLogger.Verify(x => x.Info(It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Process_WithMultaEJuros_IncludesInResults()
    {
        var testPdfPath = "Resources/test.pdf";
        const string userId = "test-user";
        
        _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync((List<string> descs) => descs.Select(d => d.Contains("MULTA") ? 10m : 0m).ToList());
        
        _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync((List<string> descs) => descs.Select(d => d.Contains("MULTA") ? 10m : 0m).ToList());

        var result = await _service.Process(testPdfPath, userId);

        var hasMulta = result.Comprovantes
            .Any(c => c.Descricoes.Any(d => d.Contains("MULTA")));
        Assert.True(hasMulta);
    }
}