# Help System

Built-in help is available at every level — root, group, and command — via the `--help` or `-h` global argument.

---

## Using Help

### Root Help

```
app> --help
```

Shows all top-level commands and groups.

### Group Help

```
app> server --help
```

Shows commands and sub-groups within the `server` group.

### Command Help

```
app> deploy --help
```

```
DEPLOY
  Deploys the application to a target environment

Usage: deploy [arguments]

Arguments:
  --environment, -e    Target environment                  [required]
  --count              Number of instances                 [default: 1]
  --verbose, -v        Enable verbose output               [flag]
```

---

## The `IHelpFormatter` Interface

Help output is rendered by an `IHelpFormatter`. The framework provides a default `HelpFormatter` implementation:

```csharp
public interface IHelpFormatter
{
    void DisplayGroupHelp(IAnsiConsole console, GroupInfo group, ICommandRegistry registry);
    void DisplayCommandHelp(IAnsiConsole console, CommandInfo command);
    void DisplayRootHelp(IAnsiConsole console, ICommandRegistry registry);
}
```

---

## Custom Help Formatter

Replace the default formatter with a custom implementation:

```csharp
var app = new CommandLineApplicationBuilder()
    .UseHelpFormatter<MyHelpFormatter>()
    .Build();
```

Or provide an instance:

```csharp
builder.UseHelpFormatter(new MyHelpFormatter());
```

The custom formatter receives the full `CommandInfo`, `GroupInfo`, or `ICommandRegistry` along with the `IAnsiConsole` for rendering.

---

## Help Processing

Help is handled as a [global argument](global-arguments.md):

1. `GlobalArgumentParser` detects `--help` / `-h` and sets `GlobalArguments.HelpRequested = true`
2. The pipeline still resolves the command or group (to determine what help to show)
3. Instead of executing, the `IHelpFormatter` renders help
4. `RunResultCode.HelpDisplayed` (value `0`) is returned

---

## See Also

- [Running Commands](index.md)
- [Command Naming](../commands/naming.md)
- [Arguments](../commands/arguments.md)
- [Command Groups](../commands/groups.md)
