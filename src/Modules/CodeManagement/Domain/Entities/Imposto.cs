namespace ApiPdfCsv.Modules.CodeManagement.Domain.Entities;

public class Imposto
{
    public int Id { get; set; }
    public string Nome { get; set; }= string.Empty;
    public int? CodigoDebitoId { get; set; }
    public CodigoConta? CodigoDebito { get; set; }
    public int? CodigoCreditoId { get; set; }
    public CodigoConta? CodigoCredito { get; set; }
    public string? UserId { get; set; }
}