namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;

public class TermoEspecialDto
{
    public string Id { get; set; } = string.Empty;
    public string Termo { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public int CodigoDebito { get; set; }
    public int CodigoCredito { get; set; }
    public int? CodigoBanco { get; set; }
    public string? CNPJ { get; set; }
    public bool TipoValor { get; set; }
}