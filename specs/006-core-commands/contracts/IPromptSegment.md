# Contract: IPromptSegment

**Package**: `BitPantry.CommandLine`  
**Namespace**: `BitPantry.CommandLine.Input`

## Interface Definition

```csharp
namespace BitPantry.CommandLine.Input;

/// <summary>
/// Contributes a segment to the composite prompt.
/// Implementations are resolved from DI and rendered in Order sequence.
/// </summary>
public interface IPromptSegment
{
    /// <summary>
    /// Order in which this segment appears. Lower values appear first.
    /// </summary>
    /// <remarks>
    /// Convention:
    /// <list type="bullet">
    ///   <item>0-99: Core (application name, base state)</item>
    ///   <item>100-199: Connection state (server, profile)</item>
    ///   <item>200-299: Session state (future extensions)</item>
    ///   <item>300+: Custom/user segments</item>
    /// </list>
    /// </remarks>
    int Order { get; }
    
    /// <summary>
    /// Renders this segment's content.
    /// </summary>
    /// <returns>
    /// The segment text including any decorators (e.g., "@", "[]"),
    /// or null to skip this segment entirely.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Return null to hide the segment (e.g., when disconnected)</item>
    ///   <item>Return empty string to reserve space with no content</item>
    ///   <item>Include decorators in return value (segment owns its formatting)</item>
    ///   <item>Do not include trailing space (CompositePrompt adds separators)</item>
    /// </list>
    /// </remarks>
    string? Render();
}
```

## Built-in Implementations

### AppNameSegment (Order: 0)

```csharp
public class AppNameSegment : IPromptSegment
{
    public int Order => 0;
    public string? Render() => _appName; // e.g., "myapp"
}
```

### ServerConnectionSegment (Order: 100)

```csharp
public class ServerConnectionSegment : IPromptSegment
{
    public int Order => 100;
    public string? Render()
    {
        if (!connected) return null;
        return $"@{hostname}"; // e.g., "@api.example.com"
    }
}
```

### ProfileSegment (Order: 110)

```csharp
public class ProfileSegment : IPromptSegment
{
    public int Order => 110;
    public string? Render()
    {
        if (!connected || !hasProfile) return null;
        return $"[{profileName}]"; // e.g., "[prod]"
    }
}
```

## Registration

```csharp
// Core library registers default segments
services.AddSingleton<IPromptSegment, AppNameSegment>();

// SignalR Client registers its segments
services.AddSingleton<IPromptSegment, ServerConnectionSegment>();
services.AddSingleton<IPromptSegment, ProfileSegment>();

// User code can add custom segments
services.AddSingleton<IPromptSegment, MyCustomSegment>();
```

## Usage Example

```csharp
public class GitBranchSegment : IPromptSegment
{
    private readonly IGitService _git;
    
    public int Order => 50;
    
    public GitBranchSegment(IGitService git)
    {
        _git = git;
    }
    
    public string? Render()
    {
        var branch = _git.GetCurrentBranch();
        return branch != null ? $"({branch})" : null;
    }
}
```

## Error Handling

If a segment throws an exception during `Render()`:
- The exception is caught and logged
- The segment is skipped
- Other segments continue rendering
- No user-visible error (degraded UX only)
