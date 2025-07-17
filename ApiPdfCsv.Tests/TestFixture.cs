using System;
using Serilog;
using Serilog.Sinks.TestCorrelator;

public class TestFixture : IDisposable
{
    public TestFixture()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Warning()
            .WriteTo.TestCorrelator()
            .CreateLogger();
    }
    
    public void Dispose()
    {
        Log.CloseAndFlush();
    }
}
