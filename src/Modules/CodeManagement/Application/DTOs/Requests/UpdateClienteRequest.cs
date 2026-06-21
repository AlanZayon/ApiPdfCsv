namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;

public class UpdateClienteRequest
{
    public string RazaoSocial { get; set; } = string.Empty;
    public int? CodigoBancoPadrao { get; set; }
    public bool Ativo { get; set; } = true;
}
