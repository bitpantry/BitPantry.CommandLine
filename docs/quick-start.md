# Getting Started

Install the NuGet package, define a command, build the application, and run it — all in under five minutes.

---

## Install

```shell
dotnet add package BitPantry.CommandLine
```

---

## Define a Command

Create a class that extends `CommandBase` and decorate it with attributes:

```csharp
using BitPantry.CommandLine;
using BitPantry.CommandLine.API;

[Command(Name = "hello")]
[Description("Prints a greeting")]
public class HelloCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    [Description("The name to greet")]
    public string Name { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        Console.MarkupLine($"Hello, [bold]{Name}[/]!");
    }
}
```

---

## Build the Application

Use `CommandLineApplicationBuilder` to register commands and build:

```csharp
using BitPantry.CommandLine;

var app = new CommandLineApplicationBuilder()
    .RegisterCommand<HelloCommand>()
    .Build();
```

---

## Run — Interactive Mode

Start a REPL loop with autocomplete and syntax highlighting:

```csharp
await app.RunInteractive();
```

```
app> hello World
Hello, World!

app> hello --help
HELLO
  Prints a greeting

Usage: hello <name>

Arguments:
  name    The name to greet    [required]

app>
```

---

## Run — Single Command Mode

Execute one command and exit. Useful for CLI tools, scripts, and CI pipelines:

```csharp
var result = await app.RunOnce("hello World");
// result.ResultCode == RunResultCode.Success
```

---

## Next Steps

You now have a working command-line application. From here you can:

- Add [arguments](commands/arguments.md), [flags](commands/flags.md), and [positional arguments](commands/positional-arguments.md)
- Organize commands into [groups](commands/groups.md)
- Configure [autocomplete](autocomplete/index.md) for argument values
- Connect to a [remote server](remote/index.md) for distributed execution

---

## See Also

- [Introduction & Overview](index.md)
- [Defining Commands](commands/index.md)
- [Building the Application](building/index.md)
- [Running Commands](running/index.md)
