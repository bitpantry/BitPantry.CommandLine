# Commands

[← Back to Implementer Guide](../ImplementerGuide.md)

A command is a class that implements certain convention-based characteristics which define the concept and behaviors of a command.

## Table of Contents

- [Overview](#overview)
- [Minimal Command Class Requirements](#minimal-command-class-requirements)
- [Command Attribute](#command-attribute)
  - [Command Name](#command-name)
- [Command Groups](#command-groups)
  - [Defining Groups](#defining-groups)
  - [Assigning Commands to Groups](#assigning-commands-to-groups)
  - [Nested Groups](#nested-groups)
  - [Group Attribute Properties](#group-attribute-properties)
  - [Group Discoverability](#group-discoverability)
- [Arguments](#arguments)
  - [Argument Value Parsing](#argument-value-parsing)
  - [Argument Attribute](#argument-attribute)
  - [Argument Alias Attribute](#alias-attribute)
  - [Required Arguments](#required-arguments)
- [Options](#options)
- [The Execute Function](#the-execute-function)
  - [Synchronous and Asynchronous Execution](#synchronous-and-asynchronous-execution)
  - [Inputs and Outputs](#inputs-and-outputs)
- [Description Attribute](#description-attribute)
- [Order of Execution](#order-of-execution)
- [See Also](#see-also)

## Overview

Commands are the core building blocks of a BitPantry.CommandLine application. Each command:

- Inherits from [CommandBase](CommandBase.md)
- Has an `Execute` method that runs when the command is invoked
- Can accept arguments and options from the command line
- Has access to the console for output via the inherited `Console` property

## Minimal Command Class Requirements
At minimum, a command class must (1) inherit from the [CommandBase](CommandBase.md) class, which provides various application services to the command, and (2) it must define a public ```Execute(CommandExecutionContext ctx)``` function.

The following class, ```MyCommand``` is an example of a class that implements the minimal command class requirements as defined above.

```cs
class MyCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

Once [registered](CommandLineApplicationBuilder.md#registering-commands), the command can be executed using the [string command expression], "myCommand" (case insensitive).

Related resources,

- *For more information on registering commands, see [QuickStart](QuickStart.md) or [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md#registering-commands).*
- *For more information on the ```CommandExecutionContext``` class, see [CommandExecutionContext](CommandExecutionContext.md).*

## Command Attribute
By default, the command name is the name of the class (case insensitive), but it can be overridden by using the `Command` attribute.

### Command Name

```cs
[Command(Name="myCmd")]
class MyCommand : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

Now instead of using the [string command expression](CommandSyntax.md) "myCommand" to execute the command, use "myCmd". Once a name is defined using the `Command` attribute, the class name will no longer be recognized as a valid command name.

## Command Groups

Commands can be organized into groups for better organization. Groups are defined using **marker classes** decorated with the `[Group]` attribute, and commands are assigned to groups using the `Command` attribute's `Group` property.

### Defining Groups

Groups are defined as marker classes (empty classes) with the `[Group]` attribute:

```cs
[Group]
public class MyGroup { }
```

The group name is derived from the class name (lowercased, with "Group" suffix removed). You can override the name and add a description:

```cs
[Group(Name = "math")]
[Description("Mathematical operations")]
public class MathGroup { }
```

### Assigning Commands to Groups

Use the `Command` attribute's `Group` property to assign a command to a group:

```cs
[Group(Name = "my")]
public class MyGroup { }

[Command(Group = typeof(MyGroup))]
class Cmd : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

Groups and command names are separated by spaces, so the string command expression used to execute the Cmd command will be "my cmd".

### Nested Groups

Groups can be nested to create hierarchies by using C# nested classes:

```cs
[Group(Name = "my")]
public class MyGroup
{
    [Group(Name = "math")]
    [Description("Mathematical operations")]
    public class MathGroup { }
}

[Command(Group = typeof(MyGroup.MathGroup), Name = "add")]
class Add : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

To execute the *Add* command, use "my math add".

This is helpful for organizing related commands. For example, you could add another math command, *Subtract*, in the same group:

```cs
[Command(Group = typeof(MyGroup.MathGroup), Name = "subtract")]
class Subtract : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

Both *Add* and *Subtract* are now accessible under "my math add" and "my math subtract".

### Group Attribute Properties

The `[Group]` attribute supports the following properties:

| Property | Description |
|----------|-------------|
| `Name` | Optional. Overrides the group name derived from the class name. |

To add a description to a group, use the `[Description]` attribute on the class:

```cs
[Group(Name = "math")]
[Description("Mathematical operations")]
public class MathGroup { }
```

Command names defined via the `Command` attribute and group names will be validated by the parser using the C# `CodeDomProvider` as valid identifiers. Invalid names will result in a `CommandDescriptionException`.

### Group Discoverability

One of the key benefits of groups is **discoverability**. Users can explore available commands by invoking a group name alone:

```
> myapp math
```

This displays all subgroups and commands within the `math` group, along with their descriptions. Users can also use `--help` or `-h`:

```
> myapp math --help
```

This pattern works at any level of the hierarchy, including the root:

```
> myapp
```

See [Help System](Help.md) for complete documentation on the help output format and customization.

## Arguments
Arguments are parsed from the input string into properties on the command class. These properties must be decorated with the ```Argument``` attribute and expose a public setter to be found and recognized as valid command arguments.

Updating the *MyCommand* class we can add an integer argument called *myInt*.

```cs
class MyCommand : CommandBase
{
    [Argument]
    public int MyInt { set; }

    public void Execute(CommandExecutionContext ctx)
    {
        throw new NotImplementedException();
    }
}
```

The following [string command expression](CommandSyntax.md) can be used to invoke the *MyCommand* command and pass in a value for *MyInt* (***arguments are prefixed with a double-dash***)

```myCommand --myInt 10```

### Argument Value Parsing
The parser will attempt to parse any string input to the target property type using the [BitPantry.Parsing.Strings](https://github.com/bitpantry/BitPantry.Parsing.Strings) package,  which is capable of parsing all primative types, enumerations (from string or value representations), as well as arrays and implementations of ```ICollection<>``` (including, ```IDictionary<,>``` as an implementation of ```ICollection<KeyValuePair<,>>```).

So, for example, we could update the *MyInt* argument to be a ```Dictionary<string, bool>``` called *MyDict*.

```cs
[Argument]
public Dictionary<string, bool> MyDict { get; set; }
```

The command can now be executed using the following [string command expression](CommandSyntax.md), and upon execution, the *MyDict* dictionary will contain two key value pairs.

```myCommand --myDict "cats=no;dogs=yes"```

### Argument Attribute
The ```Argument``` attribute can be applied to an argument property to define additional arugment behaviors.

By default, the argument name is the name of the property. However, a name can also be assigned to the argument using the ```Argument``` attribute. Once a name is assigned using the ```Argument``` attribute, the property name is ignored.

```cs
[Argument(Name="int")]
public int MyInt { set; }
```

Now, instead of using "myInt" in the [string command expression](CommandSyntax.md), use "int" (case insensitive).

```myCommand --int 10```

Also, the ```Argument``` attribute can be used to provide argument auto-complete values. 

```cs
class MyCommand : CommandBase
{
    [Argument(AutoCompleteFunctionName = nameof(Fruit_AutoComplete))]
    public string Fruit { get; set; }

    ...

    public List<AutoCompleteOption> Fruit_AutoComplete(AutoCompleteContext context)
    {
        return new List<AutoCompleteOption>
        {
            new AutoCompleteOption("Orange"),
            new AutoCompleteOption("Apple"),
            new AutoCompleteOption("Bannana"),
        };
    }
}
```

For more information, see [auto-complete](AutoComplete.md).

### Alias Attribute

The ```Alias``` attribute can be applied to an argument property to define a single-character argument alias.

```cs
[Alias('i')]
public int MyInt { set; }
```

 Aliases can be used *in addition to* the argument name, so that both of these [string command expressions](CommandSyntax.md) will work (***aliases are prefixed with a single dash***)

```myCommand --MyInt 10```

```myCommand -i 10```

### Required Arguments

By default, arguments are optional. Use `IsRequired = true` to make an argument mandatory:

```cs
[Argument(IsRequired = true)]
[Description("The file path to process")]
public string FilePath { get; set; }
```

If a required argument is not provided, the command fails with a validation error.

## Flags

Flags are boolean arguments that don't take a value - they are either present (true) or absent (false). Use the `[Flag]` attribute to mark a `bool` property as a flag.

```cs
class MyCommand : CommandBase
{
    [Argument]
    [Flag]
    public bool Verbose { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        if(Verbose)
            Console.WriteLine("Verbose mode enabled");
        else
            Console.WriteLine("Normal mode");
    }
}
```

When entering a command, a flag works just like an argument, except there is no associated value.

```mycommand --verbose```

The ```Argument``` and ```Alias``` attributes can also be used to decorate a flag argument.

```cs
[Argument(Name = "v")]
[Alias('v')]
[Flag]
public bool Verbose { get; set; }
```

**Note:** The `[Flag]` attribute can only be applied to `bool` properties. Applying it to other types will result in a validation error at startup.

## The Execute Function
The ```Execute``` function is called by the command line application when the command is invoked. At a minimum, the ```Execute``` function must be called *"Execute"*, be public, and accept a ```CommandExecutionContext```.

### Synchronous and Asynchronous Execution

The function can be implemented as synchronous or asynchronous.

```cs
public void Execute(CommandExecutionContext ctx)
```

```cs
public async Task Execute(CommandExecutionContext ctx)
```

### Inputs and Outputs
Commands can return data by returning a value from the ```Execute``` function. Return data will either be returned from the [CommandLineApplication](CommandLineApplication.md) ```Run``` function as the ```Result``` property of the [RunResult](RunResult.md), or passed to the next command in the [Command Pipeline](CommandPipeline.md).

```cs
public int Execute(CommandExecutionContext ctx)
{
   return 5; 
}
```

```cs
public async Task<int> Execute(CommandExecutionContext ctx)
{
    return await DoSomeMathAsync();
}
```

Commands can also receive data from the pipeline by using the generic ```CommandExecutionContext<T>```. The following example defines a command that can receive an integer input.

```cs
public void Execute(CommandExecutionContext<int> ctx)
```

The Generic ```CommandExecutionContext<T>``` exposes an additional property, ```Input``` of type T. If no data is available on the [command pipeline](CommandPipeline.md), ```Input``` is set to ```default(T)```.

*See [Command Pipeline](CommandPipeline.md) for more information on how inputs and outputs interact between commands*

## Description Attribute
The ```Description``` attribute is used for self-documentation and can be applied to classes (commands and groups) and argument properties. It doesn't change the behavior of the command. When the command is parsed and registered, the descriptions assigned through the attribute are preserved in the command registry for the command and arguments (available from the [CommandExecutionContext](CommandExecutionContext.md)).

```cs
[Group(Name = "my")]
public class MyGroup
{
    [Group(Name = "math")]
    [Description("Mathematical operations")]
    public class MathGroup { }
}

[Description("Adds two numbers together")]
[Command(Group = typeof(MyGroup.MathGroup), Name = "add")]
class Add : CommandBase
{

    [Description("The first number")]
    [Argument]
    public int Num1 { set; }

    [Description("The second number")]
    [Argument]
    public int Num2 { set; }

    public int Execute(CommandExecutionContext ctx)
    {
        return Num1 + Num2;
    }
}
```

## Order of Execution
When a command is executed the following code is executed in order.

- The class is first created by the dependency injection service provider
- Argument properties are set
- The *Execute* function is invoked

When commands are registered with the [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md). For more information, see [Dependency Injection](DependencyInjection.md).

---

## See Also

**Core Concepts**
- [CommandBase](CommandBase.md) - Base class for all commands
- [IAnsiConsole](IAnsiConsole.md) - Console output with Spectre.Console
- [CommandExecutionContext](CommandExecutionContext.md) - Execution context passed to commands

**Command Structure**
- [ArgumentInfo](ArgumentInfo.md) - Argument metadata structure
- [CommandInfo](CommandInfo.md) - Command metadata structure
- [Command Syntax](CommandSyntax.md) - How commands are invoked
- [Help System](Help.md) - Built-in help for commands and groups

**Advanced Topics**
- [Command Pipeline](CommandPipeline.md) - Chaining commands together
- [Auto-Complete](AutoComplete.md) - Tab completion for arguments
- [Dependency Injection](DependencyInjection.md) - Injecting services
- [Logging](Logging.md) - Adding logging to commands

**External Resources**
- [BitPantry.Parsing.Strings](https://github.com/bitpantry/BitPantry.Parsing.Strings) - Value parsing library

