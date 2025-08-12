using Xunit;
using Moq;
using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
using ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using System.IO;
using System.Threading.Tasks;
using ApiPdfCsv.Modules.CodeManagement.Application.Interfaces;

public class PdfProcessingFunctionalTests
{
    [Fact]
    public async Task FullProcessing_ValidPdf_GeneratesCsv()
    {
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "outputs");
        Directory.CreateDirectory(outputDir);

        var csvPath = Path.Combine(outputDir, "PGTO.csv");

        try
        {
            var logger = new Logger();
            
            var mockImpostoService = new Mock<IImpostoService>();
            mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<decimal> { 0m });
            mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), It.IsAny<string>()))
                .ReturnsAsync(new List<decimal> { 0m });

            var pdfProcessor = new PdfProcessorService(logger, mockImpostoService.Object);

            var mockFileService = new Mock<IFileService>();
            mockFileService.Setup(f => f.GetOutputDir()).Returns(outputDir);
            mockFileService.Setup(f => f.GetUploadDir()).Returns(string.Empty);
            mockFileService.Setup(f => f.ClearDirectories());
            mockFileService.Setup(f => f.GetSingleFile()).Returns(string.Empty);

            var useCase = new ProcessPdfUseCase(pdfProcessor, logger, mockFileService.Object);
            var testPdfPath = Path.Combine(Directory.GetCurrentDirectory(), "Resources", "test.pdf");
            const string userId = "test-user";

            Assert.True(File.Exists(testPdfPath), $"Arquivo de teste '{testPdfPath}' não foi encontrado.");

            var result = await useCase.Execute(new ProcessPdfCommand(testPdfPath, userId));

            Assert.NotNull(result);
            Assert.Equal(csvPath, result.OutputPath);
            Assert.True(File.Exists(csvPath), $"Arquivo esperado '{csvPath}' não foi gerado.");
        }
        finally
        {
            // Clean up the generated CSV
            if (File.Exists(csvPath))
            {
                File.Delete(csvPath);
            }
        }
    }
}