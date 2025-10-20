namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Requests;

public class AtualizarTermoEspecialRequest
{
    public string CNPJ { get; set; } = string.Empty;
    public int? CodigoBanco { get; set; }
    
    public List<AtualizacaoCodigoDto> Atualizacoes { get; set; } = new();
}