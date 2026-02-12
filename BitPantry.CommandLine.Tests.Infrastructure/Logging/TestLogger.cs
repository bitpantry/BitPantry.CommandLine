using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace BitPantry.CommandLine.Tests.Infrastructure.Logging
{
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

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        if (formatter != null)
        {
            var message = formatter(state, exception);
            _output.Log(_categoryName, new TestLoggerEntry(message, logLevel, exception, _categoryName));
        }
    }
}


    public class TestLoggerEntry
    {
        public DateTime Timestamp { get; } = DateTime.UtcNow;
        public LogLevel LogLevel { get; }
        public string Message { get; }
        public Exception Exception { get; }
        public string Category { get; }

        public TestLoggerEntry(string message, LogLevel logLevel = LogLevel.Information, Exception exception = null, string category = null)
        {
            Message = message;
            LogLevel = logLevel;
            Exception = exception;
            Category = category;
        }

        public override string ToString()
        {
            var result = $"[{LogLevel}] {Category}: {Message}";
            if (Exception != null)
                result += $"\n  Exception: {Exception.GetType().Name}: {Exception.Message}";
            return result;
        }
    }
}
