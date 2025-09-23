using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
namespace ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;

public interface IOfxProcessorService
{
    Task<ProcessedOfxData> Process(string filePath, string userId);
}