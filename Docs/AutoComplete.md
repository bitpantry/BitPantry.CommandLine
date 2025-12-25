# AutoComplete Feature Guide

The BitPantry.CommandLine library includes a comprehensive autocomplete system that provides intelligent tab completion for commands, arguments, and values.

## Overview

The autocomplete system provides:

- **Command completion**: Tab completes available commands
- **Argument completion**: Completes `--argument` names and `-a` aliases
- **Value completion**: Context-aware value completion (file paths, enums, static values)
- **Ghost text**: Shows inline suggestions as you type
- **History integration**: Suggests previously used commands

## Quick Start

Autocomplete is enabled by default. Simply run your command-line application and press **Tab** to see suggestions.

```csharp
var app = CommandLineApplication.Create()
    .RegisterCommands<MyCommands>()
    .Build();

await app.RunAsync();
```

## Keyboard Controls

| Key | Action |
|-----|--------|
| `Tab` | Open completion menu / Accept selected item |
| `↑`/`↓` | Navigate menu items |
| `Escape` | Close menu without selecting |
| `Right Arrow` | Accept ghost text suggestion |
| Any printable character | Filter menu items |

## Custom Completion Providers

### File Path Completion

Use the `[FilePathCompletion]` attribute to enable file path suggestions:

```csharp
[Command]
public class CopyCommand
{
    public async Task<int> Execute(
        [Argument(Required = true)]
        [FilePathCompletion(Extensions = ".txt,.md")]
        string source,
        
        [Argument(Required = true)]
        [DirectoryPathCompletion]
        string destination)
    {
        // Implementation
    }
}
```

### Static Value Completion

Use the `[Completion]` attribute for predefined values:

```csharp
[Command]
public class ServerCommand
{
    public async Task<int> Execute(
        [Argument]
        [Completion("development", "staging", "production")]
        string environment)
    {
        // Implementation
    }
}
```

### Enum Completion

Enum types are automatically completed:

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

[Command]
public class LogCommand
{
    public async Task<int> Execute(
        [Argument]
        LogLevel level = LogLevel.Info)
    {
        // Implementation
    }
}
```

### Custom Provider

Implement `ICompletionProvider` for dynamic completions:

```csharp
public class DatabaseNameProvider : ICompletionProvider
{
    public int Priority => 100;

    public bool CanHandle(CompletionContext context)
    {
        return context.ArgumentName == "database";
    }

    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var databases = await GetDatabaseNamesAsync();
        var items = databases.Select(db => new CompletionItem
        {
            DisplayText = db,
            InsertText = db,
            Description = "Database",
            Kind = CompletionItemKind.Value
        }).ToList();

        return new CompletionResult(items);
    }
}
```

Register the provider:

```csharp
var app = CommandLineApplication.Create()
    .RegisterCommands<MyCommands>()
    .ConfigureServices(services =>
    {
        services.AddSingleton<ICompletionProvider, DatabaseNameProvider>();
    })
    .Build();
```

## Remote Completion

For remote commands (SignalR), completions are fetched from the server:

```csharp
var app = CommandLineApplication.Create()
    .UseSignalR(options =>
    {
        options.ServerUrl = "https://server.example.com/commandhub";
    })
    .Build();
```

Remote completions include:
- 3-second timeout for network requests
- Automatic caching of results
- Loading indicator during fetch
- Fallback to cached results on timeout

## Caching

The autocomplete system caches completion results for performance:

- **TTL**: Results are cached for 30 seconds by default
- **Invalidation**: Cache is automatically cleared when commands execute
- **Key**: Cache is keyed by command name + argument name + partial value

## Ghost Text

Ghost text shows inline suggestions as you type. To accept:
- Press `Right Arrow` to accept the full suggestion
- Press `Tab` to see all matching options

Ghost text priority:
1. Command history (most recent first)
2. Available commands matching the prefix

## Menu Behavior

- Single match: Automatically accepted (no menu shown)
- Multiple matches: Menu opens for selection
- No matches: "No matches" indicator shown
- Large result sets: Menu scrolls with indicator

## Configuration

### Custom Match Mode

The default matching uses case-insensitive prefix matching. This ensures:
- "server" matches "ServerMain" and "server-dev"
- Filtering narrows results as you type

### Menu Size

The menu displays up to 10 items by default with scroll support for larger result sets.

## Best Practices

1. **Use specific completion attributes**: Add `[FilePathCompletion]` or `[Completion]` to provide relevant suggestions
2. **Implement custom providers for dynamic data**: Database names, API endpoints, etc.
3. **Keep completion results focused**: Return relevant items to avoid overwhelming users
4. **Use descriptions**: Add `Description` to `CompletionItem` for context

## Troubleshooting

### Completions not appearing
- Ensure the command is registered
- Check that the argument name matches
- Verify the completion provider is registered

### Remote completions timing out
- Check network connectivity
- Verify the SignalR hub is running
- Increase timeout if needed

### Cache issues
- Cache invalidates on command execution
- Manual invalidation: `orchestrator.InvalidateCacheForCommand("commandName")`
