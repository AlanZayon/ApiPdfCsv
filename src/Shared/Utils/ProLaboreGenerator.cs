using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;

namespace ApiPdfCsv.Shared.Utils;

public static class ProLaboreGenerator
{
    /// <summary>
    /// Gera as linhas de pro labore para todos os meses do ano
    /// </summary>
    /// <param name="ano">Ano do pro labore (ex: 2025)</param>
    /// <param name="valor">Valor do pro labore</param>
    /// <returns>Lista de ExcelData para cada mês</returns>
    public static List<ExcelData> GerarLinhasProLabore(int ano, decimal valor)
    {
        var linhas = new List<ExcelData>();
        
        // Cria linhas para todos os 12 meses
        for (int mes = 1; mes <= 12; mes++)
        {
            // Último dia do mês
            var ultimoDia = DateTime.DaysInMonth(ano, mes);
            var dataProLabore = new DateTime(ano, mes, ultimoDia);
            
            linhas.Add(new ExcelData
            {
                DataDeArrecadacao = dataProLabore.ToString("dd/MM/yyyy"),
                Debito = 5, // Ou o código de débito apropriado
                Credito = 5, // Ou o código de crédito apropriado
                Total = valor,
                Descricao = $"PG. PRO LABORE XX",
                Divisao = 1,
                CodigoBanco = null // Ou um código específico se necessário
            });
        }
        
        return linhas;
    }
    
    /// <summary>
    /// Versão alternativa com códigos personalizados
    /// </summary>
    public static List<ExcelData> GerarLinhasProLabore(int ano, decimal valor, decimal codigoDebito, decimal codigoCredito)
    {
        var linhas = new List<ExcelData>();
        
        for (int mes = 1; mes <= 12; mes++)
        {
            var ultimoDia = DateTime.DaysInMonth(ano, mes);
            var dataProLabore = new DateTime(ano, mes, ultimoDia);
            
            linhas.Add(new ExcelData
            {
                DataDeArrecadacao = dataProLabore.ToString("dd/MM/yyyy"),
                Debito = codigoDebito,
                Credito = codigoCredito,
                Total = valor,
                Descricao = $"PG. PRO LABORE XX",
                Divisao = 1
            });
        }
        
        return linhas;
    }
}