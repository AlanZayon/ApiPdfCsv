namespace ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;


public class ComprovanteData
{
    public string DataArrecadacao { get; set; } = string.Empty;
    public List<decimal> Debito { get; set; } = new();
    public decimal Credito { get; set; }
    public List<decimal> Total { get; set; } = new();
    public List<string> Descricoes { get; set; } = new();

    public ComprovanteData() { }

    public ComprovanteData(ComprovanteData other)
    {
        DataArrecadacao = other.DataArrecadacao;
        Debito = new List<decimal>(other.Debito);
        Credito = other.Credito;
        Total = new List<decimal>(other.Total);
        Descricoes = new List<string>(other.Descricoes);
    }
}