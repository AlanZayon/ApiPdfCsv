namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;

public class ExcelData
{
    public string DataDeArrecadacao { get; set; } = string.Empty;
    public decimal Debito { get; set; }
    public decimal Credito { get; set; }
    public decimal Total { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int Divisao { get; set; }
}