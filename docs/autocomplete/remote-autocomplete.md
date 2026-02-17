# Remote Autocomplete

When a command is registered from a connected remote server, autocomplete requests are transparently forwarded to the server.

---

## How It Works

1. The user types input that matches a remote command
2. The autocomplete system detects that the matched `CommandInfo` has `IsRemote = true`
3. An `AutoCompleteRequest` is sent to the server via `IServerProxy.AutoComplete()`
4. The server resolves and executes the appropriate handler using its own handler registry
5. Results are serialized and returned to the client
6. Suggestions are displayed normally (ghost text or menu)

---

## AutoCompleteContext Serialization

The `AutoCompleteContext` is serialized for the round-trip:

```csharp
public class AutoCompleteContext
{
    public required string QueryString { get; init; }
    public required string FullInput { get; init; }
    public required int CursorPosition { get; init; }
    public required ArgumentInfo ArgumentInfo { get; init; }
    public required IReadOnlyDictionary<ArgumentInfo, string> ProvidedValues { get; init; }
    public required CommandInfo CommandInfo { get; init; }
}
```

The `ArgumentInfo` and `CommandInfo` objects are serialized using the custom JSON converters in the [shared protocol](../remote/shared-protocol.md) library.

---

## Server-Side Handler Resolution

The server uses its own handler resolution chain — attribute handlers registered on server command properties, type handlers registered via `ConfigureAutoComplete()` in the server options, and the built-in enum/boolean handlers.

Server-side handlers have access to server-side DI services, enabling suggestions based on server-side data:

```csharp
// On the server
public class RemoteEnvironmentHandler : IAutoCompleteHandler
{
    private readonly IDeploymentService _deployments;

    public RemoteEnvironmentHandler(IDeploymentService deployments)
        => _deployments = deployments;

    public async Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var envs = await _deployments.GetEnvironmentsAsync();
        return envs
            .Where(e => e.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList();
    }
}
```

---

## Transparency

From the user's perspective, remote autocomplete is indistinguishable from local autocomplete. The same ghost text, menu rendering, and filtering behavior applies.

---

## See Also

- [Autocomplete](index.md)
- [Remote Execution](../remote/index.md)
- [The IServerProxy Interface](../remote/server-proxy.md)
- [RPC Communication Pattern](../remote/rpc.md)
