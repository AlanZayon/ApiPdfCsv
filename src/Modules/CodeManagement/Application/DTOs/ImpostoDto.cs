
namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;

public class ImpostoDto
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public CodigoContaDto? CodigoDebito { get; set; }
    public CodigoContaDto? CodigoCredito { get; set; }
}