# Built-in Commands

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine includes built-in commands and features that are automatically available with every application.

## Built-in Commands by Package

### BitPantry.CommandLine (Core)

| Command | Description |
|---------|-------------|
| `version` | Displays application version information |
| `--help` | Shows help for commands and groups (built-in flag) |

### BitPantry.CommandLine.Remote.SignalR.Client

| Command | Group | Description |
|---------|-------|-------------|
| `connect` | `server` | Connects to a remote command line server |
| `disconnect` | `server` | Disconnects from the current remote server |
| `status` | `server` | Displays connection and profile status |
| `add` | `server profile` | Creates a new connection profile |
| `remove` | `server profile` | Removes an existing profile |
| `show` | `server profile` | Shows profile details |
| `list` | `server profile` | Lists all configured profiles |
| `set-default` | `server profile` | Sets the default profile |
| `set-key` | `server profile` | Updates the API key for a profile |

---

## Help System

The framework provides automatic help for all commands and groups via the `--help` (or `-h`) flag:

```
> myapp --help           # Show root help (all groups and commands)
> myapp math             # Show group help (subgroups and commands in math)
> myapp math add --help  # Show command help (usage and arguments)
```

Help is auto-generated from your command and group metadata (names, descriptions, arguments, aliases).

See [Help System](Help.md) for complete documentation.

---

## Version Command (`version`)

`BitPantry.CommandLine.Commands.VersionCommand`

Displays the application version information.

### Syntax

```
version [--full|-f]
```

### Arguments

| Argument | Alias | Type | Description |
|----------|-------|------|-------------|
| `--full` | `-f` | `Switch` | Shows detailed version information including runtime and OS |

### Output

#### Basic Output

Displays the application version from the entry assembly.

```
> version
1.0.0
```

#### Full Output

With the `--full` flag, displays extended version information:

```
> version --full
Version:     1.0.0
Runtime:     .NET 8.0.0
OS:          Microsoft Windows 10.0.22631
```

### Examples

#### Show Version

```
> version
1.2.3
```

#### Show Full Version Info

```
> version --full
Version:     1.2.3
Runtime:     .NET 8.0.0
OS:          Linux 5.15.0-generic #1 SMP
```

---

## See Also

- [Help System](Help.md) - Automatic help for commands and groups
- [Commands](Commands.md) - Creating custom commands
- [Command Syntax](CommandSyntax.md) - How to invoke commands
- [Remote Built-in Commands](../Remote/BuiltInCommands.md) - Server connection commands
- [Profile Management](../Remote/ProfileManagement.md) - Connection profile commands
- [End User Guide](../EndUserGuide.md) - User documentation
