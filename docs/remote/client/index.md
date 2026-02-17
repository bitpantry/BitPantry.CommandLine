# Setting Up the Client

Configure the client to connect to remote servers and execute commands.

---

## Installation

```shell
dotnet add package BitPantry.CommandLine.Remote.SignalR.Client
```

---

## Configuration

Add `ConfigureSignalRClient()` to the builder:

```csharp
var app = new CommandLineApplicationBuilder()
    .ConfigureSignalRClient()
    .RegisterCommands(typeof(MyLocalCommand))
    .Build();

await app.RunInteractive();
```

This single call registers all client infrastructure:

| Component | Description |
|-----------|-------------|
| `SignalRServerProxy` | `IServerProxy` implementation |
| `SignalRAutoConnectHandler` | `IAutoConnectHandler` for `RunOnce()` mode |
| `ConnectionService` | Shared auth and connection logic |
| `AccessTokenManager` | Token lifecycle management |
| `FileTransferService` | Upload/download via HTTP |
| `ProfileManager` | Profile CRUD and storage |
| `CredentialStore` | Encrypted credential storage (DPAPI) |
| Built-in commands | `server connect/disconnect`, `server upload/download`, `server profile *` |
| Prompt segments | `ServerConnectionSegment`, `ProfilePromptSegment` |

---

## SignalRClientOptions

Customize client behavior:

```csharp
builder.ConfigureSignalRClient(opts =>
{
    opts.TokenRefreshMonitorInterval = TimeSpan.FromMinutes(2);
    opts.TokenRefreshThreshold = TimeSpan.FromMinutes(10);
    opts.ProfilesStoragePath = @"C:\MyApp\profiles";
});
```

| Property | Default | Description |
|----------|---------|-------------|
| `HttpClientFactory` | `DefaultHttpClientFactory` | Factory for HTTP clients (file transfers, auth) |
| `HttpMessageHandlerFactory` | `DefaultHttpMessageHandlerFactory` | Factory for HTTP message handlers |
| `TokenRefreshMonitorInterval` | 1 minute | How often to check token expiry |
| `TokenRefreshThreshold` | 5 minutes | Refresh when token expires within this window |
| `Transports` | `null` (auto) | SignalR transport type override |
| `ProfilesStoragePath` | `{LocalAppData}/BitPantry/CommandLine/profiles` | Profile storage directory |

---

## In This Section

| Page | Description |
|------|-------------|
| [Connecting & Disconnecting](connecting.md) | Interactive connection management |
| [Auto-Connect](auto-connect.md) | Automatic connection for `RunOnce()` mode |
| [Server Profiles](profiles.md) | Save and manage server connections |
| [File Transfers](file-transfers.md) | Upload and download files |

---

## See Also

- [Remote Execution](../index.md)
- [Connecting & Disconnecting](connecting.md)
- [Server Profiles](profiles.md)
- [Setting Up the Server](../server/index.md)
