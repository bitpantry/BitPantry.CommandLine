# REPL

[← Back to Implementer Guide](../ImplementerGuide.md)

The REPL (Read-Eval-Print Loop) is the interactive command execution environment provided by BitPantry.CommandLine.

## Table of Contents

- [Starting the REPL](#starting-the-repl)
- [Command Input](#command-input)
- [Input History](#input-history)
- [Tab Autocomplete](#tab-autocomplete)
- [Prompt Customization](#prompt-customization)
- [See Also](#see-also)

## Starting the REPL

Call `Run()` without arguments to start the interactive REPL:

```csharp
var app = builder.Build();
await app.Run();  // Starts REPL
```

When running, users can execute commands at the prompt:

```
> helloworld
Hello World!
>
```

## Command Input

The REPL supports standard console input:

- Type command text and press **Enter** to execute
- **Backspace** and **Delete** for editing
- **Home** and **End** to move cursor
- **Left/Right Arrow** for cursor positioning
- **Ctrl+C** to cancel current input

## Input History

The REPL maintains a history of previously executed commands within the session.

### Navigation

- **Up Arrow** - Navigate to previous command
- **Down Arrow** - Navigate to next command

### Behavior

- History is session-based (not persisted between application runs)
- Empty lines are not added to history
- Duplicate consecutive commands are stored once

### Example

```
> greet --name "Alice"
Hello, Alice!
> greet --name "Bob"
Hello, Bob!
> [Up Arrow]
> greet --name "Bob"      # Previous command appears
> [Up Arrow]
> greet --name "Alice"    # Earlier command appears
```

## Tab Autocomplete

The REPL provides Tab completion for commands and arguments. When Tab is pressed, the system suggests completions based on:

1. **Command names** - Matches registered commands
2. **Namespaces** - Matches command namespace prefixes
3. **Argument names** - After typing `--`, shows available arguments
4. **Argument values** - For arguments with custom autocomplete functions

### Implementation

Commands can define autocomplete functions for their arguments:

```csharp
[Command(Name = "greet")]
public class GreetCommand : CommandBase
{
    [Argument(AutoCompleteFunctionName = nameof(GetNames))]
    public string Name { get; set; }

    public List<AutoCompleteOption> GetNames(AutoCompleteContext context)
    {
        return new List<AutoCompleteOption>
        {
            new AutoCompleteOption("Alice"),
            new AutoCompleteOption("Bob"),
            new AutoCompleteOption("Charlie")
        };
    }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Hello, {Name}!");
    }
}
```

See [AutoComplete](AutoComplete.md) for detailed autocomplete implementation.

## Prompt Customization

The default prompt is `> `. You can customize the prompt appearance during application setup.

### Setting a Custom Prompt

The prompt can be configured through the `Prompt` class which is registered as a service:

```csharp
// After building the app, access the prompt service
var prompt = app.Services.GetService<Prompt>();
// Configure as needed for your application
```

### Remote Connection Prompts

When connected to a remote server, the prompt typically changes to indicate the connection:

```
# Local mode
> 

# Connected to remote
remote.server.com> 
```

## See Also

- [AutoComplete](AutoComplete.md) - Implementing custom autocomplete
- [CommandLineApplication](CommandLineApplication.md) - Application execution
- [End User Guide](../EndUserGuide.md) - User-focused documentation
- [Command Syntax](CommandSyntax.md) - Command input format