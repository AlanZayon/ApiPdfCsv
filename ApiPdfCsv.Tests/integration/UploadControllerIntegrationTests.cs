using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using ApiPdfCsv.API.Controllers;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using System.Security.Claims;
using System.Threading.Tasks;

public class UploadControllerIntegrationTests
{
    private readonly Mock<ApiPdfCsv.Shared.Logging.ILogger> _mockLogger;
    private readonly Mock<IFileService> _mockFileService;
    private readonly Mock<ProcessPdfUseCase> _mockProcessPdfUseCase;
    private readonly Mock<ProcessOfxUseCase> _mockProcessOfxUseCase;
    private readonly UploadController _controller;

    public UploadControllerIntegrationTests()
    {
        _mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
        _mockFileService = new Mock<IFileService>();
        _mockProcessPdfUseCase = new Mock<ProcessPdfUseCase>();
        _mockProcessOfxUseCase = new Mock<ProcessOfxUseCase>();

        // Setup controller with mocked dependencies
        _controller = new UploadController(
            _mockLogger.Object,
            _mockFileService.Object,
            _mockProcessPdfUseCase.Object,
            _mockProcessOfxUseCase.Object
        );

        // Mock user context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        // Act
        var result = await _controller.Upload(null!);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Arquivo não enviado.", (badRequestResult.Value as dynamic)?.message);
        
        // Verify logging was called
        _mockLogger.Verify(l => l.Warn("Tentativa de upload sem envio de arquivo."), Times.Once);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("test.pdf");

        // Act
        var result = await _controller.Upload(mockFile.Object);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Upload_UnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(1024);
        mockFile.Setup(f => f.FileName).Returns("test.txt");
        mockFile.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Upload(mockFile.Object);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Tipo de arquivo não suportado. Use apenas PDF ou OFX.", 
                    (badRequestResult.Value as dynamic)?.message);
    }
}