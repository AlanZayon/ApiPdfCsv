namespace ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Cnpj { get; set; } = string.Empty;
    public string RazaoSocial { get; set; } = string.Empty;
    public int? CodigoBancoPadrao { get; set; }
    public bool Ativo { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
