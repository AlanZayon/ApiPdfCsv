using System.Text.RegularExpressions;

namespace ApiPdfCsv.Shared.Validation;

public static class UploadFileValidator
{
    private const long MaxFileSizeBytes = 104_857_600;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".ofx"
    };

    public static void ValidateFile(IFormFile file, string extension)
    {
        if (file.Length == 0)
        {
            throw new InvalidDataException("Arquivo vazio.");
        }

        if (file.Length > MaxFileSizeBytes)
        {
            throw new InvalidDataException("Arquivo excede o tamanho máximo permitido (100 MB).");
        }

        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidDataException("Tipo de arquivo não suportado. Use apenas PDF ou OFX.");
        }
    }

    public static async Task ValidateContentAsync(string filePath, string extension)
    {
        await using var stream = File.OpenRead(filePath);
        var header = new byte[8];
        var read = await stream.ReadAsync(header.AsMemory(0, 8));

        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            if (read < 4 || header[0] != 0x25 || header[1] != 0x50 || header[2] != 0x44 || header[3] != 0x46)
            {
                throw new InvalidDataException("Conteúdo do arquivo não corresponde a um PDF válido.");
            }
            return;
        }

        if (extension.Equals(".ofx", StringComparison.OrdinalIgnoreCase))
        {
            stream.Position = 0;
            using var reader = new StreamReader(stream, leaveOpen: true);
            var firstLine = (await reader.ReadLineAsync()) ?? string.Empty;
            if (!firstLine.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase)
                && !firstLine.Contains("OFX", StringComparison.OrdinalIgnoreCase))
            {
                var buffer = new char[512];
                stream.Position = 0;
                await reader.ReadAsync(buffer, 0, buffer.Length);
                var content = new string(buffer);
                if (!content.Contains("OFXHEADER", StringComparison.OrdinalIgnoreCase)
                    && !content.Contains("<OFX>", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("Conteúdo do arquivo não corresponde a um OFX válido.");
                }
            }
        }
    }

    public static void ValidateCnpj(string cnpj)
    {
        if (string.IsNullOrWhiteSpace(cnpj))
        {
            throw new InvalidDataException("CNPJ é obrigatório para arquivos OFX.");
        }

        var digits = Regex.Replace(cnpj, @"\D", "");
        if (digits.Length != 14)
        {
            throw new InvalidDataException("CNPJ deve conter 14 dígitos.");
        }
    }
}
