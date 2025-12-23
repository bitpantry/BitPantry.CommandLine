# BitPantry.CommandLine Documentation

Welcome to the BitPantry.CommandLine documentation. This library provides a comprehensive framework for building command-line applications in .NET, with support for local and remote (SignalR-based) command execution.

## Choose Your Path

### 📘 For Implementers (Developers Building CLI Apps)

If you're a developer looking to build command-line applications using BitPantry.CommandLine, start here:

**[→ Implementer Guide](ImplementerGuide.md)** - Complete guide to building CLI applications

Quick links:
- [Quick Start](CommandLine/QuickStart.md) - Get up and running in 5 minutes
- [Defining Commands](CommandLine/Commands.md) - Create commands with arguments and options
- [Configuration](CommandLine/CommandLineApplicationBuilder.md) - Configure your CLI application
- [Remote CLI Server](Remote/CommandLineServer.md) - Host commands over SignalR

### 📗 For End Users (Operating CLI Applications)

If you're using a CLI application built with BitPantry.CommandLine and need to understand how to operate it:

**[→ End User Guide](EndUserGuide.md)** - Learn to use CLI applications effectively

Quick links:
- [Command Syntax](CommandLine/CommandSyntax.md) - How to write commands
- [REPL Features](CommandLine/REPL.md) - Interactive mode features
- [Built-in Commands](CommandLine/BuiltInCommands.md) - Commands available out of the box

## Package Overview

| Package | Purpose |
|---------|---------|
| `BitPantry.CommandLine` | Core library for building CLI applications |
| `BitPantry.CommandLine.Remote.SignalR.Server` | Host CLI commands over SignalR |
| `BitPantry.CommandLine.Remote.SignalR.Client` | Connect to remote CLI servers |

## Installation

```
NuGet\Install-Package BitPantry.CommandLine 
```

For remote CLI support:
```
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Server
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Client
```

## See Also

- [GitHub Repository](https://github.com/bitpantry/BitPantry.CommandLine)
- [NuGet Package](https://www.nuget.org/packages/BitPantry.CommandLine)
