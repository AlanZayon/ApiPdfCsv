using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using ApiPdfCsv.API.Controllers;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Processing;
using ApiPdfCsv.Shared.Storage;
using System.Security.Claims;

namespace ApiPdfCsv.Tests.Integration;

public class UploadControllerIntegrationTests
{
    private readonly Mock<ILogger> _mockLogger = new();
    private readonly Mock<IBlobStorageService> _mockBlobStorage = new();
    private readonly Mock<IUploadJobService> _mockUploadJobService = new();
    private readonly UploadController _controller;

    public UploadControllerIntegrationTests()
    {
        _controller = new UploadController(
            _mockLogger.Object,
            _mockBlobStorage.Object,
            _mockUploadJobService.Object);

        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, "test-user-id")
        ], "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
        _controller.HttpContext.Request.Headers["X-User-Session"] = "test-user-id_session_test";
    }

    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        var result = await _controller.Upload(null!, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
        _mockLogger.Verify(l => l.Warn("Tentativa de upload sem envio de arquivo."), Times.Once);
    }

    [Fact]
    public async Task Upload_EmptyFile_ReturnsBadRequest()
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.Length).Returns(0);
        mockFile.Setup(f => f.FileName).Returns("test.pdf");

        var result = await _controller.Upload(mockFile.Object, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
