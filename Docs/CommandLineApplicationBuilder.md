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

## Configuring Dependency Injection

By default, each time a command is invoked, the [CommandLineApplication](CommandLineApplication.md) will create an instance of the command type using ```System.Activator.CreateInstance```, which doesn't allow for any instance management or dependency injection. 

The ```CommandLineApplicationBuilder``` can be configured to build a [CommandLineApplication](CommandLineApplication.md) that implements a specific dependency injection strategy. 

```cs
/// <summary>
/// Configures the CommandLineApplicationBuilder to build a CommandLineApplication that uses the given IContainer implementation for instance and dependency management
/// </summary>
/// <param name="container">The container implementation to use</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder UsingDependencyContainer(IContainer container)
```

*For more information, including how to create a custom ```IContainer``` implementation, see [Dependency Injection](DependencyInjection.md).*

## Configuring the Interface

The *Interface* defines the IO channels (in most cases, the user interface) used by the [CommandLineApplication](CommandLineApplication.md).
By default, the standard system console is used (e.g., ```Console.WriteLine("")``` or ```Console.ReadLine()```).

```cs
/// <summary>
/// Configures the CommandLineApplicationBuilder to build a CommandApplication that uses the given IInterface implementation
/// </summary>
/// <param name="interfc">The interface implementation to use</param>
/// <returns>The CommandLineApplicationBuilder</returns>
public CommandLineApplicationBuilder UsingInterface(IInterface interfc)
```

*For more information, including how to create a custom IInterface implementation, see [IInterface](IInterface.md).*

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
- [IInterface](IInterface.md)
- [RunResult](RunResult.md)
- [RunResultCode](RunResultCode.md)