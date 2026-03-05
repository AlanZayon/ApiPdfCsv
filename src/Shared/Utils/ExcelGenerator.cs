using ClosedXML.Excel;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ApiPdfCsv.Shared.Utils;

public static class ExcelGenerator
{
    public static void Generate(List<ExcelData> data, string outputPath, bool manterOrdemOriginal = false)
    {
        if (data.Count == 0)
        {
            throw new InvalidDataException("Nenhum dado para gerar o CSV");
        }

        // Só ordena por data se não for para manter a ordem original
        List<ExcelData> dadosParaProcessar;
        if (manterOrdemOriginal)
        {
            dadosParaProcessar = data; // Mantém a ordem que recebemos (PDF + Pro Labore em blocos)
        }
        else
        {
            dadosParaProcessar = data.OrderBy(item => ParseData(item.DataDeArrecadacao)).ToList();
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Relatório");

        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 30;
        worksheet.Column(4).Width = 30;
        worksheet.Column(5).Width = 30;
        worksheet.Column(6).Width = 30;

        var rowIndex = 1;
        foreach (var item in dadosParaProcessar)
        {
            worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
            worksheet.Cell(rowIndex, 2).Value = item.Debito;
            worksheet.Cell(rowIndex, 3).Value = item.Credito;
            worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
            worksheet.Cell(rowIndex, 5).Value = item.Descricao;
            worksheet.Cell(rowIndex, 6).Value = "1";
            rowIndex++;
        }

        var csvContent = ConvertToFixedLayoutCsv(worksheet);
        File.WriteAllText(outputPath, csvContent, new UTF8Encoding(true));
    }

    private static DateTime ParseData(string dataString)
    {
        if (DateTime.TryParseExact(dataString, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime data))
        {
            return data;
        }

        if (DateTime.TryParse(dataString, CultureInfo.GetCultureInfo("pt-BR"), DateTimeStyles.None, out data))
        {
            return data;
        }

        return DateTime.MinValue;
    }

    private static string ConvertToFixedLayoutCsv(IXLWorksheet worksheet)
    {
        var rows = new List<string>();
        var lastRow = worksheet.LastRowUsed();

        if (lastRow == null)
            return string.Empty;

        for (int row = 1; row <= lastRow.RowNumber(); row++)
        {
            var tipo = "1";

            if (!worksheet.Cell(row, 1).TryGetValue<DateTime>(out var dataValue))
                continue;

            var data = dataValue.ToString("ddMMyyyy");

            var codigoOrigem = worksheet.Cell(row, 2).GetValue<string>();
            var codigoDestino = worksheet.Cell(row, 3).GetValue<string>();

            var valor = worksheet.Cell(row, 4)
                .GetValue<decimal>()
                .ToString("F2", CultureInfo.InvariantCulture);

            var descricao = worksheet.Cell(row, 5)
                .GetString()
                .Replace("\"", "\"\"");

            var csvLine = string.Format(
                "{0},{1},{2},{3},{4},,\"{5}\"",
                tipo,
                data,
                codigoOrigem,
                codigoDestino,
                valor,
                descricao
            );

            rows.Add(csvLine);
        }

        return string.Join(Environment.NewLine, rows);
    }
}