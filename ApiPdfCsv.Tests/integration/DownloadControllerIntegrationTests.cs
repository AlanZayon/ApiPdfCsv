// using Microsoft.AspNetCore.Mvc;
// using Moq;
// using Xunit;
// using ApiPdfCsv.API.Controllers;
// using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

// public class DownloadControllerIntegrationTests
// {
//     [Fact]
//     public void DownloadFile_NoFilesAvailable_ReturnsNotFound()
//     {
//         // Arrange
//         var mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
//         var mockFileService = new Mock<IFileService>();
//         mockFileService.Setup(x => x.GetSingleFile()).Throws<FileNotFoundException>();
        
//         var controller = new DownloadController(mockLogger.Object, mockFileService.Object);
        
//         // Act
//         var result = controller.DownloadFile();
        
//         // Assert
//         Assert.IsType<NotFoundObjectResult>(result);
//     }
// }