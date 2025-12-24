# Built-in Commands

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine includes built-in commands that are automatically registered with every application.

## List Commands (`lc`)

`BitPantry.CommandLine.Commands.ListCommandsCommand`

Lists all registered commands in the application.

### Syntax

```
lc [--filter|-f <expression>]
```

### Arguments

| Argument | Alias | Type | Description |
|----------|-------|------|-------------|
| `--filter` | `-f` | `string` | Dynamic LINQ expression to filter commands |

### Output

The command displays a table with:

| Column | Description |
|--------|-------------|
| Group | Command group path (or "None") |
| Name | Command name |
| Is Remote | Whether the command runs on a remote server |
| Description | Command description |
| Input Type | Pipeline input type (if any) |
| Return Type | Return type (if any) |

### Examples

#### List All Commands

```
> lc
```

Output:
```
┌───────────┬──────┬───────────┬────────────────────────────────┬────────────┬─────────────┐
│ Group     │ Name │ Is Remote │ Description                    │ Input Type │ Return Type │
├───────────┼──────┼───────────┼────────────────────────────────┼────────────┼─────────────┤
│ (None)    │ lc   │ ✘         │ Filters and lists registered   │ (None)     │ (None)      │
│           │      │           │ commands                       │            │             │
│ math      │ add  │ ✘         │ Adds two numbers               │ (None)     │ System.Int32│
│ files     │ copy │ ✘         │ Copies a file                  │ (None)     │ (None)      │
└───────────┴──────┴───────────┴────────────────────────────────┴────────────┴─────────────┘
```

#### Filter Commands by Group

```
> lc --filter "Group.FullPath == \"math\""
```

Or using the alias:

```
> lc -f "Group.FullPath == \"math\""
```

#### Filter Commands with Description

```
> lc -f "Description != null"
```

#### Filter Remote Commands Only

```
> lc -f "IsRemote == true"
```

### Filter Expression Syntax

The filter uses Dynamic LINQ syntax. Common operators:

| Operator | Example |
|----------|---------|
| `==` | `Group.FullPath == "math"` |
| `!=` | `Description != null` |
| `Contains()` | `Name.Contains("add")` |
| `StartsWith()` | `Group.FullPath.StartsWith("file")` |
| `&&` | `IsRemote == true && Group != null` |
| `\|\|` | `Group.FullPath == "math" \|\| Group.FullPath == "files"` |

### Available Filter Properties

| Property | Type | Description |
|----------|------|-------------|
| `Group` | `Group` | Command group (can be null for root commands) |
| `Group.FullPath` | `string` | Full space-separated group path |
| `Name` | `string` | Command name |
| `IsRemote` | `bool` | True if command runs remotely |
| `Description` | `string` | Command description (can be null) |
| `InputType` | `string` | Assembly-qualified input type name |
| `ReturnType` | `string` | Assembly-qualified return type name |

## See Also

- [Commands](Commands.md) - Creating custom commands
- [Command Syntax](CommandSyntax.md) - How to invoke commands
- [Remote Built-in Commands](../Remote/BuiltInCommands.md) - Server connection commands
- [End User Guide](../EndUserGuide.md) - User documentation
