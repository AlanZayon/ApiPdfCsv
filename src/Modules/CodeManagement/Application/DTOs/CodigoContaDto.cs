namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
public class CodigoContaDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
}