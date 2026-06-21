namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;

public class CopiarMapeamentosRequest
{
    public string CnpjOrigem { get; set; } = string.Empty;
    public string CnpjDestino { get; set; } = string.Empty;
    public int? CodigoBancoOrigem { get; set; }
    public int? CodigoBancoDestino { get; set; }
}
