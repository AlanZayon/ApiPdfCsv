namespace ApiPdfCsv.Modules.CodeManagement.Application.DTOs.Responses;
public class ResultadoAtualizacao
{
    public bool Sucesso { get; set; }
    public string Mensagem { get; set; } = string.Empty;
    public int RegistrosAtualizados { get; set; }
}