# ArgumentInfo
```BitPantry.CommandLine.Component.ArgumentInfo```

The ```ArgumentInfo``` object models a structured representation of a [command argument](Commands.md#arguments).


When a [command](Commands.md) type is registered with a [CommandRegistry](CommandRegistry.md), relevant type information is organized into a [CommandInfo](CommandInfo.md) object. [CommandInfo](CommandInfo.md) objects are used by the command line application to resolve, activate, and execute commands. Relevant property information for [command arguments](Commands.md#arguments) is stored in ```ArgumentInfo``` objects, which are availalable as a collection on the [CommandInfo](CommandInfo.md) object.

```cs
public class ArgumentInfo
{
	/// <summary>
	/// The name of the argument
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The alias of the argument, or default(char) if no alias is defined
	/// </summary>
	public char Alias { get; }

	/// <summary>
	/// The data type of the argument property
	/// </summary>
	public Type DataType { get; }

	/// <summary>
	/// The argument description defined by the Description attribute on the property
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// The PropertyInfo object that represents the argument property
	/// </summary>
	public PropertyInfo PropertyInfo { get; }
}
```

*For more information on building [command](Commands.md) types, including defining arguments, see [Command Arguments](Commands.md#arguments).*

---
See also,

- [Commands](Commands.md)
- [CommandInfo](CommandInfo.md)
- [Command Arguments](Commands.md#arguments)
- [Description Attribute](Commands.md#description-attribute)