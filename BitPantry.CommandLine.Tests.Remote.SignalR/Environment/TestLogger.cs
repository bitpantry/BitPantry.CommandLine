using Microsoft.Extensions.Logging;

public class TestLogger<T> : ILogger<T>
{
    public List<TestLoggerEntry> LoggedMessages { get; } = new List<TestLoggerEntry>();

    public IDisposable BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        if (formatter != null)
            LoggedMessages.Add(new TestLoggerEntry(formatter(state, exception)));
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
