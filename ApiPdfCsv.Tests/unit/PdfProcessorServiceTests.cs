// using Moq;
// using Xunit;
// using ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;
// using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Services;
// using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
// using System.IO;
// using System.Threading.Tasks;
// using System.Linq;
// using System.Collections.Generic;

// public class PdfProcessorServiceTests
// {
//     private readonly Mock<ApiPdfCsv.Shared.Logging.ILogger> _mockLogger;
//     private readonly Mock<IImpostoService> _mockImpostoService;
//     private readonly PdfProcessorService _service;

//     public PdfProcessorServiceTests()
//     {
//         _mockLogger = new Mock<ApiPdfCsv.Shared.Logging.ILogger>();
//         _mockImpostoService = new Mock<IImpostoService>();
//         _service = new PdfProcessorService(_mockLogger.Object, _mockImpostoService.Object);
//     }

//     [Fact]
//     public async Task Process_InvalidFilePath_ThrowsException()
//     {
//         const string invalidPath = "invalid_path.pdf";
//         const string userId = "test-user";

//         await Assert.ThrowsAsync<IOException>(() => _service.Process(invalidPath, userId));
//     }

//     [Fact]
//     public async Task Process_ValidFile_ReturnsProcessedData()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "test-user";
        
//         _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });
        
//         _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });

//         var result = await _service.Process(testPdfPath, userId);

//         // Assert
//         Assert.NotNull(result);
//         Assert.IsType<ProcessedPdfData>(result);
//         Assert.NotEmpty(result.Comprovantes);
//         _mockLogger.Verify(x => x.Info(It.Is<string>(s => s.Contains("Processing PDF file"))), Times.AtLeastOnce);
//     }

//     [Fact]
//     public async Task Process_WithMultaEJuros_IncludesInResults()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "test-user";
        
//         _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync((List<string> descs, string _) => descs.Select(d => d.Contains("MULTA") ? 10m : 0m).ToList());
        
//         _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync((List<string> descs, string _) => descs.Select(d => d.Contains("MULTA") ? 10m : 0m).ToList());

//         var result = await _service.Process(testPdfPath, userId);

//         var hasMulta = result.Comprovantes
//             .Any(c => c.Descricoes.Any(d => d.Contains("MULTA")));
//         Assert.True(hasMulta);
//     }

//     // New tests

//     [Fact]
//     public async Task Process_WhenTotaisLineMissingPrices_DoesNotAddEmptyDescriptions()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "user";

//         _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });
//         _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });

//         var result = await _service.Process(testPdfPath, userId);

//         Assert.All(result.Comprovantes, c => Assert.DoesNotContain(c.Descricoes, d => string.IsNullOrWhiteSpace(d)));
//     }

//     [Fact]
//     public async Task Process_AgregaDescricoesEValores_PassaDescricoesAgrupadasParaServicos()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "user";

//         List<string>? capturedDescricoes = null;
//         _mockImpostoService
//             .Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
//             .Callback<List<string>, string>((descs, _) => capturedDescricoes = descs)
//             .ReturnsAsync(new List<decimal> { 0m });

//         _mockImpostoService
//             .Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });

//         var result = await _service.Process(testPdfPath, userId);

//         Assert.NotNull(capturedDescricoes);
//         Assert.All(result.Comprovantes, c => Assert.Equal(c.Descricoes, capturedDescricoes));
//     }

//     [Fact]
//     public async Task Process_MultaEJuros_AdicionaDescricaoEValoresMapeados()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "user";

//         _mockImpostoService.Setup(x => x.MapearDebito(It.Is<List<string>>(l => l.Contains("PG. MULTA E JUROS XX")), userId))
//             .ReturnsAsync(new List<decimal> { 1m });
//         _mockImpostoService.Setup(x => x.MapearCredito(It.Is<List<string>>(l => l.Contains("PG. MULTA E JUROS XX")), userId))
//             .ReturnsAsync(new List<decimal> { 2m });

//         var result = await _service.Process(testPdfPath, userId);

//         var comprovante = Assert.Single(result.Comprovantes);
//         Assert.Contains("PG. MULTA E JUROS XX", comprovante.Descricoes);
//         Assert.Contains(1m, comprovante.Debito);
//         Assert.Contains(2m, comprovante.Credito);
//     }

//     [Fact]
//     public async Task Process_PropagaIOException_ParaCaminhoInvalido()
//     {
//         await Assert.ThrowsAsync<IOException>(() => _service.Process("not-found.pdf", "u"));
//     }

//     [Fact]
//     public async Task Process_GeraComprovantesComDataArrecadacao()
//     {
//         var testPdfPath = "Resources/test.pdf";
//         const string userId = "user";

//         _mockImpostoService.Setup(x => x.MapearDebito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });
//         _mockImpostoService.Setup(x => x.MapearCredito(It.IsAny<List<string>>(), userId))
//             .ReturnsAsync(new List<decimal> { 0m });

//         var result = await _service.Process(testPdfPath, userId);

//         Assert.NotEmpty(result.Comprovantes);
//         Assert.All(result.Comprovantes, c => Assert.False(string.IsNullOrEmpty(c.DataArrecadacao)));
//     }
// }
