# The Processing Pipeline

Every command execution passes through a multi-stage pipeline that transforms raw input into a completed result.

---

## Pipeline Stages

```
Raw Input → Global Argument Extraction → Parsing → Resolution → Activation → Execution → RunResult
```

### 1. Global Argument Extraction

The `GlobalArgumentParser` scans the raw input string and strips [global arguments](global-arguments.md) (`--profile`, `--help`). The cleaned input proceeds to parsing.

### 2. Parsing

The parser tokenizes the cleaned input into a `ParsedInput` — a structured representation of the command name, argument names, argument values, and pipe operators.

Key types:
- `ParsedInput` — The complete parsed result
- `ParsedCommand` — A single command segment (multiple in piped input)

### 3. Resolution

The resolver matches the parsed tokens against the `ICommandRegistry`:

- Resolves the command name (including group path) to a `CommandInfo`
- Maps argument tokens to `ArgumentInfo` definitions
- Validates required arguments, positional ordering, and type compatibility

Key types:
- `ResolvedInput` — The fully resolved result
- `ResolvedCommand` — Resolved command with matched arguments
- `ArgumentValues` — Argument name-to-value mappings

### 4. Activation

The activator creates command instances from the DI container and injects argument values into the decorated properties:

- Constructs the `CommandBase` subclass via `IServiceProvider`
- Parses string values into target property types using `BitPantry.Parsing.Strings`
- Sets the `Console` property on the command instance

Key types:
- `CommandActivator` — Performs activation
- `ActivationResult` — Contains the initialized command

### 5. Execution

The executor calls the command's `Execute` method, captures the return value (if any), handles exceptions, and produces a `RunResult`:

- Wraps execution in a try/catch
- Checks for `IUserFacingException` to determine safe error messages
- Sets `RunResultCode` based on outcome

Key type:
- `CommandLineApplicationCore` — Orchestrates the full pipeline

---

## Error Handling Through the Pipeline

| Stage | Error Type | `RunResultCode` |
|-------|-----------|-----------------|
| Parsing | Invalid syntax, unknown tokens | `ParsingError` (1001) |
| Resolution | Unknown command, missing required args | `ResolutionError` (1002) |
| Execution | Exception from `Execute()` / `Fail()` | `RunError` (1003) |
| Execution | `CancellationToken` canceled | `RunCanceled` (1004) |

---

## Pipeline Flow for Piped Commands

When input contains `|`, multiple commands are parsed and executed in sequence. The output of each command becomes the `Input` property of the next command's `CommandExecutionContext<T>`:

```
cmd1 --arg val | cmd2 --arg2 val2
```

Each piped segment goes through Resolution → Activation → Execution independently, with the pipeline data flowing between them.

---

## See Also

- [Running Commands](index.md)
- [Error Handling](../commands/error-handling.md)
- [Solution Architecture](../architecture.md)
- [Component Model](../api-reference/component-model.md)
