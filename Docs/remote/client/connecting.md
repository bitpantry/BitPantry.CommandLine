# Connecting & Disconnecting

Manage connections to remote servers in interactive mode using the built-in `server connect` and `server disconnect` commands.

---

## Connecting

### By URI

```
app> server connect --uri http://localhost:5000
```

With an API key:

```
app> server connect --uri http://localhost:5000 --api-key my-secret-key
```

With a custom token endpoint:

```
app> server connect --uri http://localhost:5000 --api-key my-key --token-endpoint /custom/token
```

### By Profile

```
app> server connect --profile production
```

See [Server Profiles](profiles.md) for managing saved connections.

---

## What Happens on Connect

1. The `ConnectionService` authenticates with the server (API key → access token)
2. A SignalR hub connection is established
3. The server sends its `ServerCapabilities` (commands, connection ID, max file size)
4. Remote commands are registered in the client's `ICommandRegistry` with `IsRemote = true`
5. The `ServerConnectionSegment` updates the prompt to show the connected URI
6. The `ProfilePromptSegment` shows the profile name (if connecting by profile)

```
myapp [connected: localhost:5000] [profile: production]> _
```

---

## Disconnecting

```
app> server disconnect
```

This drops the SignalR connection and removes all remote commands from the registry.

---

## ConnectionService

The `ConnectionService` consolidates the shared authentication and connection logic used by both interactive `server connect` and auto-connect paths:

- Handles the 401 → token acquisition → retry flow
- Manages the `AccessTokenManager` lifecycle
- Provides `EnsureConnectedAsync()` for reconnection attempts

---

## Connection States

The `IServerProxy` tracks connection state:

| State | Description |
|-------|-------------|
| `Disconnected` | No active connection |
| `Connecting` | Connection in progress |
| `Connected` | Active and ready |
| `Reconnecting` | Lost connection, attempting to restore |

---

## See Also

- [Setting Up the Client](index.md)
- [Auto-Connect](auto-connect.md)
- [Server Profiles](profiles.md)
- [Server Authentication](../server/authentication.md)
- [The IServerProxy Interface](../server-proxy.md)
