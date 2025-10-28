// using Xunit;
// using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.File;
// using ApiPdfCsv.Modules.PdfProcessing.Infrastructure.Options;
// using System;
// using System.IO;
// using Microsoft.Extensions.Options;


// public class FileServiceTests : IDisposable
// {
//     private readonly string _outputDir;
//     private readonly string _uploadDir;
//     private readonly FileService _fileService;

//     public FileServiceTests()
//     {
//         _outputDir = Path.Combine(Path.GetTempPath(), "test_output_" + Guid.NewGuid());
//         _uploadDir = Path.Combine(Path.GetTempPath(), "test_upload_" + Guid.NewGuid());

//         Directory.CreateDirectory(_outputDir);
//         Directory.CreateDirectory(_uploadDir);

//         // Cria uma instância IOptions<FileServiceOptions> para injetar no construtor
//         var options = Options.Create(new FileServiceOptions
//         {
//             OutputDir = _outputDir,
//             UploadDir = _uploadDir
//         });

//         _fileService = new FileService(options);
//     }

//     public void Dispose()
//     {
//         if (Directory.Exists(_outputDir))
//         {
//             Directory.Delete(_outputDir, true);
//         }
//         if (Directory.Exists(_uploadDir))
//         {
//             Directory.Delete(_uploadDir, true);
//         }
//     }

//     [Fact]
//     public void GetSingleFile_WhenNoFiles_ThrowsFileNotFoundException()
//     {
//         // Pastas estão vazias, deve lançar exceção
//         Assert.Throws<FileNotFoundException>(() => _fileService.GetSingleFile());
//     }

//     [Fact]
//     public void ClearDirectories_DeletesAllFiles()
//     {
//         // Cria arquivos nas pastas que o serviço usa
//         File.WriteAllText(Path.Combine(_outputDir, "file1.txt"), "teste");
//         File.WriteAllText(Path.Combine(_uploadDir, "file2.txt"), "teste");

//         _fileService.ClearDirectories();

//         Assert.Empty(Directory.GetFiles(_outputDir));
//         Assert.Empty(Directory.GetFiles(_uploadDir));
//     }
// }
