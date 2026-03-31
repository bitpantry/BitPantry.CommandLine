# Auto-Connect

Auto-connect enables automatic server connection when using `RunOnce()` mode, so single-command invocations can transparently execute remote commands.

---

## How It Works

When `RunOnce()` is called:

1. Auto-connect is enabled on the `IAutoConnectHandler`
2. **Before command parsing/resolution**, `EnsureConnectedAsync()` is called
3. The handler resolves a server profile and connects
4. On successful connection, remote commands are registered in the command registry
5. The command is parsed and resolved (remote commands are now discoverable)
6. The command executes (locally or remotely)
7. After execution, auto-connect is disabled and the connection is dropped

> **Note:** The early connection step (before parsing) is critical for remote command discovery. Without it, commands like `myapp admin keys` would fail with "command not found" because the `admin` group isn't registered until the server connection is established.

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
    string LastAutoConnectFailure { get; }
    Task<bool> EnsureConnectedAsync(IServerProxy proxy, CancellationToken token = default);
}
```

| Property | Description |
|----------|-------------|
| `RequestedProfileName` | Set by `GlobalArgumentParser` from `--profile` / `-P` |
| `AutoConnectEnabled` | Toggled by `RunOnce()` before/after execution |
| `LastAutoConnectFailure` | Contains the failure message if auto-connect fails |

---

## Error Handling

- If the specified profile is not found, an `InvalidOperationException` is thrown
- If the server is unreachable, a warning is displayed and local commands proceed normally
- If no profile is available at all (no `--profile`, no env var, no default), the command proceeds locally
- The `LastAutoConnectFailure` property contains the error message when connection fails

---

## REPL Mode vs RunOnce Mode

| Mode | Auto-Connect | Behavior |
|------|--------------|----------|
| `RunOnce()` | Enabled | Early connect before parsing; remote commands discoverable |
| `RunInteractive()` | Disabled | No auto-connect; use `server connect` explicitly |

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
