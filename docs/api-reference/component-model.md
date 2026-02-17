# Component Model

Runtime metadata objects that describe commands, arguments, and groups. These are constructed during command registration and used throughout the pipeline, autocomplete, help system, and serialization.

---

## CommandInfo

Describes a registered command.

```csharp
public class CommandInfo
{
    public GroupInfo Group { get; }
    public string Name { get; }
    public string FullyQualifiedName { get; }
    public string Description { get; }
    public Type Type { get; }
    public IReadOnlyCollection<ArgumentInfo> Arguments { get; }
    public bool IsRemote { get; }
    public bool IsExecuteAsync { get; }
    public Type InputType { get; }
    public Type ReturnType { get; }
}
```

| Property | Description |
|----------|-------------|
| `Group` | Parent group, or `null` for root-level commands |
| `Name` | The command's invocation name |
| `FullyQualifiedName` | Full path including groups (e.g., `"server profile add"`) |
| `Description` | From `[Description]` attribute |
| `Type` | The `CommandBase` subclass type |
| `Arguments` | All declared arguments |
| `IsRemote` | Whether this command was registered from a remote server |
| `IsExecuteAsync` | Whether the `Execute` method is async |
| `InputType` | The generic `T` from `CommandExecutionContext<T>`, or `null` |
| `ReturnType` | Return type of `Execute` (default: `typeof(void)`) |

---

## ArgumentInfo

Describes a command argument.

```csharp
public class ArgumentInfo
{
    public string Name { get; }
    public char Alias { get; }
    public string Description { get; }
    public SerializablePropertyInfo PropertyInfo { get; }
    public bool IsRequired { get; }
    public int Position { get; }
    public bool IsRest { get; }
    public bool IsFlag { get; }
    public bool IsPositional { get; }
    public bool IsCollection { get; }
}
```

| Property | Description |
|----------|-------------|
| `Name` | Argument name (used as `--name`) |
| `Alias` | Single-character alias, or `default(char)` if none |
| `Description` | From `[Description]` attribute |
| `PropertyInfo` | Serializable representation of the property |
| `IsRequired` | Whether the argument must be provided |
| `Position` | Positional index (`-1` for named arguments) |
| `IsRest` | Whether this captures remaining positional values |
| `IsFlag` | Whether this is a `[Flag]` boolean |
| `IsPositional` | Computed: `Position >= 0` |
| `IsCollection` | Computed: array, `List<T>`, or `IEnumerable<T>` (not `string`) |

---

## GroupInfo

Describes a command group.

```csharp
public class GroupInfo
{
    public string Name { get; }
    public string Description { get; }
    public GroupInfo Parent { get; }
    public Type MarkerType { get; }
    public IReadOnlyList<GroupInfo> ChildGroups { get; }
    public IReadOnlyList<CommandInfo> Commands { get; }
    public string FullPath { get; }
    public int Depth { get; }
}
```

| Property | Description |
|----------|-------------|
| `Name` | Group name (lowercased, `Group` suffix removed) |
| `Description` | From `[Description]` attribute |
| `Parent` | Parent group, or `null` for top-level |
| `MarkerType` | The marker class type decorated with `[Group]` |
| `ChildGroups` | Nested sub-groups |
| `Commands` | Commands directly in this group |
| `FullPath` | Space-separated group path (e.g., `"server profile"`) |
| `Depth` | Nesting depth (`0` for top-level) |

---

## See Also

- [The Processing Pipeline](../running/processing-pipeline.md)
- [Shared Protocol](../remote/shared-protocol.md)
- [Interfaces](interfaces.md)
- [Solution Architecture](../architecture.md)
