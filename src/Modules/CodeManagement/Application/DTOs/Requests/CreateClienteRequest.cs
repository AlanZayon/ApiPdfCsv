namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;

public class CreateClienteRequest
{
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public int? CodigoBancoPadrao { get; set; }
}
