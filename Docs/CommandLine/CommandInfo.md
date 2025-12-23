# CommandInfo

`BitPantry.CommandLine.Component.CommandInfo`

[← Back to Implementer Guide](../ImplementerGuide.md)

The `CommandInfo` object models a structured representation of a [command](Commands.md) type.


When a [command](Commands.md) type is registered with a [CommandRegistry](CommandRegistry.md), relevant type information is organized into a ```CommandInfo``` object. ```CommandInfo``` objects are used by the command line application to resolve, activate, and execute commands. If a ```CommandInfo``` object cannot be found in the application's [CommandRegistry](CommandRegistry.md), it cannot be resolved.

```cs
public class CommandInfo
{
	/// <summary>
	/// The command namespace, or null if no namespace is defined
	/// </summary>
	public string Namespace { get; }

	/// <summary>
	/// The command name by which the command can be resolved
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The description of the command defined by the Description attribute on the command class
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// The type of the command
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// The collection of arguments defined in the command type
	/// </summary>
	public IReadOnlyCollection<ArgumentInfo> Arguments { get; }

	/// <summary>
	/// If the command type contains an asynchronous Execute function, then true - false otherwise
	/// </summary>
	public bool IsExecuteAsync { get; }

	/// <summary>
	/// If the generic CommandExecutionContext<T> is used for the Execute function of the command type, this will be the type of the generic type parameter, or null if the non-generic CommandExecutionContext type is used
	/// </summary>
	public Type InputType { get; }

	/// <summary>
	/// The return type of the Execute function of the command type (including void, if the return type is void)
	/// </summary>
	public Type ReturnType { get; }
}
```

*For more information on building [command](Commands.md) types, see [Commands](Commands.md).*

---
See also,

- [ArgumentInfo](ArgumentInfo.md)
- [Commands](Commands.md)
- [Command Arguments](Commands.md#arguments)
- [CommandRegistry](CommandRegistry.md)
- [Description Attribute](Commands.md#description-attribute)