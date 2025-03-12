using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

public class TestLogger : ILogger
{
    private readonly string _categoryName;
    private readonly ILogger _logger;
    private readonly TestLoggerOutput _output;

    public TestLogger(ILoggerFactory loggerFactory, string categoryName, TestLoggerOutput output)
    {
        _categoryName = categoryName;
        _logger = loggerFactory.CreateLogger(categoryName);
        _output = output;
    }

    public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter != null)
            _output.Log(_categoryName, new TestLoggerEntry(formatter(state, exception)));
    }
}


public class TestLoggerEntry
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
    public string Message { get; }

    public TestLoggerEntry(string message)
    {
        Message = message;
    }
}
