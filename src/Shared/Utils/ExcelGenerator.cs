using ClosedXML.Excel;
using ApiPdfCsv.Modules.PdfProcessing.Domain.Entities;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ApiPdfCsv.Shared.Utils;

public static class ExcelGenerator
{
    public static void Generate(List<ExcelData> data, string outputPath)
    {
        if (data.Count == 0)
        {
            throw new InvalidDataException("Nenhum dado para gerar o CSV");
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Relatório");

        // Configurar colunas
        worksheet.Column(1).Width = 30;
        worksheet.Column(2).Width = 30;
        worksheet.Column(3).Width = 30;
        worksheet.Column(4).Width = 30;
        worksheet.Column(5).Width = 30;
        worksheet.Column(6).Width = 30;

        var rowIndex = 1;
        foreach (var item in data)
        {
            // Verificar se existe o campo CodigoBanco
            bool hasCodigoBanco = item.CodigoBanco.HasValue && !string.IsNullOrEmpty(item.CodigoBanco.Value.ToString());

            if (hasCodigoBanco)
            {
                // Se tem Código Banco - apenas UMA linha com débito primeiro e crédito depois
                worksheet.Cell(rowIndex, 1).Value = item.DataDeArrecadacao;
                worksheet.Cell(rowIndex, 2).Value = item.Debito;  // Débito primeiro
                worksheet.Cell(rowIndex, 3).Value = item.Credito; // Crédito depois
                worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                worksheet.Cell(rowIndex, 6).Value = "1";
                rowIndex++;
            }
            else
            {
                // Se não tem Código Banco - DUAS linhas normais (débito primeiro e crédito depois)
                if (item.Total < 0)
                {
                    // Linha 1 - Débito
                    worksheet.Cell(rowIndex, 1).Value = item.DataDeArrecadacao;
                    worksheet.Cell(rowIndex, 2).Value = item.Debito;
                    worksheet.Cell(rowIndex, 3).Value = "";
                    worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                    worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                    worksheet.Cell(rowIndex, 6).Value = "1";
                    rowIndex++;

                    // Linha 2 - Crédito
                    worksheet.Cell(rowIndex, 1).Value = item.DataDeArrecadacao;
                    worksheet.Cell(rowIndex, 2).Value = "";
                    worksheet.Cell(rowIndex, 3).Value = item.Credito;
                    worksheet.Cell(rowIndex, 4).Value = Math.Abs(item.Total);
                    worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                    worksheet.Cell(rowIndex, 6).Value = "";
                    rowIndex++;
                }
                else
                {
                    // Linha 1 - Débito
                    worksheet.Cell(rowIndex, 1).Value = item.DataDeArrecadacao;
                    worksheet.Cell(rowIndex, 2).Value = item.Debito;
                    worksheet.Cell(rowIndex, 3).Value = "";
                    worksheet.Cell(rowIndex, 4).Value = item.Total;
                    worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                    worksheet.Cell(rowIndex, 6).Value = "1";
                    rowIndex++;

                    // Linha 2 - Crédito
                    worksheet.Cell(rowIndex, 1).Value = item.DataDeArrecadacao;
                    worksheet.Cell(rowIndex, 2).Value = "";
                    worksheet.Cell(rowIndex, 3).Value = item.Credito;
                    worksheet.Cell(rowIndex, 4).Value = item.Total;
                    worksheet.Cell(rowIndex, 5).Value = item.Descricao;
                    worksheet.Cell(rowIndex, 6).Value = "";
                    rowIndex++;
                }
            }
        }

        var csvContent = ConvertToCSV(worksheet);
        File.WriteAllText(outputPath, csvContent);
    }

    private static string ConvertToCSV(IXLWorksheet worksheet)
    {
        var rows = new List<string>();
        var lastRow = worksheet.LastRowUsed();

        if (lastRow == null)
        {
            return string.Empty;
        }

        for (var row = 1; row <= lastRow.RowNumber(); row++)
        {
            var values = new List<string>();
            var lastColumn = worksheet.LastColumnUsed();

            if (lastColumn == null)
            {
                continue;
            }

            for (var col = 1; col <= lastColumn.ColumnNumber(); col++)
            {
                var cell = worksheet.Cell(row, col);

                string formattedValue;
                if (cell.DataType == XLDataType.DateTime)
                {
                    var date = cell.GetValue<DateTime>();
                    formattedValue = date.ToString("dd/MM/yyyy");
                }
                else if (cell.DataType == XLDataType.Number)
                {
                    if (decimal.TryParse(cell.Value.ToString(), out var dec))
                    {
                        formattedValue = dec.ToString("F2", new CultureInfo("pt-BR"));
                    }
                    else if (double.TryParse(cell.Value.ToString(), out var d))
                    {
                        formattedValue = d.ToString("F2", new CultureInfo("pt-BR"));
                    }
                    else
                    {
                        formattedValue = cell.Value.ToString() ?? "";
                    }
                }
                else
                {
                    formattedValue = cell.Value.ToString() ?? "";
                }

                values.Add(formattedValue);
            }

            rows.Add(string.Join(";", values));
        }

        return string.Join(Environment.NewLine, rows);
    }
}