namespace ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

public class CodigoConta
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? UserId { get; set; }
}