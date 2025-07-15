using Serilog;

namespace ApiPdfCsv.Shared.Logging;

public class Logger : ILogger
{
    public void Info(string message) => Log.Information(message);
    public void Warn(string message) => Log.Warning(message);
    public void Error(string message, Exception? ex = null) => Log.Error(ex, message);
}