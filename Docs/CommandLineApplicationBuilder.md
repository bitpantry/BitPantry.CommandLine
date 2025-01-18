# CommandLineApplicationBuilder
```BitPantry.CommandLine.CommandLineApplicationBuilder```

The ```CommandLineApplicationBuilder``` class is used to configure and build a [CommandLineApplication](CommandLineApplication.md).

- [Registering Commands](#registering-commands)
- [Configuring Dependency Injection](#configuring-dependency-injection)
- [Configuring the Interface](#configuring-the-interface)
- [Building the CommandLineApplication](#building-the-command-line-application)



## Registering Commands

Registering commands, using the functions below, will make the commands available to the [CommandLineApplication](CommandLineApplication.md). For any command type that is not properly defined, the ```CommandLineApplicationBuilder``` will throw an exception when building the [CommandLineApplication](CommandLineApplication.md). For more information on properly defining command types, see [Commands](Commands.md).

```cs
/// <summary>
/// Registers the command by the given type parameter, T
/// </summary>
/// <typeparam name="T">The type of the command to register</typeparam>
/// <returns>The CommandLineApplicationBuilder</returns>
Public CommandLineApplicationBuilder RegisterCommand<T>() where T : CommandBase
```
```cs
/// <summary>
/// Registers the command by the given type
/// </summary>
/// <param name="type">The type of the command to register</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder RegisterCommand(Type type)
```
```cs
/// <summary>
/// Registers all types that extend CommandBase for all assemblies represented by the types provided
/// </summary>
/// <param name="assemblyTargetTypes">The types that represent assemblies to be searched for commands to register</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder RegisterCommands(params Type[] assemblyTargetTypes)
```

If an attempt to run a command is made, but the command has not been registered, the [RunResult](RunResult.md) will return with a [RunResultCode](RunResultCode.md) of value ```ResolutionError```.

## Configuring the IAnsiConsole

[Spectr.Console](https://spectreconsole.net/) provides numerous services and abstractions to the System.Console that "make it easier to create beautiful console applications." If you haven't heard of Spectr.Console before, it's an awesome project that made it easy to take this project to the next level.

The Spectr.Console [*IAnsiConsole*](IAnsiConsole.md) interface is used to define a custom implementation of this abstraction. If one is not configured the default implementation is used - ```AnsiApplication.Create```.

```cs
/// <summary>
/// Configures the application to use the given IAnsiConsole implementation
/// </summary>
/// <param name="console">The implementation to use</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder UsingAnsiConsole(IAnsiConsole console)
```

## Building the Command Line Application

Calling the ```Build``` function on the ```CommandLineApplicationBuilder``` will build and return the [CommandLineApplication](CommandLineApplication.md).

```cs
/// <summary>
/// Builds and returns the CommandLineApplication
/// </summary>
/// <returns>The CommandLineApplication</returns>
public CommandLineApplication Build()
```
---
See also,

- [CommandLineApplication](CommandLineApplication.md)
- [Commands](Commands.md)
- [CommandBase](CommandBase.md)
- [Dependency Injection](DependencyInjection.md)
- [IAnsiConsole](IAnsiConsole.md)
- [RunResult](RunResult.md)
- [RunResultCode](RunResultCode.md)