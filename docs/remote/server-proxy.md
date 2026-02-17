# The IServerProxy Interface

`IServerProxy` is the core abstraction for all remote operations. It encapsulates connection management, command execution, autocomplete, and RPC communication.

---

## Interface Definition

```csharp
public interface IServerProxy
{
    ServerProxyConnectionState ConnectionState { get; }
    ServerCapabilities Server { get; }

    Task Connect(string uri, CancellationToken token = default);
    Task<bool> EnsureConnectedAsync(CancellationToken token = default);
    Task Disconnect(CancellationToken token = default);

    Task<RunResponse> Run(RunRequest request, CancellationToken token = default);
    Task<AutoCompleteResponse> AutoComplete(AutoCompleteRequest request, CancellationToken token = default);
    Task<TResponse> SendRpcRequest<TResponse>(object request, CancellationToken token = default);
}
```

---

## Connection States

```csharp
public enum ServerProxyConnectionState : int
{
    Disconnected = 0,
    Connected = 1,
    Connecting = 2,
    Reconnecting = 3
}
```

---

## ServerCapabilities

When connected, the `Server` property provides information about the remote server:

```csharp
public class ServerCapabilities
{
    public Uri ConnectionUri { get; }
    public string ConnectionId { get; }
    public IReadOnlyList<CommandInfo> Commands { get; }
    public long MaxFileSizeBytes { get; }

    public static string FormatFileSize(long bytes);
}
```

---

## Implementations

| Type | Description |
|------|-------------|
| `NoopServerProxy` | Default — all operations are no-ops. Used when no remote package is installed. |
| `SignalRServerProxy` | Full implementation using SignalR hub connections. Registered by `ConfigureSignalRClient()`. |

---

## Usage in Commands

Inject `IServerProxy` to interact with the remote server programmatically:

```csharp
[Command(Name = "status")]
public class StatusCommand : CommandBase
{
    private readonly IServerProxy _proxy;

    public StatusCommand(IServerProxy proxy) => _proxy = proxy;

    public void Execute(CommandExecutionContext ctx)
    {
        if (_proxy.ConnectionState == ServerProxyConnectionState.Connected)
            Console.MarkupLine($"[green]Connected to {_proxy.Server.ConnectionUri}[/]");
        else
            Console.MarkupLine("[yellow]Not connected[/]");
    }
}
```

---

## See Also

- [Remote Execution](index.md)
- [RPC Communication Pattern](rpc.md)
- [Remote Console I/O](remote-console-io.md)
- [Connecting & Disconnecting](client/connecting.md)
- [Interfaces](../api-reference/interfaces.md)
