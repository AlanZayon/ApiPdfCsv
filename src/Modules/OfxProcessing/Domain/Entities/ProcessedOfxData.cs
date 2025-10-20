namespace ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;

public class OfxTransactionData
{
    public string DataTransacao { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public string Descricao { get; set; } = string.Empty;
    public int CodigoBanco { get; set; } 
}

public class ProcessedOfxData
{
    public List<OfxTransactionData> Transacoes { get; }

    public ProcessedOfxData(List<OfxTransactionData> transacoes)
    {
        Transacoes = transacoes;
    }
}