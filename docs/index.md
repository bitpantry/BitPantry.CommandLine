# BitPantry.CommandLine

[![Build Status](https://img.shields.io/github/actions/workflow/status/bitpantry/BitPantry.CommandLine/release-unified.yml?branch=master)](https://github.com/bitpantry/BitPantry.CommandLine/actions)
[![NuGet](https://img.shields.io/nuget/v/BitPantry.CommandLine)](https://www.nuget.org/packages/BitPantry.CommandLine)
[![License](https://img.shields.io/github/license/bitpantry/BitPantry.CommandLine)](LICENSE)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-blue)](https://dotnet.microsoft.com/)

A .NET 8 framework for building interactive command-line applications with attribute-driven command definitions, real-time autocomplete, syntax highlighting, and optional remote execution over SignalR.

---

## Features

- **Attribute-driven commands** — Define commands, arguments, flags, and groups using simple C# attributes
- **Dependency injection** — Full `IServiceCollection` / `IServiceProvider` integration throughout
- **Autocomplete** — Inline ghost text and dropdown menu suggestions, extensible with custom handlers
- **Syntax highlighting** — Real-time token-based input coloring with a configurable theme
- **Command piping** — Chain commands with `|` and pass typed output between them
- **Remote execution** — Execute commands on a remote server transparently over SignalR
- **File transfers** — Upload and download files between client and server with progress, checksums, and glob support
- **Server profiles** — Save, manage, and switch between server connections with encrypted credential storage
- **Testable** — Companion `VirtualConsole` packages for automated UX testing

---

## NuGet Packages

| Package | Version | Description |
|---------|---------|-------------|
| `BitPantry.CommandLine` | [![NuGet](https://img.shields.io/nuget/v/BitPantry.CommandLine)](https://www.nuget.org/packages/BitPantry.CommandLine) | Core framework — commands, autocomplete, syntax highlighting, DI |
| `BitPantry.CommandLine.Remote.SignalR` | [![NuGet](https://img.shields.io/nuget/v/BitPantry.CommandLine.Remote.SignalR)](https://www.nuget.org/packages/BitPantry.CommandLine.Remote.SignalR) | Shared protocol library for client/server remote execution |
| `BitPantry.CommandLine.Remote.SignalR.Client` | [![NuGet](https://img.shields.io/nuget/v/BitPantry.CommandLine.Remote.SignalR.Client)](https://www.nuget.org/packages/BitPantry.CommandLine.Remote.SignalR.Client) | Client-side SignalR integration — connect, profiles, file transfers |
| `BitPantry.CommandLine.Remote.SignalR.Server` | [![NuGet](https://img.shields.io/nuget/v/BitPantry.CommandLine.Remote.SignalR.Server)](https://www.nuget.org/packages/BitPantry.CommandLine.Remote.SignalR.Server) | Server-side ASP.NET integration — hub, JWT auth, sandboxed file system |
| `BitPantry.VirtualConsole` | N/A | Virtual terminal emulator for testing (.NET Standard 2.0) |
| `BitPantry.VirtualConsole.Testing` | N/A | FluentAssertions extensions for VirtualConsole |

---

## Quick Example

```csharp
using BitPantry.CommandLine;
using BitPantry.CommandLine.API;

// Define a command
[Command(Name = "greet")]
[Description("Greets a user by name")]
public class GreetCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("The name to greet")]
    public string Name { get; set; } = "";

    [Argument(Name = "loud")]
    [Flag]
    public bool Loud { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        var message = $"Hello, {Name}!";
        Console.MarkupLine(Loud ? $"[bold]{message.ToUpper()}[/]" : message);
    }
}

// Build and run
var app = new CommandLineApplicationBuilder()
    .RegisterCommand<GreetCommand>()
    .Build();

await app.RunInteractive();
```

```
app> greet World
Hello, World!

app> greet World --loud
HELLO, WORLD!
```

---

## Documentation

| Section | Description |
|---------|-------------|
| [Solution Architecture](architecture.md) | Project layout, dependencies, and package boundaries |
| [Getting Started](quick-start.md) | Install, define a command, build, and run |
| [Defining Commands](commands/index.md) | Attributes, arguments, flags, groups, and error handling |
| [Building the Application](building/index.md) | Builder API, DI, console/prompt/theme configuration |
| [Running Commands](running/index.md) | Execution modes, global arguments, pipeline, piping, help |
| [Autocomplete](autocomplete/index.md) | Built-in, attribute, and type handlers; rendering |
| [Syntax Highlighting](syntax-highlighting.md) | Token-based input coloring and theme integration |
| [Remote Execution](remote/index.md) | SignalR client/server setup, profiles, file transfers |
| [VirtualConsole](virtual-console/index.md) | Virtual terminal emulator (companion package) |
| [Testing Guide](testing/index.md) | Unit, integration, and UX testing strategies |
| [API Reference](api-reference/index.md) | Attributes, builder API, component model, interfaces |

---

## See Also

- [Solution Architecture](architecture.md)
- [Getting Started](quick-start.md)
- [Defining Commands](commands/index.md)
- [Building the Application](building/index.md)
