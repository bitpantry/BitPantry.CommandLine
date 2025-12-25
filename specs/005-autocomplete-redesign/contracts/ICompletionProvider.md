# ICompletionProvider Contract

**Location**: `BitPantry.CommandLine/AutoComplete/Providers/ICompletionProvider.cs`

## Interface Definition

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers;

/// <summary>
/// Provides completion suggestions for a specific context.
/// All completion providers (built-in and custom) implement this interface.
/// </summary>
/// <remarks>
/// <para>
/// Providers are resolved via dependency injection and queried in priority order.
/// The first provider where <see cref="CanHandle"/> returns true will be used.
/// </para>
/// <para>
/// Built-in providers include:
/// <list type="bullet">
///   <item><see cref="CommandCompletionProvider"/> - Commands and command groups</item>
///   <item><see cref="ArgumentNameProvider"/> - Argument names (--name)</item>
///   <item><see cref="ArgumentAliasProvider"/> - Argument aliases (-a)</item>
///   <item><see cref="FilePathProvider"/> - File path completion ([FilePath] attribute)</item>
///   <item><see cref="DirectoryPathProvider"/> - Directory path completion ([DirectoryPath] attribute)</item>
///   <item><see cref="EnumValueProvider"/> - Enum-typed argument values (automatic)</item>
///   <item><see cref="CompletionValuesProvider"/> - Static values ([CompletionValues] attribute)</item>
/// </list>
/// </para>
/// </remarks>
public interface ICompletionProvider
{
    /// <summary>
    /// Gets the priority of this provider. Higher values execute first.
    /// </summary>
    /// <remarks>
    /// Default is 0. Built-in providers use priorities:
    /// <list type="bullet">
    ///   <item>Custom providers: 50+</item>
    ///   <item>Attribute-based (File/Directory/Values): 20</item>
    ///   <item>Enum: 10</item>
    ///   <item>Default (Command/Argument): 0</item>
    /// </list>
    /// </remarks>
    int Priority => 0;
    
    /// <summary>
    /// Determines if this provider can handle the given context.
    /// </summary>
    /// <param name="context">The completion context.</param>
    /// <returns>True if this provider should be used for completions.</returns>
    /// <remarks>
    /// Providers are queried in priority order. The first provider that returns
    /// true from this method will have its <see cref="GetCompletionsAsync"/>
    /// method called. Multiple providers can contribute to results if needed.
    /// </remarks>
    bool CanHandle(CompletionContext context);
    
    /// <summary>
    /// Gets completion suggestions for the given context.
    /// </summary>
    /// <param name="context">The completion context containing input state.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The completion result containing suggestions.</returns>
    /// <remarks>
    /// <para>
    /// Implementations should:
    /// <list type="bullet">
    ///   <item>Return <see cref="CompletionResult.Empty"/> if no suggestions available</item>
    ///   <item>Honor the cancellation token, especially for I/O operations</item>
    ///   <item>Not throw exceptions - return error result instead</item>
    ///   <item>Complete within reasonable time (local: &lt;50ms, remote: &lt;3s)</item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken = default);
}
```

## Implementation Requirements

### Must Implement
- `CanHandle(CompletionContext)` - Return true only for contexts this provider handles
- `GetCompletionsAsync(...)` - Return completions or `CompletionResult.Empty`

### Should Implement
- `Priority` property override if non-default priority needed

### Must Not
- Throw exceptions from `GetCompletionsAsync` - return error result instead
- Block indefinitely - honor cancellation token
- Hold references to context after method returns

## Usage Examples

### File Path Provider

```csharp
public class FilePathProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;
    
    public int Priority => 10;
    
    public FilePathProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public bool CanHandle(CompletionContext context)
    {
        // Handle when argument is decorated with [FilePath] or explicit provider
        return context.ElementType == CompletionElementType.ArgumentValue &&
               context.HasProviderHint<FilePathProvider>();
    }
    
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var items = new List<CompletionItem>();
        var prefix = context.PartialValue;
        var dir = _fileSystem.Path.GetDirectoryName(prefix) ?? ".";
        var pattern = _fileSystem.Path.GetFileName(prefix) + "*";
        
        try
        {
            foreach (var path in _fileSystem.Directory.EnumerateFiles(dir, pattern))
            {
                cancellationToken.ThrowIfCancellationRequested();
                items.Add(new CompletionItem
                {
                    InsertText = path,
                    DisplayText = _fileSystem.Path.GetFileName(path),
                    Kind = CompletionItemKind.File
                });
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Silent fail on permission errors
        }
        catch (DirectoryNotFoundException)
        {
            // Directory doesn't exist yet
        }
        
        return new CompletionResult { Items = items };
    }
}
```

### Custom Value Provider

```csharp
public class EnvironmentProvider : ICompletionProvider
{
    public int Priority => 50;
    
    public bool CanHandle(CompletionContext context)
    {
        return context.ArgumentName == "environment";
    }
    
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var envs = new[] { "development", "staging", "production" };
        var prefix = context.PartialValue;
        
        var items = envs
            .Where(e => e.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(e => new CompletionItem
            {
                InsertText = e,
                DisplayText = e,
                Description = $"Deploy to {e}",
                Kind = CompletionItemKind.ArgumentValue
            })
            .ToList();
        
        return Task.FromResult(new CompletionResult { Items = items });
    }
}
```

## Registration

Providers are registered via dependency injection:

```csharp
services.AddSingleton<ICompletionProvider, CommandCompletionProvider>();
services.AddSingleton<ICompletionProvider, FilePathProvider>();
services.AddSingleton<ICompletionProvider, DirectoryPathProvider>();
services.AddSingleton<ICompletionProvider, HistoryProvider>();
services.AddSingleton<ICompletionProvider, LegacyFunctionProvider>();

// Custom provider
services.AddSingleton<ICompletionProvider, EnvironmentProvider>();
```
