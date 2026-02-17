# Auto-Connect

Auto-connect enables automatic server connection when using `RunOnce()` mode, so single-command invocations can transparently execute remote commands.

---

## How It Works

When `RunOnce()` is called:

1. Auto-connect is enabled on the `IAutoConnectHandler`
2. Before command execution, `EnsureConnectedAsync()` is called
3. The handler resolves a server profile and connects
4. The command executes (locally or remotely)
5. After execution, auto-connect is disabled and the connection is dropped

---

## Profile Resolution Order

The `SignalRAutoConnectHandler` resolves the server profile in this order:

1. **`--profile` / `-P` global argument** — Highest priority

   ```shell
   myapp deploy --environment staging --profile production
   ```

2. **`BITPANTRY_PROFILE` environment variable** — Fallback for CI/scripts

   ```shell
   export BITPANTRY_PROFILE=production
   myapp deploy --environment staging
   ```

3. **Default profile** — Set via `server profile set-default`

If no profile is found at any level, the command executes without a server connection (local-only).

---

## The IAutoConnectHandler Interface

```csharp
public interface IAutoConnectHandler
{
    string RequestedProfileName { get; set; }
    bool AutoConnectEnabled { get; set; }
    Task<bool> EnsureConnectedAsync(IServerProxy proxy, CancellationToken token = default);
}
```

| Property | Description |
|----------|-------------|
| `RequestedProfileName` | Set by `GlobalArgumentParser` from `--profile` / `-P` |
| `AutoConnectEnabled` | Toggled by `RunOnce()` before/after execution |

---

## Error Handling

- If the specified profile is not found, a warning is logged and the command proceeds without connection
- If the server is unreachable, an error is reported and the command fails
- If no profile is available at all (no `--profile`, no env var, no default), the command proceeds locally

---

## Example: CI Pipeline

```shell
# Set the profile via environment variable
export BITPANTRY_PROFILE=ci-server

# Execute a remote command
myapp deploy --environment staging

# Or override with --profile
myapp deploy --environment staging -P production
```

---

## See Also

- [Connecting & Disconnecting](connecting.md)
- [Server Profiles](profiles.md)
- [Global Arguments](../../running/global-arguments.md)
- [Running Commands](../../running/index.md)
