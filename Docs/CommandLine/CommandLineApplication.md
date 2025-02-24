# CommandLineApplication
*```BitPantry.CommandLine.CommandLineApplication```*

The ```CommandLineApplication``` is created using the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) and constitutes the core of the command line application - parsing and executing a string representation of a command expressions and returning a [RunResult](RunResult.md).

- [Executing Commands](#executing-commands)
- [Canceling Command Execution](#canceling-current-operation)
- [The REPL](#the-repl)
- [Executing Scripts](#executing-scripts)
- [Canceling Execution](#canceling-execution)

## Executing Commands
Commands can be passed into the ```CommandLineApplication``` as string expressions.

The ```Run(string inputStr)``` function accepts and executes a single [string command expression](CommandSyntax.md) returning a [RunResult](RunResult.md).

```cs
/// <summary>
/// Runs the command line application using the args array to construct the command expression
/// </summary>
/// <param name="inputStr">The command expression</param>
/// <returns>The run result</returns>
/// <exception cref="InvalidOperationException">Thrown if a command is already running</exception>
public async Task<RunResult> Run(string inputStr, CancellationToken token = default)
```

While a command is running, the ```IsRunning``` property of the ```CommandLineApplication``` will return *true* - otherwise, the property will return *false*.

# The REPL

To start the use ```CommandLineApplication.Run()```.

```
public async Task Run(CancellationToken token = default)
```

# Executing Scripts

When using the ```Run(string input, CancellationToken token = default)``` function, if the input is detected to be a valid file path, the command line application will treat the file as a script.

A script is any text file containing lines of command line input.

```t
helloworld
add --num1 1 --num2 3 | add --num2 4
cmd3
```

The command line application will parse the file, executing each line from top to bottom. If any command returns an error, script execution will be terminated.

# Canceling Execution

To abort command execution and to stop the REPL, cancel the passed CancellationToken.

---
See also,

- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md)
- [RunResult](RunResult.md)
- [REPL](REPL.md)
- [Command Syntax](CommandSyntax.md)
