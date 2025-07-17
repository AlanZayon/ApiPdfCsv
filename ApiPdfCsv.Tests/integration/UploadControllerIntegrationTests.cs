using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using ApiPdfCsv.API.Controllers;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Shared.Logging;

public class UploadControllerIntegrationTests
{
    [Fact]
    public async Task Upload_NoFile_ReturnsBadRequest()
    {
        var mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
        var mockPdfService = new Mock<IPdfProcessorService>();
        var mockFileService = new Mock<IFileService>();

        var processPdfUseCase = new ProcessPdfUseCase(
            mockPdfService.Object,
            mockLogger.Object,
            mockFileService.Object
        );

        var controller = new UploadController(
            mockLogger.Object,
            mockPdfService.Object,
            mockFileService.Object,
            processPdfUseCase
        );

        var result = await controller.Upload(null!);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
