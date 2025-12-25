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
/// Providers are resolved via dependency injection. When an argument has a
/// <see cref="CompletionAttribute"/>, the system resolves the appropriate provider:
/// </para>
/// <para>
/// <list type="bullet">
///   <item><see cref="StaticValuesProvider"/> - Values from [Completion("a", "b", "c")]</item>
///   <item><see cref="MethodProvider"/> - Method from [Completion(nameof(GetValues))]</item>
///   <item>Custom providers - Type from [Completion(typeof(MyProvider))]</item>
/// </list>
/// </para>
/// <para>
/// For system-level completion (commands, arguments), built-in providers:
/// <list type="bullet">
///   <item><see cref="CommandCompletionProvider"/> - Commands and command groups</item>
///   <item><see cref="ArgumentNameProvider"/> - Argument names (--name)</item>
///   <item><see cref="ArgumentAliasProvider"/> - Argument aliases (-a)</item>
///   <item><see cref="EnumProvider"/> - Enum-typed argument values (automatic)</item>
/// </list>
/// </para>
/// <para>
/// Built-in value providers used via shortcut attributes:
/// <list type="bullet">
///   <item><see cref="FilePathProvider"/> - [FilePath] attribute</item>
///   <item><see cref="DirectoryPathProvider"/> - [DirectoryPath] attribute</item>
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

### File Path Provider (used by [FilePath] shortcut attribute)

```csharp
public class FilePathProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;
    
    public FilePathProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public bool CanHandle(CompletionContext context)
    {
        // This provider is explicitly specified via [FilePath] attribute
        // which inherits from [Completion(typeof(FilePathProvider))]
        return context.CompletionAttribute?.ProviderType == typeof(FilePathProvider);
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

### Custom Provider (used via [Completion(typeof(...))])

```csharp
/// <summary>
/// Custom provider for environment completion.
/// Usage: [Completion(typeof(EnvironmentProvider))]
/// Or create a shortcut: [Environment] : CompletionAttribute
/// </summary>
public class EnvironmentProvider : ICompletionProvider
{
    private readonly IDeploymentService _deploymentService;
    
    // DI constructor - services injected automatically
    public EnvironmentProvider(IDeploymentService deploymentService)
    {
        _deploymentService = deploymentService;
    }
    
    public bool CanHandle(CompletionContext context)
    {
        return context.CompletionAttribute?.ProviderType == typeof(EnvironmentProvider);
    }
    
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        // Dynamic values - could be from database, API, config file, etc.
        var envs = await _deploymentService.GetEnvironmentsAsync(cancellationToken);
        var prefix = context.PartialValue;
        
        var items = envs
            .Where(e => e.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(e => new CompletionItem
            {
                InsertText = e.Name,
                DisplayText = e.Name,
                Description = e.Description,
                Kind = CompletionItemKind.ArgumentValue
            })
            .ToList();
        
        return new CompletionResult { Items = items };
    }
}
```

### Enum Provider (automatic for enum properties)

```csharp
/// <summary>
/// Automatically handles enum-typed arguments.
/// No attribute needed - the system detects enum types automatically.
/// </summary>
internal class EnumProvider : ICompletionProvider
{
    public bool CanHandle(CompletionContext context)
    {
        // PropertyType comes from the argument's declared type
        return context.PropertyType?.IsEnum == true &&
               context.CompletionAttribute == null; // No explicit provider specified
    }
    
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var enumType = context.PropertyType!;
        var prefix = context.PartialValue;
        
        var items = Enum.GetNames(enumType)
            .Where(n => n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(n => new CompletionItem
            {
                InsertText = n,
                DisplayText = n,
                Kind = CompletionItemKind.EnumValue
            })
            .ToList();
        
        return Task.FromResult(new CompletionResult { Items = items });
    }
}
```

## Registration

Providers are registered via dependency injection:

```csharp
// Built-in system providers (commands, arguments)
services.AddSingleton<ICompletionProvider, CommandCompletionProvider>();
services.AddSingleton<ICompletionProvider, ArgumentNameProvider>();
services.AddSingleton<ICompletionProvider, ArgumentAliasProvider>();

// Built-in value providers (used by shortcut attributes)
services.AddSingleton<ICompletionProvider, FilePathProvider>();
services.AddSingleton<ICompletionProvider, DirectoryPathProvider>();
services.AddSingleton<ICompletionProvider, EnumProvider>();

// Internal providers (used by [Completion] attribute)
services.AddSingleton<ICompletionProvider, StaticValuesProvider>();
services.AddSingleton<ICompletionProvider, MethodProvider>();

// Custom providers (added by application code)
services.AddSingleton<ICompletionProvider, EnvironmentProvider>();
```
