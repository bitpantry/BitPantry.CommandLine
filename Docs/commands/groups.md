# Command Groups

Groups organize commands into hierarchical namespaces, providing logical structure for applications with many commands.

---

## Defining a Group

Create a marker class decorated with `[Group]`:

```csharp
[Group(Name = "server")]
[Description("Server management commands")]
public class ServerGroup { }
```

If `Name` is not specified, the name is derived from the class name by removing the `Group` suffix and converting to lowercase:

```csharp
[Group]  // Name becomes "server"
public class ServerGroup { }
```

---

## Assigning Commands to Groups

Use `[InGroup<T>]` on the command class:

```csharp
[InGroup<ServerGroup>]
[Command(Name = "connect")]
[Description("Connect to a remote server")]
public class ConnectCommand : CommandBase
{
    [Argument(Name = "uri", IsRequired = true)]
    public string Uri { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Connecting to [bold]{Uri}[/]...");
    }
}

[InGroup<ServerGroup>]
[Command(Name = "disconnect")]
[Description("Disconnect from the current server")]
public class DisconnectCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine("Disconnected.");
    }
}
```

```
app> server connect --uri http://localhost:5000
Connecting to http://localhost:5000...

app> server disconnect
Disconnected.
```

---

## Nested Groups

Create hierarchical groups by nesting marker classes:

```csharp
[Group(Name = "server")]
public class ServerGroup
{
    [Group(Name = "profile")]
    [Description("Manage server profiles")]
    public class ProfileGroup { }
}

[InGroup<ServerGroup.ProfileGroup>]
[Command(Name = "add")]
public class ProfileAddCommand : CommandBase { ... }

[InGroup<ServerGroup.ProfileGroup>]
[Command(Name = "list")]
public class ProfileListCommand : CommandBase { ... }
```

```
app> server profile add --name production --uri https://prod.example.com
app> server profile list
```

The fully qualified path is constructed by joining group names with spaces.

---

## Group Help

Running `--help` at a group level shows all commands and sub-groups within it:

```
app> server --help
SERVER
  Server management commands

Commands:
  connect       Connect to a remote server
  disconnect    Disconnect from the current server

Groups:
  profile       Manage server profiles
```

---

## GroupInfo at Runtime

Groups are represented at runtime by `GroupInfo`:

| Property | Type | Description |
|----------|------|-------------|
| `Name` | `string` | Group name (lowercased, `Group` suffix removed) |
| `Description` | `string` | From `[Description]` attribute |
| `Parent` | `GroupInfo` | Parent group, or `null` for top-level |
| `ChildGroups` | `IReadOnlyList<GroupInfo>` | Nested sub-groups |
| `Commands` | `IReadOnlyList<CommandInfo>` | Commands in this group |
| `FullPath` | `string` | Space-separated path (e.g., `"server profile"`) |
| `Depth` | `int` | Nesting depth (`0` for top-level) |

---

## See Also

- [Defining Commands](index.md)
- [Command Naming](naming.md)
- [Registering Commands](../building/registering-commands.md)
- [Help System](../running/help-system.md)
- [Core Attributes](../api-reference/attributes.md)
