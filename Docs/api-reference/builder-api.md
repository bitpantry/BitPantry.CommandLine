# Builder API

Reference for `CommandLineApplicationBuilder` and `CommandLineServerOptions`.

---

## CommandLineApplicationBuilder

The primary entry point for building a `CommandLineApplication`.

### Constructor

```csharp
public CommandLineApplicationBuilder()
```

Initializes with:
- UTF-8 output encoding
- Default `AnsiConsole`
- New `ServiceCollection`
- `IFileSystem` registration
- `NoopServerProxy` as default `IServerProxy`

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `Services` | `IServiceCollection` | DI container for registering application services |
| `Console` | `IAnsiConsole` | The Spectre.Console instance (configurable via `UsingConsole`) |
| `ConsoleService` | `IConsoleService` | Low-level console operations (default: `SystemConsoleService`) |
| `BuildActions` | `List<Action<IServiceProvider>>` | Post-build callbacks |

### Fluent Methods

All methods return `CommandLineApplicationBuilder` for chaining.

| Method | Signature | Description |
|--------|-----------|-------------|
| `RegisterCommand<T>` | `RegisterCommand<T>() where T : CommandBase` | Register a single command |
| `RegisterCommand` | `RegisterCommand(Type type)` | Register a command by type |
| `RegisterCommands` | `RegisterCommands(params Type[] assemblyTargetTypes)` | Scan assemblies for commands |
| `RegisterCommands` | `RegisterCommands(Type[], Type[])` | Scan with exclusions |
| `UsingConsole` | `UsingConsole(IAnsiConsole console)` | Set custom console |
| `UsingConsole` | `UsingConsole(IAnsiConsole console, IConsoleService svc)` | Set console and console service |
| `UsingFileSystem` | `UsingFileSystem(IFileSystem fileSystem)` | Set custom file system |
| `UseHelpFormatter<T>` | `UseHelpFormatter<T>() where T : class, IHelpFormatter` | Set custom help formatter type |
| `UseHelpFormatter` | `UseHelpFormatter(IHelpFormatter formatter)` | Set custom help formatter instance |
| `ConfigurePrompt` | `ConfigurePrompt(Action<PromptOptions> configure)` | Configure prompt display |
| `ConfigureTheme` | `ConfigureTheme(Action<Theme> configure)` | Configure visual theme |
| `ConfigureAutoComplete` | `ConfigureAutoComplete(Action<IAutoCompleteHandlerRegistryBuilder>)` | Register autocomplete handlers |
| `Build` | `CommandLineApplication Build()` | Build the application |

---

## CommandLineApplication

| Member | Type | Description |
|--------|------|-------------|
| `Services` | `IServiceProvider` | Built service provider |
| `LastRunResult` | `RunResult` | Result of the most recent execution |
| `RunInteractive()` | `Task` | Start REPL loop |
| `RunOnce(string)` | `Task<RunResult>` | Execute single command |
| `Dispose()` | `void` | Clean up resources |

---

## CommandLineServerOptions

Server-side builder for ASP.NET integration.

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Services` | `IServiceCollection` | — | Server DI container |
| `HubUrlPattern` | `string` | `"/cli"` | SignalR hub URL path |
| `FileTransferOptions` | `FileTransferOptions` | — | File transfer config |

Inherits `RegisterCommand<T>()`, `RegisterCommands()`, and `ConfigureAutoComplete()`.

---

## PromptOptions

| Method | Return | Description |
|--------|--------|-------------|
| `Name(string)` | `PromptOptions` | Set the prompt application name |
| `WithSuffix(string)` | `PromptOptions` | Set the prompt suffix (default: `"> "`) |

Both support Spectre.Console markup.

---

## See Also

- [Building the Application](../building/index.md)
- [Registering Commands](../building/registering-commands.md)
- [Dependency Injection](../building/dependency-injection.md)
- [Interfaces](interfaces.md)
