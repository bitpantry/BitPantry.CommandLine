# Building the Application

`CommandLineApplicationBuilder` is the fluent entry point for configuring and constructing a `CommandLineApplication`. It handles command registration, service configuration, console setup, and theme customization.

---

## Basic Usage

```csharp
using BitPantry.CommandLine;

var app = new CommandLineApplicationBuilder()
    .RegisterCommand<GreetCommand>()
    .RegisterCommand<DeployCommand>()
    .Build();

await app.RunInteractive();
```

---

## Builder API Summary

| Method | Description |
|--------|-------------|
| `RegisterCommand<T>()` | Register a single command type |
| `RegisterCommands(params Type[])` | Scan assemblies for commands |
| `ConfigureAutoComplete(Action)` | Register custom autocomplete handlers |
| `UsingConsole(IAnsiConsole)` | Provide a custom Spectre.Console instance |
| `UsingFileSystem(IFileSystem)` | Provide a custom file system abstraction |
| `UseHelpFormatter<T>()` | Replace the default help formatter |
| `ConfigurePrompt(Action)` | Set application name and prompt suffix |
| `ConfigureTheme(Action)` | Customize syntax highlighting and autocomplete styles |
| `Build()` | Construct the `CommandLineApplication` |

The `Services` property exposes the `IServiceCollection` for registering application services:

```csharp
var builder = new CommandLineApplicationBuilder();
builder.Services.AddSingleton<IMyService, MyService>();
```

---

## In This Section

| Page | Description |
|------|-------------|
| [Registering Commands](registering-commands.md) | Individual registration, assembly scanning, duplicate handling |
| [Dependency Injection](dependency-injection.md) | Service registration, constructor injection, command lifetimes |
| [Console Configuration](console-configuration.md) | Custom `IAnsiConsole`, `IConsoleService`, `IFileSystem` |
| [Prompt Configuration](prompt-configuration.md) | App name, suffix, multi-segment prompts |
| [Theme Configuration](theme-configuration.md) | Syntax highlighting colors, ghost text, menu styles |

---

## See Also

- [Registering Commands](registering-commands.md)
- [Dependency Injection](dependency-injection.md)
- [Defining Commands](../commands/index.md)
- [Running Commands](../running/index.md)
- [Builder API Reference](../api-reference/builder-api.md)
