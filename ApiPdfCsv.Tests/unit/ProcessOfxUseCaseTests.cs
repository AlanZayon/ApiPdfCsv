using ApiPdfCsv.Modules.CodeManagement.Domain.Entities;
using ApiPdfCsv.Modules.CodeManagement.Domain.Repositories.Interfaces;
using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ApiPdfCsv.Shared.Storage;
using Moq;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Tests.Unit;

public class ProcessOfxUseCaseTests : IDisposable
{
    private readonly string _outputDir;
    private readonly string _uploadDir;
    private readonly Mock<IOfxProcessorService> _ofxProcessorMock = new();
    private readonly Mock<ILogger> _loggerMock = new();
    private readonly Mock<ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces.IFileService> _fileServiceMock = new();
    private readonly Mock<ITermoEspecialRepository> _termoRepositoryMock = new();
    private readonly LocalBlobStorageService _blobStorage;

    public ProcessOfxUseCaseTests()
    {
        _outputDir = Path.Combine(Path.GetTempPath(), $"ofx-test-{Guid.NewGuid():N}");
        _uploadDir = Path.Combine(Path.GetTempPath(), $"ofx-upload-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_outputDir);
        Directory.CreateDirectory(_uploadDir);

        _fileServiceMock
            .Setup(x => x.GetUserOutputDir(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string userId, string sessionId) => Path.Combine(_outputDir, userId, sessionId));
        _fileServiceMock
            .Setup(x => x.GetUserUploadDir(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string userId, string sessionId) => Path.Combine(_uploadDir, userId, sessionId));
        _fileServiceMock
            .Setup(x => x.GetUserFile(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
            .Returns((string userId, string sessionId, string? fileName) =>
                Path.Combine(_outputDir, userId, sessionId, fileName ?? "EXTRATO.csv"));
        _fileServiceMock.Setup(x => x.ClearUserFiles(It.IsAny<string>(), It.IsAny<string>()));

        _blobStorage = new LocalBlobStorageService(_fileServiceMock.Object);
    }

    public void Dispose()
    {
        if (Directory.Exists(_outputDir))
        {
            Directory.Delete(_outputDir, recursive: true);
        }

        if (Directory.Exists(_uploadDir))
        {
            Directory.Delete(_uploadDir, recursive: true);
        }
    }

    [Fact]
    public async Task Execute_WhenAllTransactionsAreClassified_GeneratesExtratoCsv()
    {
        var sessionId = "user123_session456";
        var transacoes = new List<OfxTransactionData>
        {
            new()
            {
                Descricao = "PIX RECEBIDO",
                DataTransacao = "01/01/2025",
                Valor = 100m,
                CodigoBanco = 341
            }
        };

        _ofxProcessorMock
            .Setup(x => x.Process(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new ProcessedOfxData(transacoes));

        _termoRepositoryMock
            .Setup(x => x.BuscarCodigosBancoPorCnpjAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new List<int?> { 341 });

        _termoRepositoryMock
            .Setup(x => x.BuscarTodosTermosRelevantesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>()))
            .ReturnsAsync(new Dictionary<(string, bool), TermoEspecial>
            {
                [("pix recebido", true)] = new TermoEspecial
                {
                    Termo = "PIX RECEBIDO",
                    CodigoDebito = 1001,
                    CodigoCredito = 2001,
                    CodigoBanco = 341,
                    TipoValor = true
                }
            });

        var useCase = new ProcessOfxUseCase(
            _ofxProcessorMock.Object,
            _loggerMock.Object,
            _blobStorage,
            _termoRepositoryMock.Object);

        var command = new ProcessOfxCommand(
            filePath: Path.GetTempFileName(),
            cnpj: "12345678000199",
            userId: "user123",
            codigoBanco: "341",
            userSessionId: sessionId);

        var result = await useCase.Execute(command);

        Assert.Null(result.TransacoesPendentes);
        Assert.NotNull(result.OutputFile);
        Assert.Equal("EXTRATO.csv", result.OutputFile);
        Assert.True(await _blobStorage.ExistsAsync("user123", sessionId, "EXTRATO.csv"));
    }
}
