# Global Arguments

Global arguments are extracted from raw input before command routing. They apply to the application as a whole, not to specific commands.

---

## Built-in Global Arguments

The `GlobalArgumentParser` strips the following arguments from input before it reaches the command pipeline:

| Argument | Alias | Description |
|----------|-------|-------------|
| `--profile` | `-P` | Select a server profile for the current execution |
| `--help` | `-h` | Display help for the matched command or group |

```
app> deploy --environment staging --profile production
```

In this example, `--profile production` is extracted by the `GlobalArgumentParser`. The command `deploy` receives only `--environment staging`.

---

## The GlobalArguments Class

```csharp
public class GlobalArguments
{
    public string ProfileName { get; set; }
    public bool HelpRequested { get; set; }

    public static IReadOnlyList<string> ReservedNames => ["profile", "help"];
    public static IReadOnlyList<char> ReservedAliases => ['p', 'P', 'h', 'H'];
}
```

---

## Reserved Names

Commands **cannot** use the reserved argument names or aliases:

- Argument names: `profile`, `help`
- Aliases: `p`, `P`, `h`, `H`

Attempting to define a command argument with a reserved name will produce a description-time error.

---

## Processing Order

1. Raw input string arrives
2. `GlobalArgumentParser` extracts `--profile` / `-P` and `--help` / `-h`
3. Cleaned input proceeds to the standard [processing pipeline](processing-pipeline.md)
4. If `--help` was present, help is displayed instead of executing the command

---

## Profile Selection

The `--profile` / `-P` global argument is used by the [auto-connect handler](../remote/client/auto-connect.md) to select a server profile for `RunOnce()` mode:

```shell
# Use a specific profile
myapp deploy --environment staging --profile production

# Short form
myapp deploy --environment staging -P production
```

When no `--profile` is specified, the auto-connect handler falls back to the `BITPANTRY_PROFILE` environment variable, then the default profile.

---

## See Also

- [Running Commands](index.md)
- [The Processing Pipeline](processing-pipeline.md)
- [Arguments](../commands/arguments.md)
- [Auto-Connect](../remote/client/auto-connect.md)
