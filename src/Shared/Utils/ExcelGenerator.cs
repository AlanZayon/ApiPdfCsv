using ClosedXML.Excel;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace ApiPdfCsv.Shared.Utils;

public static class ExcelGenerator
{
    public static void Generate(List<ExcelData> data, string outputPath)
    {
        if (data.Count == 0)
        {
            throw new InvalidDataException("Nenhum dado para gerar o CSV");
        }

        var dadosOrdenados = data.OrderBy(item => ParseData(item.DataDeArrecadacao)).ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Relatório");

        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 30;
        worksheet.Column(4).Width = 30;
        worksheet.Column(5).Width = 30;
        worksheet.Column(6).Width = 30;

        var rowIndex = 1;
        foreach (var item in dadosOrdenados)
        {
            bool hasCodigoBanco = item.CodigoBanco.HasValue && !string.IsNullOrEmpty(item.CodigoBanco.Value.ToString());

            if (hasCodigoBanco)
            {
                worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
                worksheet.Cell(rowIndex, 2).Value = item.Debito;
                worksheet.Cell(rowIndex, 3).Value = item.Credito;
                worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                worksheet.Cell(rowIndex, 6).Value = "1";
                rowIndex++;
            }
            else
            {
                // if (item.Total < 0)
                // {
                //     worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
                //     worksheet.Cell(rowIndex, 2).Value = item.Debito;
                //     worksheet.Cell(rowIndex, 3).Value = "";
                //     worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                //     worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                //     worksheet.Cell(rowIndex, 6).Value = "1";
                //     rowIndex++;

                //     worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
                //     worksheet.Cell(rowIndex, 2).Value = "";
                //     worksheet.Cell(rowIndex, 3).Value = item.Credito;
                //     worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                //     worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                //     worksheet.Cell(rowIndex, 6).Value = "";
                //     rowIndex++;
                // }
                // else
                // {
                //     worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
                //     worksheet.Cell(rowIndex, 2).Value = item.Debito;
                //     worksheet.Cell(rowIndex, 3).Value = "";
                //     worksheet.Cell(rowIndex, 4).Value = item.Total;
                //     worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                //     worksheet.Cell(rowIndex, 6).Value = "1";
                //     rowIndex++;

                //     worksheet.Cell(rowIndex, 1).Value = ParseData(item.DataDeArrecadacao);
                //     worksheet.Cell(rowIndex, 2).Value = "";
                //     worksheet.Cell(rowIndex, 3).Value = item.Credito;
                //     worksheet.Cell(rowIndex, 4).Value = item.Total;
                //     worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                //     worksheet.Cell(rowIndex, 6).Value = "";
                //     rowIndex++;
                // }
            }
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

            // DATA (coluna correta)
            if (!worksheet.Cell(row, 1).TryGetValue<DateTime>(out var dataValue))
                continue; // ou lança erro se quiser

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



    // private static string ConvertToCSV(IXLWorksheet worksheet)
    // {
    //     var rows = new List<string>();
    //     var lastRow = worksheet.LastRowUsed();

    //     if (lastRow == null)
    //     {
    //         return string.Empty;
    //     }

    //     for (var row = 1; row <= lastRow.RowNumber(); row++)
    //     {
    //         var values = new List<string>();
    //         var lastColumn = worksheet.LastColumnUsed();

    //         if (lastColumn == null)
    //         {
    //             continue;
    //         }

    //         for (var col = 1; col <= lastColumn.ColumnNumber(); col++)
    //         {
    //             var cell = worksheet.Cell(row, col);

    //             string formattedValue;
    //             if (cell.DataType == XLDataType.DateTime)
    //             {
    //                 var date = cell.GetValue<DateTime>();
    //                 formattedValue = date.ToString("dd/MM/yyyy");
    //             }
    //             else if (cell.DataType == XLDataType.Number)
    //             {
    //                 if (decimal.TryParse(cell.Value.ToString(), out var dec))
    //                 {
    //                     formattedValue = dec.ToString("F2", new CultureInfo("pt-BR"));
    //                 }
    //                 else if (double.TryParse(cell.Value.ToString(), out var d))
    //                 {
    //                     formattedValue = d.ToString("F2", new CultureInfo("pt-BR"));
    //                 }
    //                 else
    //                 {
    //                     formattedValue = cell.Value.ToString() ?? "";
    //                 }
    //             }
    //             else
    //             {
    //                 formattedValue = cell.Value.ToString() ?? "";
    //             }

    //             values.Add(formattedValue);
    //         }

    //         rows.Add(string.Join(";", values));
    //     }

    //     return string.Join(Environment.NewLine, rows);
    // }
}