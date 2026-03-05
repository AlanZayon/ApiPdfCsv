namespace ApiPdfCsv.Modules.PdfProcessing.Application.UseCases;

public class ProcessPdfCommand
{
    public string FilePath { get; }
    public string UserId { get; }
    public string UserSessionId { get; }
    public int? ProLaboreAno { get; } // NOVO
    public decimal? ProLaboreValor { get; } // NOVO

    public ProcessPdfCommand(
        string filePath, 
        string userId, 
        string userSessionId,
        string proLaboreAno = null, // NOVO
        string proLaboreValor = null // NOVO
    )
    {
        FilePath = filePath;
        UserId = userId;
        UserSessionId = userSessionId;
        
        // NOVO: Converter strings para os tipos apropriados
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