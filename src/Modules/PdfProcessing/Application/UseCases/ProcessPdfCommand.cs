namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfCommand
{
    public string FilePath { get; }
    public string UserId { get; }
    public string UserSessionId { get; }
    public int? ProLaboreAno { get; }
    public decimal? ProLaboreValor { get; }
    public int? ClienteId { get; }

    public ProcessPdfCommand(
        string filePath, 
        string userId, 
        string userSessionId,
        string? proLaboreAno = null,
        string? proLaboreValor = null,
        int? clienteId = null)
    {
        FilePath = filePath;
        UserId = userId;
        UserSessionId = userSessionId;
        ClienteId = clienteId;
        
        if (!string.IsNullOrEmpty(proLaboreAno) && int.TryParse(proLaboreAno, out var ano))
        {
            ProLaboreAno = ano;
        }
        
        if (!string.IsNullOrEmpty(proLaboreValor) && decimal.TryParse(proLaboreValor, out var valor))
        {
            ProLaboreValor = valor;
        }
    }
}
