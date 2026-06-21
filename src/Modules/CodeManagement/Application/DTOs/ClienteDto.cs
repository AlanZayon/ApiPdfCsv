namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;

public class ClienteDto
{
    public int Id { get; set; }
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public int? CodigoBancoPadrao { get; set; }
    public bool Ativo { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
