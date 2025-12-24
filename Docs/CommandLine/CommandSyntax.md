# Command Line Syntax

[← Back to Implementer Guide](../ImplementerGuide.md)

Commands are executed (or *run*) by passing a *string command expression* into the *Run* function of a properly configured [CommandLineApplication](CommandLineApplication.md) object.

<span style="font-size:20px">```[group [subgroup ...]] commandName [--argName|-a] [argValue] [--option|-o]```</span>

| Element | Description |
|---------|-------------|
|```group```|The group of the command (if defined). Multiple groups can be nested.|
| ```commandName```|The name of the command.|
|```--argName``` <br/>*or* ```-a``` *(alias)*|The name of an argument (double dash, '--') or it's alias (single dash, '-', if an alias is defined). Attempting to use both an argument's name and it's defined alias in the same command expression will result in an error. Multiple arguments can be included in the expression (if they are also defined in the command type).|
|```argValue```|The value of the preceeding argument.<br/><br/>The command line parser uses spaces to delimit expression elements, so if the argument value has spaces, use double-quotes to ensure the value is interpreted as a single element / value in the command expression. Enclosed double-quotes are interpreted as part of the value (e.g., an argValue of *"she said, \"hello\""* will be correctly parssed as, *she said, "hello"!*)<br/><br/> All argValues are received by the parser as strings whether they have double-quotes or not. Only when assigning to an argument property are string types parsed to specific data types (see [argument value parsing](Commands.md#argument-value-parsing) for more information). This means if an integer, 5, is being passed in the expression to an integer argument, both *5* and *"5"* are equivalent.
|```--option```<br/>*or*```-o``` *(alias)*|The name of an option (double dash, '--') or it's alias (single dash, '-', if an alias is defined). Attempting to use both an options's name and it's defined alias in the same command expression will result in an error. Multiple options can be included in the expression (if they are also defined in the command type).

*NOTE: &nbsp; All command expression elements are case insensitive - but argument values (argValue) maintain their case sensitivity.*

## Examples
For the following command type ...

```cs
    class CommandWithArgument : CommandBase
    {
        [Argument]
        public int ArgOne { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
```

Use,

```commandWithArgument --argOne "hello world"```

For the following command type ...

```cs
    [Command(Name="cmd")]
    class CommandWithArgument : CommandBase
    {
        [Argument(Alias='a')]
        public int ArgOne { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
```

Use,

```cmd --argOne "hello world"```

 or,

```cmd -a "hello world"```

For the following command type ...

```cs
    [Group(Name = "my")]
    public class MyGroup
    {
        [Group(Name = "math")]
        public class MathGroup { }
    }

    [Command(Group = typeof(MyGroup.MathGroup), Name = "add")]
    class CommandWithArgument : CommandBase
    {
        [Argument(Alias='a')]
        public int ArgOne { get; set; }

        [Argument(Alias='b')]
        public int ArgTwo { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            throw new NotImplementedException();
        }
    }
```

Use,

```my math add -a 5 -b 3```

 or,

```my math add --argOne 5 --argTwo 3```

## Pipeline
Commands can also receive inputs and return outputs to the pipeline. A string command expression can include multiple commands that pass information along the pipeline using the pipeline character, '|'.

For example for the following command type (using the same group definitions as above) ...

```cs
    [Command(Group = typeof(MyGroup.MathGroup), Name = "add")]
    class CommandWithArgument : CommandBase
    {
        [Argument(Alias='n')]
        public int Num { get; set; }

        public int Execute(CommandExecutionContext<int> ctx)
        {
            return ctx.Input + Num;
        }
    }
```
Use the following string command expression to add two numbers together ...

```my math add 5 | my math add 3``` &nbsp;&nbsp; *which ultimately returns 8*

For more information on pipelining commands together, see [Pipeline](CommandPipeline.md).

---

See also,

- [Commands](Commands.md)
- [Command Pipeline](CommandPipeline.md)
- [CommandLineApplication](CommandLineApplication.md)

































