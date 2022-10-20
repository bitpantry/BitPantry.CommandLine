# IInterface 
```BitPantry.CommandLine.Interface.IInterface```

The IInterface interface defines the contract for the applications interface (typically a console). Not all functions of the interface will be applicable for all implementations.

```cs
/// <summary>
/// Returns an IWriterCollection belonging to the interface
/// </summary>
IWriterCollection WriterCollection { get; }  

/// <summary>
/// Raising this event attempts to cancel the currently focused process execution
/// </summary>
event ConsoleEvents.CancelExecutionEventHandler CancelExecutionEvent;

/// <summary>
/// Reads a line of input from the interface
/// </summary>
/// <param name="maskInput">Whether or not the interface should mask the input as entered</param>
/// <returns>The line read from the interface</returns>
string ReadLine(bool maskInput = false);

/// <summary>
/// Reads a character from the interface
/// </summary>
/// <returns>The character read from the interface</returns>
char ReadKey();

/// <summary>
/// Clears the interface
/// </summary>
void Clear();
```

## Implementations
There are two implementations of the IInterface included with the core solution.

```BitPantry.CommandLine.ConsoleInterface``` implements the interface for the standard system console. By default, the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) uses the ```ConsoleInterface```.

```BitPantry.CommandLine.Tests``` implements the interface in a form condusive for unit testing. Most functions are implemented to return defaults (e.g., ```Clear()``` does nothing and ```ReadKey``` returns a hard coded character).
