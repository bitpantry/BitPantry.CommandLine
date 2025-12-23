# Quick Start

[← Back to Implementer Guide](../ImplementerGuide.md)

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

To register the command with the command line application, go back to the [```CommandLineApplicationBuilder```](commandLineApplicationBuilder.md) and add the following.

```cs
    var app = new CommandLineApplicationBuilder()
        .RegisterCommand(typeof(HelloWorld))
        .Build();
```

Now when you enter *mycmd helloWorld** from the system command line, you should see something like the following ...

```
C:\code\myFirstCmdLine\bin\release> mycmd helloWorld
Hello World! 
```

## Next Steps

Now that you have a working command, continue learning:

1. **[Commands](Commands.md)** - Learn about arguments, options, aliases, and more
2. **[Dependency Injection](DependencyInjection.md)** - Inject services into your commands
3. **[AutoComplete](AutoComplete.md)** - Add tab-completion for better UX
4. **[REPL Mode](REPL.md)** - Run as an interactive shell

---

## See Also

- [CommandLineApplicationBuilder](CommandLineApplicationBuilder.md) - Full configuration options
- [Commands](Commands.md) - Complete command definition guide
- [Implementer Guide](../ImplementerGuide.md) - Full learning path for developers