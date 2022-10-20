# CommandBase
```BitPantry.CommandLine.API.CommandBase```

All command types must extend ```CommandBase``` which provides various application services to the command.

## Output

A command writes output through the ```Writer``` classes provided in the ```Command Base```. These output writers are defined by the [CommandLineApplication](CommandLineApplication.md)'s [IInterface](IInterface.md) implementation.

```cs
protected Writer Info;
protected Writer Warning;
protected Writer Error;
protected Writer Debug;
protected Writer Verbose;
```

## Input
A command reads input through functions provided in the ```CommandBase```.

```cs
/// <summary>
/// Reads a line of input from the interface
/// </summary>
/// <param name="maskInput">Whether or not the input should be masked (for user interfaces where input is being typed in)</param>
/// <returns>The line of input</returns>
public string ReadLine(bool maskInput = false) => _interface.ReadLine(maskInput);
```

```cs
/// <summary>
/// Reads a confirmation from the interface
/// </summary>
/// <param name="prompt">The confirmation prompt to write to the interface</param>
/// <returns>True if confirmed, otherwise false</returns>
protected bool Confirm(string prompt)
```