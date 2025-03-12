using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using System.Collections.Concurrent;

public class TestLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, TestLogger> _loggers = new ConcurrentDictionary<string, TestLogger>();
    private readonly ILoggerFactory _loggerFactory;
    private readonly TestLoggerOutput _output;

    public TestLoggerProvider(ILoggerFactory loggerFactory, TestLoggerOutput output)
    {
        _loggerFactory = loggerFactory;
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, name => new TestLogger(_loggerFactory, name, _output));
    }

    public void Dispose()
    {
        _loggers.Clear();
    }
}
