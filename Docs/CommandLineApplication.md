# CommandLineApplication
*```BitPantry.CommandLine.CommandLineApplication```*

The ```CommandLineApplication``` is created using the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) and constitutes the core of the command line application - parsing and executing a string representation of a command expressions and returning a [RunResult](RunResult.md).

- [Executing Commands](#executing-commands)
- [Canceling Command Execution](#canceling-current-operation)

## Executing Commands
Commands are passed into the ```CommandLineApplication``` as string expressions.

The ```Run(string[] args)``` function is a helper function that accepts the string[] array from the standard console application and joins it into a single [string command expression](CommandSyntax.md) before executing.

```cs
/// <summary>
/// Runs the command line application using the args array to construct the command expression
/// </summary>
/// <param name="args">An array of strings to join together to create the command expression</param>
/// <returns>The run result</returns>
/// <example>
/// 
/// If -
///     args[0] = "myCmd"
///     args[1] = "-p"
///     args[2] = "10"
///     
/// Then -
/// 
///     The arg array will be joined into a string representing a single command expression and executed - "myCmd -p 10"
/// 
/// </example>
public async Task<RunResult> Run(string[] args)
```
The ```Run(string inputStr)``` function accepts and executes a single [string command expression](CommandSyntax.md).

```cs
/// <summary>
/// Runs the command line application using the args array to construct the command expression
/// </summary>
/// <param name="inputStr">The command expression</param>
/// <returns>The run result</returns>
/// <exception cref="InvalidOperationException">Thrown if a command is already running</exception>
public async Task<RunResult> Run(string inputStr)
```

While a command is running, the ```IsRunning``` property of the ```CommandLineApplication``` will return *true* - otherwise, the property will return *false*.

```cs
public bool IsRunning { get; }
```


## Canceling Current Operation
If the ```IsRunning``` property is *true*, calling ```CancelCurrentOperation()``` will attempt to cancel the current command execution.

```cs
/// <summary>
/// Cancels the current command execution
/// </summary>
/// <exception cref="InvalidOperationException">If IsRunning is false, an InvalidOperationException is thrown</exception>
public void CancelCurrentOperation()
```

---
See also,

- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md)
- [RunResult](RunResult.md)
- [Command Syntax](CommandSyntax.md)
