namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs;
public class AtualizacaoCodigoDto
{
    public Guid TermoEspecialId { get; set; }
    public int? NovoCodigoDebito { get; set; }
    public int? NovoCodigoCredito { get; set; }
}