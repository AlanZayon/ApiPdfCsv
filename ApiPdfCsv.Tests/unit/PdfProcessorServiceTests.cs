using Moq;
using Xunit;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;

namespace ApiPdfCsv.Tests.Unit;

public class PdfProcessorServiceTests
{
    private const string UserPdf = @"C:\Users\alanz\Downloads\53c3dbf3-a5b3-47b3-b7c2-97d88671e75a (1).pdf";

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
        await Assert.ThrowsAnyAsync<Exception>(() => _service.Process("invalid_path.pdf", "test-user"));
    }

    [Fact]
    public async Task Process_ValidFile_ReturnsProcessedData()
    {
        var testPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "test.pdf");
        const string userId = "test-user";

        Assert.True(File.Exists(testPdfPath), $"Arquivo de teste '{testPdfPath}' não foi encontrado.");

        _mockImpostoService
            .Setup(x => x.MapearDebitoECredito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync((new List<decimal> { 0m }, new List<decimal> { 0m }));

        var result = await _service.Process(testPdfPath, userId);

        Assert.NotNull(result);
        _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Processing PDF file"))), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Process_PropagaIOException_ParaCaminhoInvalido()
    {
        await Assert.ThrowsAnyAsync<Exception>(() => _service.Process("not-found.pdf", "u"));
    }

    [Fact]
    public async Task Process_RfbDarPdf_ExtractsComprovantes()
    {
        if (!File.Exists(UserPdf))
            return;

        const string userId = "test-user";
        _mockImpostoService
            .Setup(x => x.MapearDebitoECredito(It.IsAny<List<string>>(), userId))
            .ReturnsAsync((new List<decimal> { 179m }, new List<decimal> { 0m }));

        var result = await _service.Process(UserPdf, userId);

        Assert.NotEmpty(result.Comprovantes);
        Assert.All(result.Comprovantes, c => Assert.Matches(@"\d{2}/\d{2}/\d{4}", c.DataArrecadacao));
    }
}
