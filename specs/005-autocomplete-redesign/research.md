# Research: Autocomplete Redesign

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

## 1. Terminal Rendering for Completion Menu

### Challenge
Render a multi-line completion menu below the input line without disrupting terminal buffer state or causing scrolling artifacts.

### Approach: ANSI Escape Sequences with Save/Restore

```csharp
// Save cursor position, render menu below, restore cursor
// ESC[s = save cursor position
// ESC[u = restore cursor position  
// ESC[?25l = hide cursor during render
// ESC[?25h = show cursor after render
// ESC[K = clear to end of line
// ESC[nB = move cursor down n lines
// ESC[nA = move cursor up n lines
```

### Spectre.Console Approach
Spectre.Console provides `IAnsiConsole` which handles terminal capabilities. Use:
- `IAnsiConsole.Write(IRenderable)` for styled output
- `AnsiConsole.Cursor` for position management
- `Markup` class for styled text with color codes

### Menu Rendering Strategy
1. Calculate viewport height (min 10 rows or available terminal rows)
2. Save cursor position
3. Move down one line, clear from cursor down
4. Render visible items (10-row viewport)
5. If more items exist, show "▼ +N more" indicator
6. Restore cursor position to input line

### Considerations
- **Windows Terminal**: Full ANSI support ✓
- **Windows Console (legacy)**: Limited support, use `Console.SetCursorPosition()` fallback
- **Linux/macOS**: Full ANSI support ✓
- **SSH/Remote**: Depends on client terminal

---

## 2. Ghost Text Rendering

### ANSI Color Codes for Muted Text

```csharp
// Dim/muted gray text (widely supported)
public const string MutedStart = "\x1b[90m";  // Bright black (gray)
public const string MutedEnd = "\x1b[0m";     // Reset

// Alternative using dim attribute
public const string DimStart = "\x1b[2m";     // Dim
public const string DimEnd = "\x1b[22m";      // Normal intensity
```

### Ghost Text Implementation
1. Render input text normally
2. After input text, render ghost text in muted color
3. Ghost text appears inline without affecting cursor position
4. Use `ESC[s` (save) and `ESC[u` (restore) to position cursor after ghost

### Acceptance Behavior
- **Right Arrow**: Accept ghost suggestion, append to input buffer
- **Any other key**: Clear ghost, process key normally

### Spectre.Console Integration
```csharp
// Use Style with Decoration.Dim
var ghostStyle = new Style(Color.Grey, decoration: Decoration.Dim);
```

---

## 3. Debounce Pattern in C#

### Pattern: CancellationTokenSource Reset

```csharp
public class Debouncer
{
    private CancellationTokenSource? _cts;
    private readonly TimeSpan _delay;
    
    public Debouncer(TimeSpan delay) => _delay = delay;
    
    public async Task DebounceAsync(Func<CancellationToken, Task> action)
    {
        // Cancel previous pending operation
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        
        try
        {
            await Task.Delay(_delay, token);
            await action(token);
        }
        catch (OperationCanceledException)
        {
            // Expected when debounced - ignore
        }
    }
}
```

### Usage in Autocomplete
- **Delay**: 150ms for remote, 0ms for local (per spec)
- **Trigger**: On each keystroke while typing argument values
- **Cancellation**: Escape key or Tab key (to select) cancels pending fetch

---

## 4. File System Completion Edge Cases

### Handling with System.IO.Abstractions

```csharp
public class FilePathProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;
    
    public FilePathProvider(IFileSystem fileSystem) => _fileSystem = fileSystem;
    
    public async Task<CompletionResult> GetCompletionsAsync(CompletionContext ctx, CancellationToken ct)
    {
        var path = ctx.PartialValue;
        var directory = _fileSystem.Path.GetDirectoryName(path) ?? ".";
        var prefix = _fileSystem.Path.GetFileName(path);
        
        try
        {
            var entries = _fileSystem.Directory.EnumerateFileSystemEntries(directory, prefix + "*");
            // ... build completion items
        }
        catch (UnauthorizedAccessException)
        {
            return CompletionResult.Empty; // Silent fail on permission errors
        }
        catch (DirectoryNotFoundException)
        {
            return CompletionResult.Empty; // Path doesn't exist yet
        }
    }
}
```

### Edge Cases to Handle
| Case | Behavior |
|------|----------|
| Hidden files (`.dotfiles`) | Include by default, filter with option |
| Symlinks | Resolve target, show as regular file/dir |
| Permission denied | Return empty, no error shown |
| Network paths (`\\server\share`) | Support on Windows, timeout at 3s |
| Very long paths (>260 chars) | Use extended path prefix on Windows |
| Spaces in paths | Quote the completed path |
| Case sensitivity | Follow OS conventions (Windows: insensitive, Linux: sensitive) |

### Path Normalization
```csharp
// Normalize separators based on OS
var normalizedPath = path.Replace(
    _fileSystem.Path.AltDirectorySeparatorChar, 
    _fileSystem.Path.DirectorySeparatorChar);
```

---

## 5. InputController Integration Points

### Current Architecture (from codebase analysis)

```
InputController
  └── ProcessKeyAsync(ConsoleKeyInfo key)
        └── Case: Tab
              └── AutoCompleteController.HandleAutoComplete()
                    └── AutoCompleteOptionSetBuilder.Build()
```

### Integration Strategy

Replace `AutoCompleteController.HandleAutoComplete()` call with new orchestrator:

```csharp
// In InputController.ProcessKeyAsync
case ConsoleKey.Tab:
    await _completionOrchestrator.HandleTabAsync(_inputBuffer, cancellationToken);
    break;
    
case ConsoleKey.RightArrow when _ghostText.HasValue:
    _inputBuffer.Append(_ghostText.Value);
    _ghostText.Clear();
    break;
    
// On any keystroke, update ghost suggestions
case var _ when char.IsLetterOrDigit(keyInfo.KeyChar):
    _inputBuffer.Append(keyInfo.KeyChar);
    await _ghostRenderer.UpdateAsync(_inputBuffer.ToString(), cancellationToken);
    break;
```

### Key Handler Modifications
| Key | Current Behavior | New Behavior |
|-----|-----------------|--------------|
| Tab | Build options, show/cycle | Open completion menu OR accept ghost |
| Shift+Tab | N/A | Navigate up in menu |
| Right Arrow | Move cursor | Accept ghost suggestion if present |
| Escape | Cancel input | Close menu, clear ghost |
| Up/Down | N/A | Navigate menu items |
| Enter (in menu) | N/A | Accept selected item |
| Typing (in menu) | N/A | Filter menu items |

---

## 6. Remote Completion via SignalR

### Current RPC Pattern (from SignalRServerProxy)

```csharp
// Existing pattern for RPC calls
public async Task<T> InvokeAsync<T>(string methodName, params object[] args)
{
    using var scope = _rpcScope.BeginScope();
    return await _connection.InvokeAsync<T>(methodName, args);
}
```

### New Completion RPC Method

```csharp
// Server-side (CommandLineHub)
public async Task<CompletionResult> GetCompletionsAsync(
    string commandName, 
    string argumentName, 
    string partialValue,
    CancellationToken cancellationToken)
{
    var provider = _providerRegistry.GetProvider(commandName, argumentName);
    return await provider.GetCompletionsAsync(context, cancellationToken);
}

// Client-side (SignalRServerProxy extension)
public async Task<CompletionResult> GetRemoteCompletionsAsync(
    CompletionContext context,
    CancellationToken cancellationToken)
{
    using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    using var linked = CancellationTokenSource.CreateLinkedTokenSource(
        cancellationToken, timeout.Token);
    
    try
    {
        return await _connection.InvokeAsync<CompletionResult>(
            "GetCompletionsAsync",
            context.CommandName,
            context.ArgumentName,
            context.PartialValue,
            linked.Token);
    }
    catch (OperationCanceledException)
    {
        return CompletionResult.TimedOut;
    }
}
```

### Network Failure Handling (per spec: no retry)
- Timeout after 3 seconds
- Return empty result with `IsTimedOut = true` flag
- UI shows "Request timed out - press Tab to retry" message
- User presses Tab again to re-attempt

---

## 7. Caching Strategy

### Cache Key Design

```csharp
public readonly record struct CacheKey(
    string CommandName,
    string ArgumentName, 
    string Prefix,
    bool IsRemote);
```

### LRU Cache Implementation

```csharp
public class CompletionCache
{
    private readonly LinkedList<CacheEntry> _order = new();
    private readonly Dictionary<CacheKey, LinkedListNode<CacheEntry>> _lookup = new();
    private readonly int _maxItems = 100;
    private readonly TimeSpan _ttl = TimeSpan.FromMinutes(5);
    
    public bool TryGet(CacheKey key, out CompletionResult result)
    {
        if (_lookup.TryGetValue(key, out var node) && 
            node.Value.ExpiresAt > DateTime.UtcNow)
        {
            // Move to front (most recently used)
            _order.Remove(node);
            _order.AddFirst(node);
            result = node.Value.Result;
            return true;
        }
        result = default;
        return false;
    }
    
    public void Set(CacheKey key, CompletionResult result)
    {
        // Evict oldest if at capacity
        while (_order.Count >= _maxItems)
        {
            var oldest = _order.Last!;
            _lookup.Remove(oldest.Value.Key);
            _order.RemoveLast();
        }
        
        var entry = new CacheEntry(key, result, DateTime.UtcNow.Add(_ttl));
        var node = _order.AddFirst(entry);
        _lookup[key] = node;
    }
}
```

---

## 8. Match Highlighting

### Algorithm

```csharp
public class MatchResult
{
    public string Text { get; init; }
    public int Score { get; init; }
    public IReadOnlyList<Range> MatchRanges { get; init; }
    
    public static MatchResult? TryMatch(string text, string query, MatchMode mode)
    {
        return mode switch
        {
            MatchMode.Prefix => TryPrefixMatch(text, query),
            MatchMode.Contains => TryContainsMatch(text, query),
            MatchMode.Fuzzy => TryFuzzyMatch(text, query),
            _ => null
        };
    }
    
    private static MatchResult? TryPrefixMatch(string text, string query)
    {
        if (!text.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            return null;
            
        return new MatchResult
        {
            Text = text,
            Score = 100, // Prefix matches score highest
            MatchRanges = new[] { new Range(0, query.Length) }
        };
    }
}
```

### Rendering with Highlights

```csharp
public string RenderWithHighlights(MatchResult match)
{
    var sb = new StringBuilder();
    int pos = 0;
    
    foreach (var range in match.MatchRanges)
    {
        // Non-matching prefix
        if (range.Start.Value > pos)
            sb.Append(match.Text[pos..range.Start.Value]);
        
        // Matching portion (bold)
        sb.Append("\x1b[1m"); // Bold on
        sb.Append(match.Text[range]);
        sb.Append("\x1b[22m"); // Bold off
        
        pos = range.End.Value;
    }
    
    // Remaining suffix
    if (pos < match.Text.Length)
        sb.Append(match.Text[pos..]);
    
    return sb.ToString();
}
```

---

## Summary of Key Decisions

| Area | Decision | Rationale |
|------|----------|-----------|
| Menu rendering | ANSI sequences with save/restore cursor | Works across all modern terminals |
| Ghost color | `\x1b[90m` (bright black/gray) | Widely supported, clearly distinguishes from input |
| Debounce | CancellationTokenSource pattern | Native C# async, no external dependencies |
| File system | System.IO.Abstractions | Enables clean unit testing with MockFileSystem |
| Remote timeout | 3s hardcoded, no retry | Matches spec, retry via user Tab press |
| Cache | LRU with 100 items, 5min TTL | Balances memory with responsiveness |
| Match highlighting | ANSI bold sequences | Standard escape codes, Spectre.Console compatible |
