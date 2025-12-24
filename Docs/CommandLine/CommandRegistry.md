# CommandRegistry

`BitPantry.CommandLine.CommandRegistry`

[← Back to Implementer Guide](../ImplementerGuide.md)

The `CommandRegistry` maintains a collection of all [Command](Commands.md) types, in the form of [CommandInfo](CommandInfo.md) objects, registered and available to the command line application. If a [command](Commands.md) type is not registered with the `CommandRegistry` it cannot be resolved or executed.

Registered [CommandInfo](CommandInfo.md) objects, representing all [command](Commands.md) types registered with the `CommandRegistry` is available using the *Commands* property.

```cs
public class CommandRegistry
{

    ...

    /// <summary>
    /// The collection of CommandInfos registered with this CommandRegistry
    /// </summary>
    public IReadOnlyCollection<CommandInfo> Commands { get; }

    ...

}
```

[Command](Commands.md) types can be registered with the ```CommandRegistry``` using two *RegisterCommand* functions.

```cs
public class CommandRegistry
{

    ...

    /// <summary>
    /// Registers a command with this CommandRegistry
    /// </summary>
    /// <typeparam name="T">The type of the command to register</typeparam>
	public void RegisterCommand<T>() where T : CommandBase

    /// <summary>
    /// Registers a command with this CommandRegistry
    /// </summary>
    /// <param name="type">The tyep of command to register</param>
    public void RegisterCommand(Type type)

	...

}
```

A [CommandInfo](CommandInfo.md) object can be found for a specific [command](Commands.md) type using the command group path and command name (see [Commands](Commands.md) for more information on command groups and names).

```cs
public class CommandRegistry
{

    ...

    /// <summary>
    /// Returns the CommandInfo specified by the group path and name
    /// </summary>
    /// <param name="groupPath">The command group path (e.g., "group1 group2")</param>
    /// <param name="name">The name of the command</param>
    /// <returns>The CommandInfo specified by the group path and name, or null if the CommandInfo could not be resolved</returns>
    public CommandInfo Find(string groupPath, string name)

    /// <summary>
    /// Returns the CommandInfo specified by the fully qualified command path
    /// </summary>
    /// <param name="fullyQualifiedCommandPath">The fully qualified command path, including groups (e.g., "group1 group2 commandName")</param>
    /// <returns>The CommandInfo specified by the fullyQualifiedCommandPath, or null if the CommandInfo could not be resolved</returns>
    public CommandInfo Find(string fullyQualifiedCommandPath)

	...

}
```

---
See also

- [CommandInfo](CommandInfo.md)
- [Commands](Commands.md)