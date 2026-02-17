# Remote Execution (SignalR)

BitPantry.CommandLine supports executing commands on a remote server, transparently, over SignalR. Commands registered on the server appear in the client's command registry as if they were local.

---

## Architecture

Three NuGet packages provide remote execution:

| Package | Role |
|---------|------|
| `BitPantry.CommandLine.Remote.SignalR` | Shared protocol — envelopes, RPC, serialization |
| `BitPantry.CommandLine.Remote.SignalR.Client` | Client-side — connection, profiles, file transfers |
| `BitPantry.CommandLine.Remote.SignalR.Server` | Server-side — hub, JWT auth, sandboxed file system |

```
┌─────────────┐         SignalR          ┌─────────────┐
│   Client    │ ◄──────────────────────► │   Server    │
│             │   WebSocket / SSE / LP   │             │
│  Commands   │                          │  Commands   │
│  Profiles   │   HTTP (file transfer)   │  JWT Auth   │
│  AutoComplete│ ◄─────────────────────► │  Sandbox FS │
└─────────────┘                          └─────────────┘
```

---

## How It Works

1. The client connects to a server via `server connect` or auto-connect
2. The server sends its `ServerCapabilities`, which includes its registered `CommandInfo` list
3. The client registers those commands in its `ICommandRegistry` as remote commands (`IsRemote = true`)
4. When a remote command is invoked, the client sends a `RunRequest` to the server
5. The server executes the command and streams console output back to the client
6. The client renders the output as if it were local

---

## Quick Start

### Server

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCommandLineHub(opt =>
{
    opt.FileTransferOptions.StorageRootPath = "./storage";
    opt.RegisterCommands(typeof(MyRemoteCommand));
});

var app = builder.Build();
app.ConfigureCommandLineHub();
app.Run();
```

### Client

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigureSignalRClient()
    .RegisterCommands(typeof(MyLocalCommand))
    .Build();

await app.RunInteractive();
```

```
app> server connect --uri http://localhost:5000
Connected.

app> my-remote-command --arg value
(executed on server, output streamed to client)
```

---

## In This Section

| Page | Description |
|------|-------------|
| [Shared Protocol](shared-protocol.md) | Envelopes, RPC infrastructure, serialization |
| **Server** | |
| [Setting Up the Server](server/index.md) | `AddCommandLineHub`, `ConfigureCommandLineHub` |
| [Authentication](server/authentication.md) | JWT tokens, API key stores |
| [File System & Sandboxing](server/sandboxing.md) | `SandboxedFileSystem`, validators |
| **Client** | |
| [Setting Up the Client](client/index.md) | `ConfigureSignalRClient`, options |
| [Connecting & Disconnecting](client/connecting.md) | `server connect`, `server disconnect` |
| [Auto-Connect](client/auto-connect.md) | Single-command mode with `IAutoConnectHandler` |
| [Server Profiles](client/profiles.md) | Save and manage server connections |
| [File Transfers](client/file-transfers.md) | Upload, download, globs, progress |
| **Protocol** | |
| [The IServerProxy Interface](server-proxy.md) | Core abstraction for remote operations |
| [RPC Communication Pattern](rpc.md) | Bidirectional request/response over SignalR |
| [Remote Console I/O](remote-console-io.md) | `SignalRAnsiConsole`, `SignalRAnsiInput` |

---

## See Also

- [Shared Protocol](shared-protocol.md)
- [Setting Up the Server](server/index.md)
- [Setting Up the Client](client/index.md)
- [Solution Architecture](../architecture.md)
