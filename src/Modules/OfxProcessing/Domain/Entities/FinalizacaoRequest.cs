using ApiPdfCsv.Modules.OfxProcessing.Application.UseCases;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;

namespace ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;

public class FinalizacaoRequest
{
    public List<ExcelData> TransacoesClassificadas { get; set; } = new();
    public List<ClassificacaoTransacao> Classificacoes { get; set; } = new();
    public List<TransacaoPendente> TransacoesPendentes { get; set; } = new();
    public string CNPJ { get; set; } = string.Empty;


}
