using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using System.Threading.Tasks;

namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Interfaces;

public interface IPdfProcessorService
{
    Task<ProcessedPdfData> Process(string filePath);
}