namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;

public class PerfilPlanoContasDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public string? CodigoGenericoEntrada { get; set; }
    public string? CodigoGenericoSaida { get; set; }
}

public class CriarPerfilRequest
{
    public string Nome { get; set; } = string.Empty;
    public int? CopiarDePerfilId { get; set; }
}

public class AtualizarPerfilRequest
{
    public string Nome { get; set; } = string.Empty;
    public string? CodigoGenericoEntrada { get; set; }
    public string? CodigoGenericoSaida { get; set; }
    public bool? IsDefault { get; set; }
}
