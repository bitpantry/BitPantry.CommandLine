# Contract: ProfileNameProvider

**Package**: `BitPantry.CommandLine.Remote.SignalR.Client`  
**Namespace**: `BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete`

## Interface Implementation

Implements `ICompletionProvider` to provide autocomplete suggestions for profile name arguments.

```csharp
namespace BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;

/// <summary>
/// Provides completion suggestions for profile name arguments.
/// </summary>
public class ProfileNameProvider : ICompletionProvider
{
    private readonly IProfileManager _profileManager;
    
    /// <summary>
    /// Priority 80 - higher than static values (70), lower than custom (90+).
    /// </summary>
    public int Priority => 80;
    
    public ProfileNameProvider(IProfileManager profileManager)
    {
        _profileManager = profileManager;
    }
    
    /// <summary>
    /// Handles argument values with [Completion(Provider = typeof(ProfileNameProvider))].
    /// </summary>
    public bool CanHandle(CompletionContext context)
    {
        if (context.ElementType != CompletionElementType.ArgumentValue)
            return false;
            
        return context.CompletionAttribute?.Provider == typeof(ProfileNameProvider);
    }
    
    /// <summary>
    /// Returns profile names matching the current prefix.
    /// </summary>
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return CompletionResult.Empty;
            
        var profiles = await _profileManager.GetAllProfilesAsync();
        var prefix = context.CurrentWord ?? string.Empty;
        
        var items = profiles
            .Where(p => p.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Name)
            .Select(p => new CompletionItem
            {
                DisplayText = p.Name,
                InsertText = p.Name,
                Kind = CompletionItemKind.ArgumentValue,
                Description = p.Uri,
                SortPriority = 0
            })
            .ToList();
            
        return new CompletionResult(items);
    }
}
```

## Usage in Commands

Apply the `[Completion]` attribute to profile name arguments:

```csharp
[Command(Group = typeof(ServerGroup), Name = "connect")]
public class ConnectCommand : CommandBase
{
    [Argument]
    [Alias('p')]
    [Description("Use saved profile")]
    [Completion(Provider = typeof(ProfileNameProvider))]
    public string Profile { get; set; }
}

[Command(Group = typeof(ProfileGroup), Name = "show")]
public class ProfileShowCommand : CommandBase
{
    [Argument(Position = 0)]
    [Description("Profile name")]
    [Completion(Provider = typeof(ProfileNameProvider))]
    public string Name { get; set; }
}
```

## Commands Using This Provider

| Command | Argument |
|---------|----------|
| `server connect` | `--profile` |
| `server profile add` | `<name>` (positional) |
| `server profile remove` | `<name>` (positional) |
| `server profile show` | `<name>` (positional) |
| `server profile set-default` | `<name>` (positional) |
| `server profile set-key` | `<name>` (positional) |

## Registration

```csharp
// In SignalR Client extension method
services.AddSingleton<ICompletionProvider, ProfileNameProvider>();
```

## Behavior

| Scenario | Result |
|----------|--------|
| No profiles exist | Empty completion list |
| Prefix matches some profiles | Filtered list |
| Empty prefix | All profiles |
| Case-insensitive matching | `pr` matches `Prod`, `PROD`, `prod` |

## Error Handling

If `IProfileManager.GetAllProfilesAsync()` throws:
- Return `CompletionResult.Empty`
- Log warning (don't block user input)
