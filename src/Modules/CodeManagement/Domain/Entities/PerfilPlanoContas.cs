namespace ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

public class PerfilPlanoContas
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? CodigoGenericoEntrada { get; set; }
    public string? CodigoGenericoSaida { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
