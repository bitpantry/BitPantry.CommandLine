# Running Commands

How the application processes input at runtime, the two execution modes, and the result model.

---

## Execution Modes

`CommandLineApplication` provides two execution modes:

### `RunInteractive()` — REPL Mode

Starts an interactive loop with autocomplete, syntax highlighting, and command history:

```csharp
await app.RunInteractive();
```

```
app> greet World
Hello, World!

app> deploy --environment staging --count 3
Deploying 3 instance(s) to staging

app> _
```

The loop continues until the `CancellationToken` is canceled. Each iteration updates `LastRunResult`.

### `RunOnce(string input)` — Single Command Mode

Executes a single command string and returns the result:

```csharp
var result = await app.RunOnce("greet World");
```

In this mode:
- Auto-connect is enabled (connects to a server if an `IAutoConnectHandler` is registered)
- The command is executed
- Auto-connect is disabled and the connection is dropped
- The `RunResult` is returned

---

## RunResult

Every execution produces a `RunResult`:

```csharp
public class RunResult
{
    public RunResultCode ResultCode { get; set; }
    public object Result { get; set; }
    public Exception RunError { get; set; }
}
```

| `RunResultCode` | Value | Description |
|-----------------|-------|-------------|
| `Success` | `0` | Command completed successfully |
| `HelpDisplayed` | `0` | Help was displayed (treated as success) |
| `ParsingError` | `1001` | Input could not be parsed |
| `ResolutionError` | `1002` | Command or arguments could not be resolved |
| `RunError` | `1003` | Exception during execution |
| `RunCanceled` | `1004` | Canceled via `CancellationToken` |
| `HelpValidationError` | `1005` | Help validation failed |

```csharp
var result = await app.RunOnce("deploy --environment staging");

if (result.ResultCode == RunResultCode.Success)
    Console.WriteLine($"Result: {result.Result}");
else
    Console.Error.WriteLine($"Error: {result.RunError?.Message}");
```

---

## In This Section

| Page | Description |
|------|-------------|
| [Global Arguments](global-arguments.md) | `--profile`, `--help`, reserved names |
| [The Processing Pipeline](processing-pipeline.md) | Parse → Resolve → Activate → Execute |
| [Command Piping](piping.md) | Chaining commands with `\|` |
| [Help System](help-system.md) | Built-in `--help` and custom formatters |

---

## See Also

- [Global Arguments](global-arguments.md)
- [The Processing Pipeline](processing-pipeline.md)
- [Building the Application](../building/index.md)
- [Defining Commands](../commands/index.md)
