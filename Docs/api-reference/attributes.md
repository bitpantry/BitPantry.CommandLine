# Core Attributes

All attributes are in the `BitPantry.CommandLine.API` namespace.

---

## `[Command]`

Marks a class as a command.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class CommandAttribute : Attribute
{
    public string Name { get; set; }
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(derived from class)_ | Command invocation name. If not set, derived by removing `Command` suffix and lowercasing. |

**Usage:** [Command Naming](../commands/naming.md)

---

## `[Argument]`

Declares a command argument on a public property.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgumentAttribute : Attribute
{
    public string Name { get; set; }
    public bool IsRequired { get; set; } = false;
    public int Position { get; set; } = -1;
    public bool IsRest { get; set; } = false;
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(property name)_ | Argument name (used as `--name`) |
| `IsRequired` | `bool` | `false` | Whether the argument must be provided |
| `Position` | `int` | `-1` | Positional index (`-1` = named argument, `≥0` = positional) |
| `IsRest` | `bool` | `false` | Capture remaining positional values into a collection |

**Usage:** [Arguments](../commands/arguments.md), [Positional Arguments](../commands/positional-arguments.md)

---

## `[Alias]`

Assigns a single-character alias to an argument.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class AliasAttribute : Attribute
{
    public char Alias { get; set; }
    public AliasAttribute(char alias)
}
```

| Parameter | Type | Description |
|-----------|------|-------------|
| `alias` | `char` | The alias character (must not be a space) |

**Usage:** [Arguments](../commands/arguments.md)

---

## `[Flag]`

Marks a `bool` argument as presence-only.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class FlagAttribute : Attribute { }
```

When present in input: `true`. When absent: `false`. Cannot accept a value.

**Usage:** [Flags](../commands/flags.md)

---

## `[Description]`

Documents a command or argument for the help system.

```csharp
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, AllowMultiple = false)]
public class DescriptionAttribute : Attribute
{
    public string Description { get; set; }
    public DescriptionAttribute(string description)
}
```

Applies to both command classes and argument properties.

---

## `[Group]`

Marks a class as a command group container.

```csharp
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GroupAttribute : Attribute
{
    public string Name { get; set; }
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Name` | `string` | _(derived from class)_ | Group name. Derived by removing `Group` suffix and lowercasing. |

**Usage:** [Command Groups](../commands/groups.md)

---

## `[InGroup<T>]`

Assigns a command to a group.

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class InGroupAttribute<T> : Attribute where T : class
{
    public Type GroupType => typeof(T);
}
```

| Type Parameter | Constraint | Description |
|----------------|-----------|-------------|
| `T` | `class` | The group marker class (decorated with `[Group]`) |

**Usage:** [Command Groups](../commands/groups.md)

---

## `[AutoComplete<THandler>]`

Binds a custom autocomplete handler to an argument.

```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
    where THandler : IAutoCompleteHandler
{
    public Type HandlerType => typeof(THandler);
}
```

| Type Parameter | Constraint | Description |
|----------------|-----------|-------------|
| `THandler` | `IAutoCompleteHandler` | The handler type (resolved from DI) |

**Usage:** [Custom Attribute Handlers](../autocomplete/attribute-handlers.md)

---

## See Also

- [Command Naming](../commands/naming.md)
- [Arguments](../commands/arguments.md)
- [Flags](../commands/flags.md)
- [Command Groups](../commands/groups.md)
- [Custom Attribute Handlers](../autocomplete/attribute-handlers.md)
