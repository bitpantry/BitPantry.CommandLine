
[![Build status](https://github.com/bitpantry/BitPantry.CommandLine/actions/workflows/build-core.yml/badge.svg)](https://github.com/bitpantry/BitPantry.CommandLine/actions/workflows/build-core.yml/badge.svg) [![NuGet Status](https://img.shields.io/nuget/v/bitpantry.commandline)](https://www.nuget.org/packages/BitPantry.CommandLine)

# BitPantry.CommandLine
A package for quickly bootstrapping and building command line applications.

```
NuGet\Install-Package BitPantry.CommandLine 
```

Additional packages support client / server functionality over SignalR.

```
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Server
```

```
NuGet\Install-Package BitPantry.CommandLine.Remote.SignalR.Client
```

See, [Command Line Server](Remote/CommandLineServer.md) to get started hosting and executing remote commands.

See, [File System](Remote/FileSystem.md) for using file system operations in remote commands.

# Quick Start
To get started, create a new command line application and add the following code to the main function.

```cs
using BitPantry.CommandLine;

class Program
{
    static Task<int> Main(string[] args)
    {
        var app = new CommandLineApplicationBuilder().Build();
        var result = await app.Run(String.Join(' ', args));

        Console.ReadKey();
        return 0;
    }
}
```

When executed, the ```args``` parameter will be joined into a single string (separated by an empty character) and passed into the newly built ```app.Run``` function where it will be parsed and executed returning a ```RunResult```. 

So, for example, if your console application compiled into an executable named *mycmd.exe*, you could run a command named *helloWorld* by entering ```mycmd helloWorld``` at the system command line.

But there is no *helloWorld* command yet - you have to create one. So, create a new class called ```HelloWorld``` and enter the following code.

```cs
using BitPantry.CommandLine.API;

public class HelloWorld : CommandBase
{
    public void Execute(CommandExecutionContext ctx)
    {
        Info.WriteLine("Hello World!");
    }
}
```

To register the command with the command line application, go back to the [```CommandLineApplicationBuilder```](CommandLine/CommandLineApplicationBuilder.md) in *Program.cs* and add the following.

```cs
    var app = new CommandLineApplicationBuilder()
        .RegisterCommand(typeof(HelloWorld))
        .Build();
```

Now when you enter *mycmd helloWorld* from the system command line, you should see something like the following ...

```
C:\code\myFirstCmdLine\bin\release> mycmd helloWorld
Hello World! 
```

# Using the REPL

If you'd rather run the command line application as a REPL, use the following code bootstrap your console application instead.

```cs
    var app = new CommandLineApplicationBuilder()
        .RegisterCommand(typeof(HelloWorld))
        .Build();

    await app.Run();
```

# Keep Learning
To keep learning, check out the following links

- [Commands](CommandLine/Commands.md) will show you all of the features that are available when defining a command class, including arguments and inputs & outputs
- The [CommandLineApplicationBuilder](CommandLine/CommandLineApplicationBuilder.md) will show you how to configure the command line application, including dependency injection and different interfaces
- [Command Line Syntax](CommandLine/CommandSyntax.md) will show you what's possible at the command line interface

