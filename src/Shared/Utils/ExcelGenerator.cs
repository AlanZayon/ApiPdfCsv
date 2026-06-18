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
        var bytes = GenerateBytes(data, manterOrdemOriginal);
        File.WriteAllText(outputPath, new UTF8Encoding(true).GetString(bytes), new UTF8Encoding(true));
    }

    public static byte[] GenerateBytes(List<ExcelData> data, bool manterOrdemOriginal = false)
    {
        if (data.Count == 0)
        {
            throw new InvalidDataException("Nenhum dado para gerar o CSV");
        }

        List<ExcelData> dadosParaProcessar;
        if (manterOrdemOriginal)
        {
            dadosParaProcessar = data;
        }
        else
        {
            dadosParaProcessar = data.OrderBy(item => ParseData(item.DataDeArrecadacao)).ToList();
        }

        var csvContent = BuildFixedLayoutCsv(dadosParaProcessar);
        return new UTF8Encoding(true).GetBytes(csvContent);
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

    private static string BuildFixedLayoutCsv(IEnumerable<ExcelData> rows)
    {
        var lines = new List<string>();

        foreach (var item in rows)
        {
            var dataValue = ParseData(item.DataDeArrecadacao);
            if (dataValue == DateTime.MinValue)
                continue;

            var tipo = "1";
            var data = dataValue.ToString("ddMMyyyy");
            var codigoOrigem = item.Debito.ToString(CultureInfo.InvariantCulture);
            var codigoDestino = item.Credito.ToString(CultureInfo.InvariantCulture);
            var valor = Math.Abs(item.Total).ToString("F2", CultureInfo.InvariantCulture);
            var descricao = (item.Descricao ?? string.Empty).Replace("\"", "\"\"");

            lines.Add(string.Format(
                "{0},{1},{2},{3},{4},,\"{5}\"",
                tipo,
                data,
                codigoOrigem,
                codigoDestino,
                valor,
                descricao
            ));
        }

        return string.Join(Environment.NewLine, lines);
    }
}
