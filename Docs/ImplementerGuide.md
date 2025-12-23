# Implementer Guide

This guide is for developers building command-line applications using BitPantry.CommandLine. Follow the learning path below to master the framework.

[← Back to Documentation Index](index.md)

## Table of Contents

- [Getting Started](#getting-started)
- [Defining Commands](#defining-commands)
- [Configuration](#configuration)
- [Advanced Topics](#advanced-topics)
- [Remote CLI](#remote-cli)

---

## Getting Started

Start here to create your first CLI application.

| Topic | Description |
|-------|-------------|
| [Quick Start](CommandLine/QuickStart.md) | Create a working CLI app in 5 minutes |
| [CommandLineApplicationBuilder](CommandLine/CommandLineApplicationBuilder.md) | Configure and build your application |
| [REPL Mode](CommandLine/REPL.md) | Run as an interactive shell |

---

## Defining Commands

Learn how to create powerful, flexible commands.

| Topic | Description |
|-------|-------------|
| [Commands](CommandLine/Commands.md) | Complete guide to command definition |
| [CommandBase](CommandLine/CommandBase.md) | Base class for all commands |
| [Arguments](CommandLine/ArgumentInfo.md) | Define command arguments and options |
| [AutoComplete](CommandLine/AutoComplete.md) | Add tab-completion to arguments |
| [Command Syntax](CommandLine/CommandSyntax.md) | How commands are parsed and executed |

---

## Configuration

Configure your CLI application for different scenarios.

| Topic | Description |
|-------|-------------|
| [CommandLineApplicationBuilder](CommandLine/CommandLineApplicationBuilder.md) | Builder pattern configuration |
| [Dependency Injection](CommandLine/DependencyInjection.md) | Inject services into commands |
| [Logging](CommandLine/Logging.md) | Configure logging with ILoggerFactory |
| [Command Registry](CommandLine/CommandRegistry.md) | Manage command registration |

---

## Advanced Topics

Dive deeper into the framework's capabilities.

| Topic | Description |
|-------|-------------|
| [Command Pipeline](CommandLine/CommandPipeline.md) | Input/output streaming between commands |
| [Command Execution Context](CommandLine/CommandExecutionContext.md) | Access execution state and services |
| [IAnsiConsole](CommandLine/IAnsiConsole.md) | Console output with Spectre.Console |
| [RunResult](CommandLine/RunResult.md) | Handle command execution results |

---

## Remote CLI

Build client-server CLI applications using SignalR.

| Topic | Description |
|-------|-------------|
| [Command Line Server](Remote/CommandLineServer.md) | Host commands over SignalR |
| [Client Configuration](Remote/Client.md) | Connect to remote CLI servers |
| [SignalR Client Options](Remote/SignalRClientOptions.md) | Configure client connection settings |
| [JWT Authentication](Remote/JwtAuthOptions.md) | Secure your remote CLI |
| [File System](Remote/FileSystem.md) | File operations in remote commands |
| [Troubleshooting](Remote/Troubleshooting.md) | Common issues and solutions |

---

## Next Steps

1. **Start with the [Quick Start](CommandLine/QuickStart.md)** to create your first command
2. **Learn [Commands](CommandLine/Commands.md)** to define arguments and options
3. **Configure [Dependency Injection](CommandLine/DependencyInjection.md)** for service access
4. **Add [AutoComplete](CommandLine/AutoComplete.md)** for better user experience

## See Also

- [End User Guide](EndUserGuide.md) - For users operating CLI applications
- [Built-in Commands](CommandLine/BuiltInCommands.md) - Commands included with the framework
