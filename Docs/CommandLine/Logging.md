# Logging

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine integrates with `Microsoft.Extensions.Logging` for structured logging. You can configure logging providers and inject loggers into your commands.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Overview](#overview)
- [Configuring Logging](#configuring-logging)
- [Injecting Loggers into Commands](#injecting-loggers-into-commands)
- [Log Levels](#log-levels)
- [Complete Example](#complete-example)
- [Third-Party Logging Providers](#third-party-logging-providers)
- [Best Practices](#best-practices)
- [See Also](#see-also)

## Prerequisites

- Familiarity with [Microsoft.Extensions.Logging](https://docs.microsoft.com/en-us/dotnet/core/extensions/logging)
- Understanding of [Dependency Injection](DependencyInjection.md)

## Overview

The framework uses the standard .NET logging abstraction. If no logging is configured, a null logger is used automatically. You can configure any logging provider (Console, Debug, Serilog, etc.) via the `Services` property.

## Configuring Logging

Configure logging during application setup:

```csharp
var builder = new CommandLineApplicationBuilder();

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole();
});

builder.RegisterCommands(typeof(Program));
var app = builder.Build();
```

### Filtering Log Output

Use filters to control which categories and levels are logged:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.AddFilter((provider, category, logLevel) =>
    {
        // Log debug and above for your namespace
        if (category.StartsWith("MyApp"))
            return logLevel >= LogLevel.Debug;
        
        // Log warnings and above for framework
        if (category.StartsWith("BitPantry"))
            return logLevel >= LogLevel.Warning;
        
        // Suppress other logging
        return false;
    });
    
    logging.AddConsole();
});
```

## Injecting Loggers into Commands

Use constructor injection to get a typed logger:

```csharp
using BitPantry.CommandLine.API;
using Microsoft.Extensions.Logging;

[Command(Name = "process")]
[Description("Processes data with logging")]
public class ProcessCommand : CommandBase
{
    private readonly ILogger<ProcessCommand> _logger;

    public ProcessCommand(ILogger<ProcessCommand> logger)
    {
        _logger = logger;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        _logger.LogDebug("Starting command execution");
        
        try
        {
            _logger.LogInformation("Processing started");
            
            // Do work...
            
            _logger.LogInformation("Processing completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing failed");
            Console.MarkupLine("[red]An error occurred[/]");
        }
    }
}
```

## Log Levels

| Level | Use Case |
|-------|----------|
| `Trace` | Verbose diagnostic information |
| `Debug` | Development/debugging information |
| `Information` | General operational events |
| `Warning` | Unexpected but handled situations |
| `Error` | Errors that prevent an operation |
| `Critical` | System failures |

## Complete Example

```csharp
using BitPantry.CommandLine;
using BitPantry.CommandLine.API;
using Microsoft.Extensions.Logging;

// Command with logging
[Command(Name = "fetch")]
[Description("Fetches data from remote server")]
public class FetchCommand : CommandBase
{
    private readonly ILogger<FetchCommand> _logger;
    private readonly IDataService _dataService;

    public FetchCommand(ILogger<FetchCommand> logger, IDataService dataService)
    {
        _logger = logger;
        _dataService = dataService;
    }

    [Argument]
    public string Source { get; set; }

    public async Task Execute(CommandExecutionContext ctx)
    {
        _logger.LogInformation("Fetching data from {Source}", Source);
        
        try
        {
            var data = await _dataService.FetchAsync(Source);
            
            _logger.LogDebug("Retrieved {Count} records", data.Count);
            Console.MarkupLine($"[green]Fetched {data.Count} records[/]");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from {Source}", Source);
            Console.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }
}

// Application setup
var builder = new CommandLineApplicationBuilder();

builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole(options =>
    {
        options.TimestampFormat = "[HH:mm:ss] ";
    });
});

builder.Services.AddTransient<IDataService, DataService>();
builder.RegisterCommands(typeof(Program));

var app = builder.Build();
await app.Run(args);
```

## Third-Party Logging Providers

You can integrate any logging provider that supports `Microsoft.Extensions.Logging`:

### Serilog

```csharp
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Services.AddLogging(logging =>
{
    logging.AddSerilog();
});
```

### NLog

```csharp
using NLog.Extensions.Logging;

builder.Services.AddLogging(logging =>
{
    logging.AddNLog();
});
```

## Best Practices

1. **Use structured logging** - Include relevant data in log messages via templates
2. **Choose appropriate levels** - Debug for development, Information for operations
3. **Don't log sensitive data** - Avoid logging passwords, tokens, or PII
4. **Log context** - Include correlation IDs for tracing across commands

## See Also

- [Dependency Injection](DependencyInjection.md) - Injecting services
- [Commands](Commands.md) - Command structure and execution
- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) - Application setup
