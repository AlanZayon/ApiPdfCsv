using System.Text;
using System.Text.RegularExpressions;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Entities;
using ApiPdfCsv.Modules.OfxProcessing.Domain.Interfaces;
using ApiPdfCsv.Shared.Logging;
using ILogger = ApiPdfCsv.Shared.Logging.ILogger;

namespace ApiPdfCsv.Modules.OfxProcessing.Infrastructure.Services;

public class OfxProcessorService : IOfxProcessorService
{
    private readonly ILogger _logger;

    public OfxProcessorService(ILogger logger)
    {
        _logger = logger;
    }

    public async Task<ProcessedOfxData> Process(string filePath, string userId)
    {
        _logger.Info($"Iniciando processamento do OFX: {filePath}");

        // ✅ Uso de API assíncrona de leitura
        var ofxContent = await File.ReadAllTextAsync(filePath, Encoding.GetEncoding("iso-8859-1"));

        var transacoes = new List<OfxTransactionData>();
        ProcessarTransacoesDirect(ofxContent, transacoes);

        _logger.Info($"Processamento OFX concluído. {transacoes.Count} transações encontradas.");

        return new ProcessedOfxData(transacoes);
    }

    private void ProcessarTransacoesDirect(string ofxContent, List<OfxTransactionData> transacoes)
    {
        try
        {
            _logger.Info("Processando OFX diretamente (modo SGML)");

            var lines = ofxContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            OfxTransactionData? currentTransaction = null;
            bool inTransaction = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine) ||
                    trimmedLine.StartsWith("OFXHEADER:") ||
                    trimmedLine.StartsWith("DATA:") ||
                    trimmedLine.StartsWith("VERSION:") ||
                    trimmedLine.StartsWith("SECURITY:") ||
                    trimmedLine.StartsWith("ENCODING:") ||
                    trimmedLine.StartsWith("CHARSET:") ||
                    trimmedLine.StartsWith("COMPRESSION:") ||
                    trimmedLine.StartsWith("OLDFILEUID:") ||
                    trimmedLine.StartsWith("NEWFILEUID:") ||
                    trimmedLine.StartsWith("<!--"))
                {
                    continue;
                }

                if (trimmedLine == "<STMTTRN>")
                {
                    currentTransaction = new OfxTransactionData();
                    inTransaction = true;
                    continue;
                }

                if (trimmedLine == "</STMTTRN>" && currentTransaction != null)
                {
                    transacoes.Add(currentTransaction);
                    currentTransaction = null;
                    inTransaction = false;
                    continue;
                }

                if (inTransaction && currentTransaction != null)
                {
                    ProcessTransactionField(trimmedLine, currentTransaction);
                }
            }

            _logger.Info($"Total de transações processadas: {transacoes.Count}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao processar transações OFX: {ex.Message}", ex);
            throw;
        }
    }

    private void ProcessTransactionField(string line, OfxTransactionData transaction)
    {
        try
        {
            if (line.StartsWith("<DTPOSTED>"))
            {
                transaction.DataTransacao = FormatDate(ExtractTagValue(line, "DTPOSTED"));
            }
            else if (line.StartsWith("<TRNAMT>"))
            {
                var value = ExtractTagValue(line, "TRNAMT");
                if (decimal.TryParse(value,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out decimal valor))
                {
                    transaction.Valor = valor;
                }
                else
                {
                    _logger.Warn($"Não foi possível converter valor: {value}");
                }
            }
            else if (line.StartsWith("<MEMO>"))
            {
                transaction.Descricao = ExtractTagValue(line, "MEMO");
            }
        }
        catch (Exception ex)
        {
            _logger.Warn($"Erro ao processar campo: {line} - {ex.Message}");
        }
    }

    private string ExtractTagValue(string line, string tagName)
    {
        try
        {
            var startTag = "<" + tagName + ">";
            var endTag = "</" + tagName + ">";

            if (line.Contains(startTag) && line.Contains(endTag))
            {
                int startIndex = line.IndexOf(startTag) + startTag.Length;
                int endIndex = line.IndexOf(endTag);

                if (startIndex >= 0 && endIndex > startIndex)
                {
                    return line.Substring(startIndex, endIndex - startIndex).Trim();
                }
            }

            var pattern = $"<{tagName}>(.*?)</{tagName}>";
            var match = Regex.Match(line, pattern);
            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value.Trim();
            }

            _logger.Warn($"Não foi possível extrair valor da tag: {tagName} na linha: {line}");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.Error($"Erro ao extrair valor da tag {tagName}: {ex.Message}");
            return string.Empty;
        }
    }

    private string FormatDate(string ofxDate)
    {
        try
        {
            if (ofxDate.Length >= 8)
            {
                var year = ofxDate.Substring(0, 4);
                var month = ofxDate.Substring(4, 2);
                var day = ofxDate.Substring(6, 2);

                return $"{day}/{month}/{year}";
            }
            return ofxDate;
        }
        catch (Exception ex)
        {
            _logger.Warn($"Erro ao formatar data: {ofxDate} - {ex.Message}");
            return ofxDate;
        }
    }
}
