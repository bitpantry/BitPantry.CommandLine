# CommandExecutionContext
```BitPantry.CommandLine.API.CommandExecutionContext```

The ```CommandExecutionContext``` provides access to the cancellation token for the process and the [CommandRegistry](CommandRegistry.md).

```cs
/// <summary>
/// Contains information representing the context of the currently executing command process
/// </summary>
public class CommandExecutionContext
{
	/// <summary>
	/// The cancellation token scoped to the currently executing command process
	/// </summary>
	public CancellationToken CancellationToken { get; }

	/// <summary>
	/// The command registry for the command line application
	/// </summary>
	public CommandRegistry CommandRegistry { get; }
	
}
```

By using the generic version of the ```CommandExecutionContext<>``` the command may receive, as input from the [pipeline](CommandPipeline.md), an input parameter of type T. If no input is available, the *Input* property will be initialized to default(T).

```cs
/// <summary>
/// Contains information representing the context of the currently executing command process
/// </summary>
/// <typeparam name="T">The data type of the input data that can be passed into this command</typeparam>
public class CommandExecutionContext<T> : CommandExecutionContext
{

	/// <summary>
	/// The input passed into this command. If no input is available from the pipeline, default(T) is used
	/// </summary>
	public T Input { get; }

}
```

---
See also,

- [CommandRegistry](CommandRegistry.md)
- [Command Pipeline](CommandPipeline.md)