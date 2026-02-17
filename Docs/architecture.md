# Solution Architecture

An overview of the projects in the BitPantry.CommandLine solution, their dependencies, and the boundaries between them.

---

## Project Map

```
BitPantry.CommandLine                         Core framework (net8.0)
├── BitPantry.CommandLine.Remote.SignalR       Shared protocol library (net8.0)
│   ├── .Remote.SignalR.Client                Client-side SignalR package (net8.0)
│   └── .Remote.SignalR.Server                Server-side ASP.NET package (net8.0)
│
BitPantry.VirtualConsole                      Virtual terminal emulator (netstandard2.0)
└── BitPantry.VirtualConsole.Testing          FluentAssertions extensions (netstandard2.0)
```

---

## Core — `BitPantry.CommandLine`

The foundation of the framework. Provides:

- **Command model** — `CommandBase`, attribute-driven metadata (`[Command]`, `[Argument]`, `[Flag]`, etc.)
- **Component model** — `CommandInfo`, `ArgumentInfo`, `GroupInfo` runtime metadata
- **Processing pipeline** — Parse → Resolve → Activate → Execute
- **Autocomplete engine** — Ghost text, menu rendering, handler resolution
- **Syntax highlighter** — Token-based real-time input coloring
- **Builder API** — `CommandLineApplicationBuilder` for fluent configuration
- **Application runtime** — `CommandLineApplication` with `RunInteractive()` and `RunOnce()`

**Key dependencies:**

| Dependency | Role |
|------------|------|
| `Spectre.Console` | Rich console output, ANSI rendering, markup |
| `BitPantry.Parsing.Strings` | String-to-type conversion for argument values |
| `Microsoft.Extensions.DependencyInjection` | Service container for commands and services |
| `System.IO.Abstractions` | Testable file system abstraction |

---

## Shared Protocol — `BitPantry.CommandLine.Remote.SignalR`

A shared library referenced by both client and server. Contains:

- **Envelope types** — `ServerRequest`, `ResponseMessage`, `RunRequest`, `RunResponse`, `AutoCompleteRequest`
- **RPC infrastructure** — `RpcMessageRegistry`, `RpcMessageContext`, `IRpcScope`
- **Serialization** — `RemoteJsonOptions`, custom JSON converters for `CommandInfo`, `ArgumentInfo`, `GroupInfo`
- **Constants** — `SignalRMethodNames`, `ServiceEndpointNames`

This library has no dependency on ASP.NET or SignalR client packages, keeping it transport-layer neutral.

---

## Client — `BitPantry.CommandLine.Remote.SignalR.Client`

Extends the core builder with `ConfigureSignalRClient()`. Provides:

- **`SignalRServerProxy`** — `IServerProxy` implementation using SignalR hub connection
- **`ConnectionService`** — Shared authentication and connection logic (401 → token acquisition → retry)
- **`SignalRAutoConnectHandler`** — `IAutoConnectHandler` for automatic connection in `RunOnce()` mode
- **Profile management** — `ProfileManager`, `CredentialStore` (DPAPI-encrypted), profile commands
- **File transfers** — `FileTransferService` for streaming HTTP uploads/downloads with progress
- **Prompt segments** — `ServerConnectionSegment`, `ProfilePromptSegment`
- **Built-in commands** — `server connect`, `server disconnect`, `server upload`, `server download`, `server profile *`

---

## Server — `BitPantry.CommandLine.Remote.SignalR.Server`

An ASP.NET integration package. Provides:

- **`CommandLineHub`** — SignalR hub for receiving client requests
- **`ServerLogic`** — Constructs a transient `CommandLineApplicationCore` per request for command execution
- **JWT authentication** — `TokenRequestEndpointService`, `TokenValidationMiddleware`, `JwtAuthOptions`
- **Auth contracts** — `IApiKeyStore`, `IRefreshTokenStore` interfaces for developer implementation
- **File sandboxing** — `SandboxedFileSystem`, `PathValidator`, `FileSizeValidator`, `ExtensionValidator`
- **File transfer endpoints** — `FileTransferEndpointService` for upload/download HTTP endpoints

---

## VirtualConsole — Companion Packages

Separate NuGet packages targeting .NET Standard 2.0 — not part of the core framework, but designed for testing CLI applications built with it.

| Package | Description |
|---------|-------------|
| `BitPantry.VirtualConsole` | Virtual terminal emulator — ANSI escape sequence processing, screen buffer, cursor tracking. Zero external dependencies. |
| `BitPantry.VirtualConsole.Testing` | `VirtualConsoleAssertions` (FluentAssertions), `VirtualConsoleAnsiAdapter` (Spectre.Console bridge), `KeyboardSimulator`, `TestConsoleInput` |

---

## Dependency Flow

```
                           ┌────────────────────┐
                           │  BitPantry.        │
                           │  CommandLine       │ ◄───── Core
                           └────────┬───────────┘
                                    │
                           ┌────────┴───────────┐
                           │  Remote.SignalR     │ ◄───── Shared Protocol
                           └───┬────────────┬───┘
                               │            │
                  ┌────────────┴─┐    ┌─────┴────────────┐
                  │  .Client     │    │  .Server          │
                  └──────────────┘    └──────────────────-┘

   ┌──────────────────────────┐
   │  BitPantry.VirtualConsole│ ◄───── Companion
   └────────────┬─────────────┘
                │
   ┌────────────┴─────────────┐
   │  .VirtualConsole.Testing │
   └──────────────────────────┘
```

---

## See Also

- [Introduction & Overview](index.md)
- [Getting Started](quick-start.md)
- [Building the Application](building/index.md)
- [The Processing Pipeline](running/processing-pipeline.md)
- [Remote Execution](remote/index.md)
