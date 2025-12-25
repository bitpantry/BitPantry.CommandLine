# AutoComplete

[← Back to Implementer Guide](../ImplementerGuide.md)

BitPantry.CommandLine provides a comprehensive Tab autocomplete system with intelligent suggestions for commands, arguments, and values. The system includes ghost text preview, menu navigation, and caching for optimal performance.

## Table of Contents

- [Overview](#overview)
- [Built-in Autocomplete](#built-in-autocomplete)
- [Completion Attributes](#completion-attributes)
- [Custom Completion Providers](#custom-completion-providers)
- [CompletionContext](#completioncontext)
- [CompletionItem](#completionitem)
- [Examples](#examples)
- [Ghost Text](#ghost-text)
- [Caching](#caching)
- [Keyboard Shortcuts](#keyboard-shortcuts)
- [See Also](#see-also)

## Overview

The autocomplete system provides:

- **Command completion**: Tab completes available commands and groups
- **Argument completion**: Completes `--argument` names and `-a` aliases
- **Value completion**: Context-aware value completion (file paths, enums, static values, custom providers)
- **Ghost text**: Shows inline suggestions as you type
- **History integration**: Suggests previously used commands
- **Remote support**: Fetches completions from SignalR servers

## Built-in Autocomplete

These work automatically without any configuration:

| Type | Trigger | Example |
|------|---------|---------|
| Command names | Start typing command | `gre<Tab>` → `greet` |
| Groups | Type group name | `math<Tab>` → shows `add`, `subtract` in math group |
| Argument names | Type `--` after command | `greet --<Tab>` → shows `--name`, `--count` |
| Argument aliases | Type `-` after command | `greet -<Tab>` → shows `-n`, `-c` |
| Enum values | After enum argument | `--level <Tab>` → shows enum values |

## Completion Attributes

Use attributes to declaratively specify completions for argument values.

### Static Values - `[Completion]`

Provide a fixed list of completion values:

```csharp
[Command(Name = "deploy")]
public class DeployCommand : CommandBase
{
    [Argument]
    [Completion("development", "staging", "production")]
    public string Environment { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Deploying to {Environment}");
    }
}
```

### File Paths - `[FilePathCompletion]`

Enable file path suggestions with optional extension filtering:

```csharp
[Command(Name = "open")]
public class OpenCommand : CommandBase
{
    [Argument(Required = true)]
    [FilePathCompletion(Extensions = ".txt,.md,.json")]
    public string FilePath { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Opening {FilePath}");
    }
}
```

### Directory Paths - `[DirectoryPathCompletion]`

Enable directory path suggestions:

```csharp
[Command(Name = "cd")]
public class ChangeDirectoryCommand : CommandBase
{
    [Argument(Required = true)]
    [DirectoryPathCompletion]
    public string Directory { get; set; }

    public void Execute(CommandExecutionContext ctx)
    {
        System.IO.Directory.SetCurrentDirectory(Directory);
    }
}
```

### Enum Types

Enum arguments are automatically completed without any attribute:

```csharp
public enum LogLevel { Debug, Info, Warning, Error, Critical }

[Command(Name = "log")]
public class LogCommand : CommandBase
{
    [Argument]
    public LogLevel Level { get; set; } = LogLevel.Info;

    public void Execute(CommandExecutionContext ctx)
    {
        Console.WriteLine($"Log level: {Level}");
    }
}
```

## Custom Completion Providers

For dynamic completions, implement `ICompletionProvider`:

```csharp
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Providers;

public class DatabaseNameProvider : ICompletionProvider
{
    private readonly IDatabaseService _dbService;
    
    public int Priority => 100; // Higher = checked first

    public DatabaseNameProvider(IDatabaseService dbService)
    {
        _dbService = dbService;
    }

    public bool CanHandle(CompletionContext context)
    {
        // Handle arguments named "database" or "db"
        return context.ArgumentName == "database" 
            || context.ArgumentName == "db";
    }

    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var databases = await _dbService.GetDatabaseNamesAsync(cancellationToken);
        
        var items = databases
            .Where(db => db.StartsWith(context.PartialValue, StringComparison.OrdinalIgnoreCase))
            .Select(db => new CompletionItem
            {
                DisplayText = db,
                InsertText = db,
                Description = "Database",
                Kind = CompletionItemKind.Value
            })
            .ToList();

        return new CompletionResult(items);
    }
}
```

Register the provider in your application:

```csharp
var app = CommandLineApplication.Create()
    .RegisterCommands<MyCommands>()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICompletionProvider, DatabaseNameProvider>();
    })
    .Build();
```

## CompletionContext

`BitPantry.CommandLine.AutoComplete.CompletionContext`

Provides context about the current completion request:

| Property | Type | Description |
|----------|------|-------------|
| `CommandName` | `string` | The resolved command name |
| `ArgumentName` | `string` | Current argument being completed (null if completing command) |
| `PartialValue` | `string` | The partial text typed by the user |
| `ElementType` | `CompletionElementType` | What type of element is being completed |
| `ArgumentInfo` | `ArgumentInfo` | Metadata about the argument (if applicable) |

### CompletionElementType Values

| Value | Description |
|-------|-------------|
| `Command` | Completing a command or group name |
| `ArgumentName` | Completing `--argumentName` |
| `ArgumentAlias` | Completing `-a` alias |
| `ArgumentValue` | Completing a value for an argument |

## CompletionItem

`BitPantry.CommandLine.AutoComplete.CompletionItem`

Represents a single completion suggestion:

| Property | Type | Description |
|----------|------|-------------|
| `DisplayText` | `string` | Text shown in the completion menu |
| `InsertText` | `string` | Text inserted when selected |
| `Description` | `string` | Optional description shown alongside |
| `Kind` | `CompletionItemKind` | Category icon (Command, Argument, Value, File, etc.) |
| `SortText` | `string` | Optional text for sorting (defaults to DisplayText) |

### CompletionItemKind Values

| Value | Description |
|-------|-------------|
| `Command` | A command name |
| `Argument` | An argument name (`--name`) |
| `Alias` | An argument alias (`-n`) |
| `Value` | A value suggestion |
| `File` | A file path |
| `Directory` | A directory path |
| `Enum` | An enum value |
| `History` | A command from history |

## Examples

### Context-Aware Completions

Use other argument values to filter suggestions:

```csharp
public class ProductProvider : ICompletionProvider
{
    public int Priority => 100;

    public bool CanHandle(CompletionContext context)
    {
        return context.ArgumentName == "product";
    }

    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        // Check if category was already provided in the input
        var categoryMatch = Regex.Match(context.FullInput, @"--category\s+(\w+)");
        var category = categoryMatch.Success ? categoryMatch.Groups[1].Value : null;

        var products = category switch
        {
            "electronics" => new[] { "laptop", "phone", "tablet" },
            "clothing" => new[] { "shirt", "pants", "jacket" },
            _ => new[] { "laptop", "phone", "tablet", "shirt", "pants", "jacket" }
        };

        var items = products
            .Where(p => p.StartsWith(context.PartialValue, StringComparison.OrdinalIgnoreCase))
            .Select(p => new CompletionItem
            {
                DisplayText = p,
                InsertText = p,
                Kind = CompletionItemKind.Value
            })
            .ToList();

        return Task.FromResult(new CompletionResult(items));
    }
}
```

### Remote API Completions

Fetch completions from an external API:

```csharp
public class UserProvider : ICompletionProvider
{
    private readonly HttpClient _httpClient;
    
    public int Priority => 100;

    public bool CanHandle(CompletionContext context)
    {
        return context.ArgumentName == "username";
    }

    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _httpClient.GetFromJsonAsync<List<User>>(
                $"/api/users?search={context.PartialValue}",
                cancellationToken);

            var items = users.Select(u => new CompletionItem
            {
                DisplayText = u.Username,
                InsertText = u.Username,
                Description = u.FullName,
                Kind = CompletionItemKind.Value
            }).ToList();

            return new CompletionResult(items);
        }
        catch (OperationCanceledException)
        {
            return CompletionResult.Empty;
        }
    }
}
```

## Ghost Text

Ghost text shows inline suggestions as you type, appearing in a dimmed style after the cursor.

**How it works:**
1. As you type, the system shows the best matching completion inline
2. Press `Right Arrow` to accept the ghost text
3. Press `Tab` to open the full menu with all options

**Ghost text priority:**
1. Command history (most recent matching command)
2. Best matching command/argument/value

## Caching

The autocomplete system caches results for performance:

| Setting | Value |
|---------|-------|
| Default TTL | 30 seconds |
| Cache key | Command + Argument + Partial value |
| Invalidation | Automatic after command execution |

**Manual cache invalidation:**

```csharp
// Inject ICompletionOrchestrator
orchestrator.InvalidateCacheForCommand("mycommand");
```

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **Tab** | Open menu / Accept selected item |
| **↑** / **↓** | Navigate menu items |
| **Right Arrow** | Accept ghost text suggestion |
| **Escape** | Close menu without selecting |
| **Any character** | Filter menu items |
| **Backspace** | Remove character, update suggestions |

## Menu Behavior

- **Single match**: Automatically accepted (no menu shown)
- **Multiple matches**: Menu opens for selection
- **No matches**: "No matches" indicator displayed
- **Large result sets**: Menu scrolls with position indicator

## See Also

- [Commands](Commands.md) - Defining command arguments
- [ArgumentInfo](ArgumentInfo.md) - Argument metadata
- [REPL](REPL.md) - Interactive mode with autocomplete
- [End User Guide](../EndUserGuide.md) - User documentation for autocomplete
- [AutoComplete Feature Guide](../AutoComplete.md) - Comprehensive feature documentation