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
| `InstallModule<T>` | `InstallModule<T>() where T : ICommandModule, new()` | Install a command module |
| `InstallModule<T>` | `InstallModule<T>(Action<T>)` | Install module with configuration |
| `InstallModulesFromDirectory` | `InstallModulesFromDirectory(string path)` | Load modules from plugins directory |
| `InstallModuleFromAssembly` | `InstallModuleFromAssembly(string path)` | Load module from assembly file |
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

Inherits `RegisterCommand<T>()`, `RegisterCommands()`, `ConfigureAutoComplete()`, and all `InstallModule*` methods.

---

## Module Installation Methods

### InstallModule&lt;T&gt;()

Installs a command module that registers its commands, services, and autocomplete handlers.

```csharp
builder.InstallModule<MyModule>();
```

### InstallModule&lt;T&gt;(Action&lt;T&gt;)

Installs a module with configuration. The configure callback runs before `Configure()`.

```csharp
builder.InstallModule<MyModule>(m => m.Option = "value");
```

### InstallModulesFromDirectory(string)

Loads all plugin modules from subdirectories of the specified path. Each subdirectory should contain a DLL with the same name as the directory (e.g., `plugins/MyModule/MyModule.dll`).

```csharp
builder.InstallModulesFromDirectory("./plugins");
```

If the directory doesn't exist, this is a no-op (no error thrown).

### InstallModuleFromAssembly(string)

Loads module(s) from a single assembly file.

```csharp
builder.InstallModuleFromAssembly("./plugins/MyModule/MyModule.dll");
```

Throws `FileNotFoundException` if the assembly doesn't exist.

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
- [Plugins Guide](../plugins/index.md)
- [Interfaces](interfaces.md)
