# Data Model: Autocomplete Redesign

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Research**: [research.md](research.md)

## Entity Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              AUTOCOMPLETE SYSTEM                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐   │
│  │ CompletionContext│──────▶│ICompletionProvider│──────▶│ CompletionResult│  │
│  └─────────────────┘       └─────────────────┘       └─────────────────┘   │
│         │                         ▲                         │              │
│         │                         │                         │              │
│         │              ┌──────────┼──────────┐              │              │
│         │              │          │          │              ▼              │
│         │     ┌────────┴────┐ ┌───┴───┐ ┌────┴────┐   ┌──────────────┐    │
│         │     │FilePathProv.│ │EnumPr.│ │MethodPr│   │CompletionItem│    │
│         │     │DirectoryPr. │ │Values │ │CommandPr│  └──────────────┘    │
│         │     └─────────────┘ └───────┘ └─────────┘                       │
│         │                                                                  │
│         ▼                                                                  │
│  ┌─────────────────┐       ┌─────────────────┐                            │
│  │ CompletionCache │◀─────▶│    CacheKey     │   [Completion] Attribute   │
│  └─────────────────┘       └─────────────────┘   ties args to providers   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Core Entities

### CompletionContext

The input context passed to completion providers.

```csharp
/// <summary>
/// Provides context for completion providers to generate suggestions.
/// </summary>
public sealed class CompletionContext
{
    /// <summary>
    /// The current input buffer text up to the cursor position.
    /// </summary>
    public string InputText { get; init; } = string.Empty;
    
    /// <summary>
    /// The cursor position within the input buffer.
    /// </summary>
    public int CursorPosition { get; init; }
    
    /// <summary>
    /// The name of the command being completed, if known.
    /// </summary>
    public string? CommandName { get; init; }
    
    /// <summary>
    /// The name of the argument being completed, if applicable.
    /// </summary>
    public string? ArgumentName { get; init; }
    
    /// <summary>
    /// The partial value typed so far for the current token.
    /// </summary>
    public string PartialValue { get; init; } = string.Empty;
    
    /// <summary>
    /// The type of element being completed.
    /// </summary>
    public CompletionElementType ElementType { get; init; }
    
    /// <summary>
    /// The CLR type of the property being completed (for enum detection, etc.).
    /// </summary>
    public Type? PropertyType { get; init; }
    
    /// <summary>
    /// The [Completion] attribute on the property, if any.
    /// </summary>
    public CompletionAttribute? CompletionAttribute { get; init; }
    
    /// <summary>
    /// Whether the completion is for a remote command.
    /// </summary>
    public bool IsRemote { get; init; }
    
    /// <summary>
    /// Service provider for resolving dependencies within providers.
    /// </summary>
    public IServiceProvider Services { get; init; } = null!;
}

/// <summary>
/// The type of element being completed.
/// Mirrors <see cref="CommandElementType"/> from parsing subsystem.
/// </summary>
public enum CompletionElementType
{
    /// <summary>Empty input - suggest commands.</summary>
    Empty,
    
    /// <summary>Command or command group name.</summary>
    Command,
    
    /// <summary>Argument name (--name).</summary>
    ArgumentName,
    
    /// <summary>Argument alias (-n).</summary>
    ArgumentAlias,
    
    /// <summary>Argument value (named argument).</summary>
    ArgumentValue,
    
    /// <summary>Positional argument value.</summary>
    Positional
}
```

### CompletionItem

A single completion suggestion.

```csharp
/// <summary>
/// Represents a single completion suggestion.
/// </summary>
public sealed class CompletionItem
{
    /// <summary>
    /// The text to insert when this item is selected.
    /// </summary>
    public required string InsertText { get; init; }
    
    /// <summary>
    /// The display text shown in the completion menu.
    /// May differ from InsertText (e.g., display without quotes).
    /// </summary>
    public string DisplayText { get; init; } = string.Empty;
    
    /// <summary>
    /// Optional description shown to the right of the item.
    /// </summary>
    public string? Description { get; init; }
    
    /// <summary>
    /// The type of completion item (for icons/grouping).
    /// </summary>
    public CompletionItemKind Kind { get; init; }
    
    /// <summary>
    /// Sort priority (higher = appears first). Default 0.
    /// </summary>
    public int SortPriority { get; init; }
    
    /// <summary>
    /// Match score for ranking (set by matcher).
    /// </summary>
    public int MatchScore { get; internal set; }
    
    /// <summary>
    /// Ranges within DisplayText that matched the query (for highlighting).
    /// </summary>
    public IReadOnlyList<Range> MatchRanges { get; internal set; } = Array.Empty<Range>();
}

/// <summary>
/// The kind of completion item (for visual differentiation).
/// Aligns with <see cref="CommandElementType"/> terminology.
/// </summary>
public enum CompletionItemKind
{
    /// <summary>A command (e.g., "list", "connect").</summary>
    Command,
    
    /// <summary>A command group/namespace (e.g., "file", "config").</summary>
    CommandGroup,
    
    /// <summary>An argument name (--name, --output).</summary>
    ArgumentName,
    
    /// <summary>An argument alias (-n, -o).</summary>
    ArgumentAlias,
    
    /// <summary>A value for an argument.</summary>
    ArgumentValue,
    
    /// <summary>A file path.</summary>
    File,
    
    /// <summary>A directory path.</summary>
    Directory
}
```

### CompletionResult

The result returned by a completion provider.

```csharp
/// <summary>
/// The result of a completion request.
/// </summary>
public sealed class CompletionResult
{
    /// <summary>
    /// The completion items to display.
    /// </summary>
    public IReadOnlyList<CompletionItem> Items { get; init; } = Array.Empty<CompletionItem>();
    
    /// <summary>
    /// Whether the result was retrieved from cache.
    /// </summary>
    public bool IsCached { get; init; }
    
    /// <summary>
    /// Whether the request timed out (remote completions).
    /// </summary>
    public bool IsTimedOut { get; init; }
    
    /// <summary>
    /// Whether an error occurred fetching completions.
    /// </summary>
    public bool IsError { get; init; }
    
    /// <summary>
    /// Error message if IsError is true.
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// The total number of items (before truncation/viewport).
    /// </summary>
    public int TotalCount { get; init; }
    
    /// <summary>
    /// Empty result singleton.
    /// </summary>
    public static CompletionResult Empty { get; } = new() { Items = Array.Empty<CompletionItem>() };
    
    /// <summary>
    /// Timed out result singleton.
    /// </summary>
    public static CompletionResult TimedOut { get; } = new() { IsTimedOut = true };
}
```

---

## Provider Interface

### ICompletionProvider

The uniform interface for all completion providers (built-in and custom).

```csharp
/// <summary>
/// Provides completion suggestions for a specific context.
/// All completion providers (built-in and custom) implement this interface.
/// </summary>
public interface ICompletionProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher values execute first.
    /// </summary>
    int Priority => 0;
    
    /// <summary>
    /// Determines if this provider can handle the given context.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <returns>True if this provider should be used.</returns>
    bool CanHandle(CompletionContext context);
    
    /// <summary>
    /// Gets completion suggestions for the given context.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The completion result.</returns>
    Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken = default);
}
```

---

## Cache Entities

### CacheKey

Unique identifier for cached completion results.

```csharp
/// <summary>
/// Unique key for cached completion results.
/// </summary>
public readonly record struct CacheKey(
    string CommandName,
    string ArgumentName,
    string Prefix,
    bool IsRemote)
{
    /// <summary>
    /// Creates a cache key from a completion context.
    /// </summary>
    public static CacheKey FromContext(CompletionContext context) => new(
        context.CommandName ?? string.Empty,
        context.ArgumentName ?? string.Empty,
        context.PartialValue,
        context.IsRemote);
}
```

### CacheEntry

Internal cache entry with expiration.

```csharp
/// <summary>
/// Internal cache entry with TTL tracking.
/// </summary>
internal sealed class CacheEntry
{
    public CacheKey Key { get; init; }
    public CompletionResult Result { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }
    
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
```

---

## UI State Entities

### MenuState

Tracks the state of the completion menu.

```csharp
/// <summary>
/// Tracks the current state of the completion menu.
/// </summary>
public sealed class MenuState
{
    /// <summary>
    /// Whether the menu is currently visible.
    /// </summary>
    public bool IsOpen { get; set; }
    
    /// <summary>
    /// All items in the menu.
    /// </summary>
    public IReadOnlyList<CompletionItem> Items { get; set; } = Array.Empty<CompletionItem>();
    
    /// <summary>
    /// The currently selected item index (0-based).
    /// </summary>
    public int SelectedIndex { get; set; }
    
    /// <summary>
    /// The first visible item index (for scrolling).
    /// </summary>
    public int ViewportStart { get; set; }
    
    /// <summary>
    /// The number of visible rows in the viewport.
    /// </summary>
    public int ViewportSize { get; set; } = 10;
    
    /// <summary>
    /// The current filter text (typed while menu is open).
    /// </summary>
    public string FilterText { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the currently selected item, or null if none.
    /// </summary>
    public CompletionItem? SelectedItem => 
        SelectedIndex >= 0 && SelectedIndex < Items.Count 
            ? Items[SelectedIndex] 
            : null;
    
    /// <summary>
    /// Moves selection up, wrapping to bottom if at top.
    /// </summary>
    public void MoveUp()
    {
        if (Items.Count == 0) return;
        SelectedIndex = SelectedIndex == 0 ? Items.Count - 1 : SelectedIndex - 1;
        EnsureSelectedVisible();
    }
    
    /// <summary>
    /// Moves selection down, wrapping to top if at bottom.
    /// </summary>
    public void MoveDown()
    {
        if (Items.Count == 0) return;
        SelectedIndex = (SelectedIndex + 1) % Items.Count;
        EnsureSelectedVisible();
    }
    
    /// <summary>
    /// Ensures the selected item is within the visible viewport.
    /// </summary>
    private void EnsureSelectedVisible()
    {
        if (SelectedIndex < ViewportStart)
            ViewportStart = SelectedIndex;
        else if (SelectedIndex >= ViewportStart + ViewportSize)
            ViewportStart = SelectedIndex - ViewportSize + 1;
    }
}
```

### GhostState

Tracks ghost text suggestion state.

```csharp
/// <summary>
/// Tracks the ghost text suggestion state.
/// </summary>
public sealed class GhostState
{
    /// <summary>
    /// The ghost text to display after the cursor.
    /// </summary>
    public string? Text { get; set; }
    
    /// <summary>
    /// The source of the ghost suggestion.
    /// </summary>
    public GhostSource Source { get; set; }
    
    /// <summary>
    /// Whether there is an active ghost suggestion.
    /// </summary>
    public bool HasValue => !string.IsNullOrEmpty(Text);
    
    /// <summary>
    /// Clears the ghost text.
    /// </summary>
    public void Clear()
    {
        Text = null;
        Source = GhostSource.None;
    }
}

/// <summary>
/// The source of a ghost text suggestion.
/// </summary>
public enum GhostSource
{
    None,
    History,
    Command
}
```

---

## Match Entities

### MatchResult

Result of matching an item against a query.

```csharp
/// <summary>
/// Result of matching a completion item against a query.
/// </summary>
public sealed class MatchResult
{
    /// <summary>
    /// The matched text.
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// Match score (higher = better match).
    /// </summary>
    public int Score { get; init; }
    
    /// <summary>
    /// Ranges within the text that matched (for highlighting).
    /// </summary>
    public IReadOnlyList<Range> MatchRanges { get; init; } = Array.Empty<Range>();
    
    /// <summary>
    /// Whether this is a prefix match (query at start of text).
    /// </summary>
    public bool IsPrefixMatch { get; init; }
}

/// <summary>
/// Match mode for the completion matcher.
/// </summary>
public enum MatchMode
{
    /// <summary>Query must match start of text.</summary>
    Prefix,
    
    /// <summary>Query can appear anywhere in text.</summary>
    Contains,
    
    /// <summary>Characters must appear in order but not consecutively.</summary>
    Fuzzy
}
```

---

## Completion Attribute System

The unified `[Completion]` attribute provides a single concept for specifying completion sources.

### CompletionAttribute (Base)

```csharp
/// <summary>
/// Specifies the completion source for an argument.
/// This is the unified way to add autocomplete to any argument.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class CompletionAttribute : Attribute
{
    /// <summary>
    /// The provider type to use for completions.
    /// Resolved from DI container.
    /// </summary>
    public Type? ProviderType { get; }
    
    /// <summary>
    /// The name of a method on the command class that provides completions.
    /// Method signature: Task&lt;IEnumerable&lt;string&gt;&gt; MethodName(CompletionContext, CancellationToken)
    /// </summary>
    public string? MethodName { get; }
    
    /// <summary>
    /// Static values to complete.
    /// </summary>
    public string[]? Values { get; }
    
    /// <summary>
    /// Create completion from a provider type.
    /// </summary>
    /// <example>[Completion(typeof(FilePathProvider))]</example>
    public CompletionAttribute(Type providerType)
    {
        ProviderType = providerType;
    }
    
    /// <summary>
    /// Create completion from a method name on the command class.
    /// Single string = method name.
    /// </summary>
    /// <example>[Completion(nameof(GetEnvironments))]</example>
    public CompletionAttribute(string methodName)
    {
        MethodName = methodName;
    }
    
    /// <summary>
    /// Create completion from static values.
    /// Requires 2+ values to distinguish from method name.
    /// </summary>
    /// <example>[Completion("dev", "staging", "prod")]</example>
    public CompletionAttribute(string value1, string value2, params string[] moreValues)
    {
        Values = new[] { value1, value2 }.Concat(moreValues).ToArray();
    }
    
    /// <summary>
    /// Protected constructor for shortcut attributes.
    /// </summary>
    protected CompletionAttribute() { }
}
```

### Shortcut Attributes

Built-in shortcuts for common providers. These are just convenience wrappers.

```csharp
/// <summary>
/// Enables file path completion for an argument.
/// Shortcut for [Completion(typeof(FilePathProvider))].
/// All built-in shortcut attributes include "Completion" in their name.
/// </summary>
public class FilePathCompletionAttribute : CompletionAttribute
{
    public FilePathCompletionAttribute() { ProviderType = typeof(FilePathProvider); }
}

/// <summary>
/// Enables directory path completion for an argument.
/// Shortcut for [Completion(typeof(DirectoryPathProvider))].
/// </summary>
public class DirectoryPathCompletionAttribute : CompletionAttribute
{
    public DirectoryPathCompletionAttribute() { ProviderType = typeof(DirectoryPathProvider); }
}
```

### Usage Examples

```csharp
public class DeployCommand : CommandBase
{
    // Static values via attribute
    [Argument("environment")]
    [Completion("development", "staging", "production")]
    public string Environment { get; set; }
    
    // Method-based dynamic completion
    [Argument("version")]
    [Completion(nameof(GetVersions))]
    public string Version { get; set; }
    
    // Explicit provider type
    [Argument("config")]
    [Completion(typeof(ConfigFileProvider))]
    public string ConfigPath { get; set; }
    
    // Built-in shortcut (equivalent to [Completion(typeof(FilePathProvider))])
    [Argument("output")]
    [FilePathCompletion]
    public string OutputFile { get; set; }
    
    // Enum - automatic, no attribute needed!
    [Argument("format")]
    public OutputFormat Format { get; set; }
    
    // Method for dynamic completion (DI-injected parameters supported)
    public static async Task<IEnumerable<string>> GetVersions(
        CompletionContext context,
        IVersionService versionService,  // Injected from DI
        CancellationToken cancellationToken)
    {
        return await versionService.GetAvailableVersionsAsync(
            context.PartialValue, 
            cancellationToken);
    }
    
    public void Execute(CommandExecutionContext ctx) { /* ... */ }
}
```

---

## Relationships Summary

| Entity | References | Referenced By |
|--------|------------|---------------|
| `CompletionContext` | `IServiceProvider`, `CompletionAttribute`, `Type` | `ICompletionProvider`, `CacheKey` |
| `CompletionItem` | - | `CompletionResult`, `MenuState` |
| `CompletionResult` | `CompletionItem` | `ICompletionProvider`, `CompletionCache` |
| `ICompletionProvider` | `CompletionContext`, `CompletionResult` | `CompletionOrchestrator` |
| `CompletionAttribute` | `Type`, `string`, `string[]` | `CompletionContext`, Commands |
| `FilePathCompletionAttribute` | `FilePathProvider` | Commands (shortcut) |
| `DirectoryPathCompletionAttribute` | `DirectoryPathProvider` | Commands (shortcut) |
| `CacheKey` | - | `CompletionCache`, `CacheEntry` |
| `CacheEntry` | `CacheKey`, `CompletionResult` | `CompletionCache` |
