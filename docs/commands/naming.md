# Command Naming

How command names are determined, naming conventions, and the `[Command]` attribute.

---

## The `[Command]` Attribute

Every command class must be decorated with `[Command]`. The attribute has one optional property:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
}
```

---

## Explicit Naming

Set the `Name` property to define the command's invocation name:

```csharp
[Command(Name = "deploy-app")]
public class DeployApplicationCommand : CommandBase { ... }
```

```
app> deploy-app --environment staging
```

---

## Derived Naming

When `Name` is not set, the name is derived from the class name by removing the `Command` suffix and converting to lowercase:

```csharp
[Command]
public class DeployCommand : CommandBase { ... }
// Invoked as: deploy
```

---

## Naming Rules

Command names must match the pattern `[a-zA-Z_][a-zA-Z0-9_]*(-[a-zA-Z0-9_]+)*`:

- Start with a letter or underscore
- Contain letters, digits, underscores
- Hyphens are allowed between segments (kebab-case)

**Valid names:** `deploy`, `deploy-app`, `my_command`, `task2`
**Invalid names:** `2deploy`, `-deploy`, `deploy--app`

---

## Case Sensitivity

By default, command resolution is **case-insensitive**. This can be changed via the `CommandRegistryBuilder`:

```csharp
public class CommandRegistryBuilder
{
    public bool CaseSensitive { get; set; } = false;
}
```

When case-insensitive (default), `Deploy`, `deploy`, and `DEPLOY` all resolve to the same command.

---

## Fully Qualified Names

When a command belongs to a [group](groups.md), its fully qualified name includes the group path:

```csharp
[Group(Name = "server")]
public class ServerGroup { }

[InGroup<ServerGroup>]
[Command(Name = "connect")]
public class ConnectCommand : CommandBase { ... }
// Fully qualified name: "server connect"
```

```
app> server connect --uri http://localhost:5000
```

---

## See Also

- [Defining Commands](index.md)
- [Command Groups](groups.md)
- [Help System](../running/help-system.md)
- [Core Attributes](../api-reference/attributes.md)
