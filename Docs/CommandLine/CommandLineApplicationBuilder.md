# CommandLineApplicationBuilder

`BitPantry.CommandLine.CommandLineApplicationBuilder`

[← Back to Implementer Guide](../ImplementerGuide.md)

The `CommandLineApplicationBuilder` class is used to configure and build a [CommandLineApplication](CommandLineApplication.md).

## Table of Contents

- [Quick Start](#quick-start)
- [Builder Properties](#builder-properties)
- [Builder Methods](#builder-methods)
- [Registering Commands](#registering-commands)
- [Configuring Services](#configuring-services)
- [Configuring the Console](#configuring-the-console)
- [Building the Application](#building-the-application)
- [Complete Example](#complete-example)

## Quick Start

```csharp
var builder = new CommandLineApplicationBuilder();

// Register commands from this assembly
builder.RegisterCommands(typeof(Program));

// Build and run
var app = builder.Build();
await app.Run(args);
```

## Builder Properties

| Property | Type | Description |
|----------|------|-------------|
| `Services` | `IServiceCollection` | Access to the dependency injection container for registering services |
| `Console` | `IAnsiConsole` | The Spectre.Console instance for terminal output |
| `ConsoleService` | `IConsoleService` | Low-level console abstraction (cursor position, clear, etc.) |
| `CommandRegistry` | `CommandRegistry` | Registry of all registered commands |
| `BuildActions` | `List<Action<IServiceProvider>>` | Actions executed after service provider is built |

## Builder Methods

| Method | Description |
|--------|-------------|
| `RegisterCommand<T>()` | Register a single command type |
| `RegisterCommand(Type)` | Register a command by Type |
| `RegisterGroup<T>()` | Register a group marker type |
| `RegisterGroup(Type)` | Register a group by Type |
| `RegisterCommands(params Type[])` | Register all commands and groups from assemblies containing the specified types |
| `RegisterCommands(Type[], Type[])` | Register commands and groups with exclusions |
| `UsingConsole(IAnsiConsole)` | Configure custom console implementation |
| `UsingConsole(IAnsiConsole, IConsoleService)` | Configure console with custom console service |
| `UsingFileSystem(IFileSystem)` | Configure custom file system abstraction |
| `UseHelpFormatter<T>()` | Configure custom help formatter (resolved from DI) |
| `UseHelpFormatter(IHelpFormatter)` | Configure custom help formatter (instance) |
| `Build()` | Build and return the application |

## Registering Commands

Commands must be registered before building the application. Unregistered commands cannot be executed.

### Register a Single Command

```csharp
builder.RegisterCommand<MyCommand>();
```

### Register by Type

```csharp
builder.RegisterCommand(typeof(MyCommand));
```

### Register All Commands from an Assembly

Pass a type from each assembly to search:

```csharp
// Register all commands in the assembly containing Program
builder.RegisterCommands(typeof(Program));

// Register from multiple assemblies
builder.RegisterCommands(typeof(Program), typeof(ExternalCommand));
```

### Register with Exclusions

Exclude specific command types from registration:

```csharp
builder.RegisterCommands(
    new[] { typeof(Program) },           // Assemblies to search
    new[] { typeof(InternalCommand) }    // Types to exclude
);
```

> **Note**: If a command is not registered, attempting to run it returns a `ResolutionError` result.

## Registering Groups

Groups organize commands into hierarchical structures. See [Command Groups](Commands.md#command-groups) for details on defining groups.

### Automatic Group Registration

When you register a command that references a group, the group is **automatically registered**:

```csharp
// The MathGroup is automatically registered when AddCommand is registered
builder.RegisterCommand<AddCommand>();
```

Where:
```csharp
[Group(Name = "math")]
public class MathGroup { }

[Command(Group = typeof(MathGroup), Name = "add")]
public class AddCommand : CommandBase { ... }
```

### Assembly Scanning

When using `RegisterCommands()`, both commands **and** groups are discovered automatically:

```csharp
// Discovers all [Group] classes AND all CommandBase classes
builder.RegisterCommands(typeof(Program));
```

The assembly scanner:
1. First registers all `[Group]` decorated classes
2. Then registers all `CommandBase` subclasses (linking them to their groups)

### Explicit Group Registration

You can also register groups explicitly:

```csharp
builder.RegisterGroup<MathGroup>();
```

This is useful for:
- Registering groups before their commands
- Registering groups that exist only for navigation (groups with subgroups but no direct commands)

## Configuring Services

The `Services` property provides access to the `IServiceCollection` for dependency injection:

```csharp
builder.Services.AddTransient<IMyService, MyService>();
builder.Services.AddSingleton<IConfiguration>(config);
builder.Services.AddLogging(logging => logging.AddConsole());
```

See [Dependency Injection](DependencyInjection.md) for detailed service configuration.

## Configuring the Console

[Spectre.Console](https://spectreconsole.net/) provides the `IAnsiConsole` interface for rich terminal output. By default, `AnsiConsole.Create()` is used.

### Custom Console

```csharp
var settings = new AnsiConsoleSettings
{
    ColorSystem = ColorSystemSupport.TrueColor
};
var console = AnsiConsole.Create(settings);

builder.UsingConsole(console);
```

### Custom Console Service

For advanced scenarios, you can provide a custom `IConsoleService`:

```csharp
builder.UsingConsole(console, new MyConsoleService());
```

## Building the Application

Call `Build()` to create the `CommandLineApplication`:

```csharp
var app = builder.Build();
```

During build:
1. All registered services are configured
2. Commands are added to the service container as transient
3. The console and logging are finalized
4. Build actions are executed

## Complete Example

```csharp
using BitPantry.CommandLine;
using Microsoft.Extensions.Logging;

var builder = new CommandLineApplicationBuilder();

// Configure services
builder.Services.AddTransient<IDataService, DataService>();
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole();
});

// Register commands
builder.RegisterCommands(typeof(Program));

// Build and run
var app = builder.Build();
var result = await app.Run(args);

return result.ResultCode == RunResultCode.Success ? 0 : 1;
```

---

## See Also

- [CommandLineApplication](CommandLineApplication.md) - Running commands
- [Commands](Commands.md) - Defining command types
- [CommandBase](CommandBase.md) - Command base class
- [Dependency Injection](DependencyInjection.md) - Configuring services
- [IAnsiConsole](IAnsiConsole.md) - Console output
- [RunResult](RunResult.md) - Command execution results