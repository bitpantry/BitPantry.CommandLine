# AutoComplete

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine provides Tab autocomplete functionality for commands and arguments. Users can press Tab while typing to get suggestions.

## Table of Contents

- [Overview](#overview)
- [Built-in Autocomplete](#built-in-autocomplete)
- [Custom Argument Autocomplete](#custom-argument-autocomplete)
- [AutoCompleteContext](#autocompletecontext)
- [AutoCompleteOption](#autocompleteoption)
- [Examples](#examples)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [See Also](#see-also)

## Overview

Autocomplete helps users:
- Discover available commands
- Find argument names
- Get suggested values for arguments

The framework provides built-in autocomplete for command names and argument names. You can add custom autocomplete for argument *values*.

## Built-in Autocomplete

These work automatically without any configuration:

| Type | Trigger | Example |
|------|---------|---------|
| Command names | Start typing command | `gre<Tab>` → `greet` |
| Groups | Type group name | `math<Tab>` → shows `add`, `subtract` in math group |
| Argument names | Type `--` after command | `greet --<Tab>` → shows `--name`, `--count` |
| Argument aliases | Type `-` after command | `greet -<Tab>` → shows `-n`, `-c` |

## Custom Argument Autocomplete

For argument values, define an autocomplete function using the `AutoCompleteFunctionName` property:

```csharp
[Command(Name = "greet")]
public class GreetCommand : CommandBase
{
    [Argument(AutoCompleteFunctionName = nameof(GetNameSuggestions))]
    [Alias('n')]
    public string Name { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Hello, {Name}!");
    }

    // Autocomplete function - must match this signature
    public List<AutoCompleteOption> GetNameSuggestions(AutoCompleteContext context)
    {
        return new List<AutoCompleteOption>
        {
            new AutoCompleteOption("Alice"),
            new AutoCompleteOption("Bob"),
            new AutoCompleteOption("Charlie")
        };
    }
}
```

### Requirements

The autocomplete function must:
- Be a public method in the same command class
- Accept `AutoCompleteContext` as the parameter
- Return `List<AutoCompleteOption>`
- Match the name specified in `AutoCompleteFunctionName`

## AutoCompleteContext

`BitPantry.CommandLine.AutoComplete.AutoCompleteContext`

Provides context about what the user is typing:

```csharp
public record AutoCompleteContext(
    string QueryString,                         // Current partial input
    Dictionary<ArgumentInfo, string> Values     // Already-provided argument values
);
```

| Property | Type | Description |
|----------|------|-------------|
| `QueryString` | `string` | The partial text the user has typed for this argument |
| `Values` | `Dictionary<ArgumentInfo, string>` | Values already provided for other arguments |

Use `Values` to provide context-aware suggestions based on other arguments.

## AutoCompleteOption

`BitPantry.CommandLine.AutoComplete.AutoCompleteOption`

Represents a single autocomplete suggestion:

```csharp
public class AutoCompleteOption
{
    public string Value { get; }      // The completion value
    public string Format { get; }     // Optional format string for display
    
    public AutoCompleteOption(string value, string format = null);
}
```

| Property | Type | Description |
|----------|------|-------------|
| `Value` | `string` | The actual completion value |
| `Format` | `string` | Optional format string for displaying the option |

## Examples

### Basic Static Autocomplete

Simple list of predefined options:

```csharp
[Command(Name = "fruit")]
public class FruitCommand : CommandBase
{
    [Argument(AutoCompleteFunctionName = nameof(GetFruits))]
    public string Fruit { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"You chose: {Fruit}");
    }

    public List<AutoCompleteOption> GetFruits(AutoCompleteContext context)
    {
        return new List<AutoCompleteOption>
        {
            new AutoCompleteOption("Apple"),
            new AutoCompleteOption("Banana"),
            new AutoCompleteOption("Cherry"),
            new AutoCompleteOption("Orange")
        };
    }
}
```

### Dynamic Autocomplete Based on Context

Filter suggestions based on other argument values:

```csharp
[Command(Name = "order")]
public class OrderCommand : CommandBase
{
    [Argument]
    public string Category { get; set; }

    [Argument(AutoCompleteFunctionName = nameof(GetProducts))]
    public string Product { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Ordered: {Product} from {Category}");
    }

    public List<AutoCompleteOption> GetProducts(AutoCompleteContext context)
    {
        // Get the category value if already provided
        var categoryArg = context.Values.Keys
            .FirstOrDefault(k => k.Name == "Category");
        
        var category = categoryArg != null 
            ? context.Values[categoryArg] 
            : null;

        // Return products based on category
        return category switch
        {
            "Electronics" => new List<AutoCompleteOption>
            {
                new AutoCompleteOption("Laptop"),
                new AutoCompleteOption("Phone"),
                new AutoCompleteOption("Tablet")
            },
            "Clothing" => new List<AutoCompleteOption>
            {
                new AutoCompleteOption("Shirt"),
                new AutoCompleteOption("Pants"),
                new AutoCompleteOption("Jacket")
            },
            _ => new List<AutoCompleteOption>
            {
                new AutoCompleteOption("(Select a category first)")
            }
        };
    }
}
```

### Async Autocomplete with External Data

For async data sources, the framework supports async autocomplete functions:

```csharp
[Command(Name = "user")]
public class UserCommand : CommandBase
{
    private readonly IUserService _userService;

    public UserCommand(IUserService userService)
    {
        _userService = userService;
    }

    [Argument(AutoCompleteFunctionName = nameof(GetUsernames))]
    public string Username { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Selected user: {Username}");
    }

    public async Task<List<AutoCompleteOption>> GetUsernames(AutoCompleteContext context)
    {
        // Fetch from external service
        var users = await _userService.SearchUsersAsync(context.QueryString);
        
        return users
            .Select(u => new AutoCompleteOption(u.Username))
            .ToList();
    }
}
```

### Formatted Display Options

Use format strings to display additional context:

```csharp
public List<AutoCompleteOption> GetUsers(AutoCompleteContext context)
{
    return new List<AutoCompleteOption>
    {
        new AutoCompleteOption("jdoe", "{0} - John Doe"),
        new AutoCompleteOption("asmith", "{0} - Alice Smith"),
        new AutoCompleteOption("bjones", "{0} - Bob Jones")
    };
}
```

This displays as:
```
jdoe - John Doe
asmith - Alice Smith
bjones - Bob Jones
```

But inserts only the `Value` when selected.

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Tab** | Show suggestions / select next suggestion |
| **Shift+Tab** | Select previous suggestion |
| **Enter** | Accept current suggestion |
| **Escape** | Cancel autocomplete |
| **Arrow Keys** | Navigate suggestions |

## See Also

- [Commands](Commands.md) - Defining command arguments
- [ArgumentInfo](ArgumentInfo.md) - Argument metadata including autocomplete
- [REPL](REPL.md) - Interactive mode with autocomplete
- [End User Guide](../EndUserGuide.md) - User documentation for autocomplete