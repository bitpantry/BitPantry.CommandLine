# Dependency Injection

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine has full support for dependency injection (DI) using `Microsoft.Extensions.DependencyInjection`. Services can be registered during application setup and injected into commands via constructor injection.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Overview](#overview)
- [Registering Services](#registering-services)
- [Injecting Services into Commands](#injecting-services-into-commands)
- [Built-in Services](#built-in-services)
- [Complete Example](#complete-example)
- [Accessing the Service Provider](#accessing-the-service-provider)
- [Best Practices](#best-practices)
- [Scoped Services Pattern](#scoped-services-pattern)
- [See Also](#see-also)

## Prerequisites

- Familiarity with [Microsoft.Extensions.DependencyInjection](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- Understanding of [CommandBase](CommandBase.md) and command structure

## Overview

The `CommandLineApplicationBuilder` exposes a `Services` property that provides access to the `IServiceCollection`. You can register any services your commands need, and they will be automatically injected when commands are activated.

## Registering Services

Register services in your application startup:

```csharp
var builder = new CommandLineApplicationBuilder();

// Register your services
builder.Services.AddTransient<IMyService, MyService>();
builder.Services.AddSingleton<IConfiguration>(config);
builder.Services.AddScoped<IDataContext, DataContext>();

// Register commands
builder.RegisterCommands(typeof(Program));

var app = builder.Build();
```

### Service Lifetimes

| Lifetime | Description |
|----------|-------------|
| Transient | New instance created each time requested |
| Scoped | One instance per scope (each command execution) |
| Singleton | Single instance for the application lifetime |

> **Note**: Commands themselves are registered as **transient** by default. Each command execution creates a new instance.

## Injecting Services into Commands

Use constructor injection to receive services in your commands:

```csharp
using BitPantry.CommandLine.API;

[Command(Name = "process")]
[Description("Processes data using injected services")]
public class ProcessCommand : CommandBase
{
    private readonly IDataService _dataService;
    private readonly ILogger<ProcessCommand> _logger;

    public ProcessCommand(IDataService dataService, ILogger<ProcessCommand> logger)
    {
        _dataService = dataService;
        _logger = logger;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        _logger.LogInformation("Starting data processing");
        
        var result = await _dataService.ProcessAsync();
        
        Console.MarkupLine($"[green]Processed {result.Count} items[/]");
    }
}
```

## Built-in Services

The framework automatically registers these services:

| Service | Lifetime | Description |
|---------|----------|-------------|
| `IAnsiConsole` | Singleton | Console output (Spectre.Console) |
| `IFileSystem` | Singleton | File system abstraction |
| `CommandRegistry` | Singleton | Registered command definitions |
| `IServerProxy` | Singleton | Remote server connection (when using SignalR) |
| `ILoggerFactory` | Singleton | Logging factory (if logging configured) |

## Complete Example

```csharp
// Services
public interface IGreetingService
{
    string GetGreeting(string name);
}

public class GreetingService : IGreetingService
{
    public string GetGreeting(string name) => $"Hello, {name}!";
}

// Command with injected service
[Command(Name = "greet")]
[Description("Greets a user")]
public class GreetCommand : CommandBase
{
    private readonly IGreetingService _greetingService;

    public GreetCommand(IGreetingService greetingService)
    {
        _greetingService = greetingService;
    }

    [Argument]
    public string Name { get; set; } = "World";

    public void Execute(CommandExecutionContext ctx)
    {
        var greeting = _greetingService.GetGreeting(Name);
        Console.MarkupLine($"[green]{greeting}[/]");
    }
}

// Application setup
var builder = new CommandLineApplicationBuilder();

builder.Services.AddTransient<IGreetingService, GreetingService>();
builder.RegisterCommands(typeof(Program));

var app = builder.Build();
await app.Run(args);
```

## Accessing the Service Provider

After building the application, you can access the service provider for resolving services outside of commands:

```csharp
var app = builder.Build();

// Access services directly if needed
var service = app.Services.GetRequiredService<IMyService>();
```

## Best Practices

1. **Prefer constructor injection** - It makes dependencies explicit and testable
2. **Register commands after services** - Ensure services are available when commands are registered
3. **Use appropriate lifetimes** - Transient for stateless services, singleton for shared state
4. **Avoid service locator pattern** - Don't resolve services manually in command methods

## Scoped Services Pattern

For services that need to maintain state across a single command execution (but not between commands), use a scoped pattern:

```csharp
// Define a scoped service
public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    Task SaveChangesAsync();
}

// Register as transient (new instance per command)
builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

// Use in command
[Command(Name = "createuser")]
public class CreateUserCommand : CommandBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateUserCommand(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task Execute(CommandExecutionContext ctx)
    {
        var user = new User { Name = "John" };
        _unitOfWork.Users.Add(user);
        await _unitOfWork.SaveChangesAsync();
        
        Console.MarkupLine("[green]User created![/]");
    }
}
```

Since commands are transient, each command execution gets its own service instances, providing natural scoping.

## See Also

- [CommandBase](CommandBase.md) - Command base class
- [Commands](Commands.md) - Complete command definition guide
- [Logging](Logging.md) - Configuring logging with DI
- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) - Application configuration
