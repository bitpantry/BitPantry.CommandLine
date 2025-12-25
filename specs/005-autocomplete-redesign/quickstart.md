# Quickstart: Autocomplete Implementation

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Data Model**: [data-model.md](data-model.md)

## Overview

Autocomplete uses **one unified concept**: the `[Completion]` attribute. This attribute tells the system where to get completion values for an argument.

All completion sources—built-in and custom—work the same way. Built-in providers are just providers we ship.

---

## The `[Completion]` Attribute

Three ways to specify completion source:

```csharp
// 1. Method on command class (single string = method name)
[Completion(nameof(GetEnvironments))]

// 2. Static values (requires 2+ values)
[Completion("dev", "staging", "prod")]

// 3. Provider type (reusable across commands)
[Completion(typeof(FilePathProvider))]
```

---

## Quick Examples

### Static Values

```csharp
public class DeployCommand : CommandBase
{
    [Argument("environment")]
    [Alias('e')]
    [Completion("development", "staging", "production")]
    public string Environment { get; set; }
    
    public void Execute(CommandExecutionContext ctx) { /* ... */ }
}
```

### Dynamic Values via Method

```csharp
public class DeployCommand : CommandBase
{
    [Argument("version")]
    [Alias('v')]
    [Completion(nameof(GetVersions))]
    public string Version { get; set; }
    
    // Method receives DI-injected services
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

### Enum Arguments (Automatic)

```csharp
public enum OutputFormat { Json, Xml, Csv, Text }

public class ExportCommand : CommandBase
{
    [Argument("format")]
    [Alias('f')]
    public OutputFormat Format { get; set; }  // Auto-completes enum values!
    
    public void Execute(CommandExecutionContext ctx) { /* ... */ }
}
```

No attribute needed—the system detects enum types and provides completion automatically.

---

## Built-in Providers

| Provider | Use Case | Shortcut Attribute |
|----------|----------|-------------------|
| `FilePathProvider` | Complete file paths | `[FilePathCompletion]` |
| `DirectoryPathProvider` | Complete directory paths | `[DirectoryPathCompletion]` |
| `EnumProvider` | Complete enum values | (automatic) |
| `StaticValuesProvider` | Static string list (2+) | `[Completion("a","b")]` |
| `MethodProvider` | Dynamic via method | `[Completion(nameof(X))]` |
| `CommandCompletionProvider` | Commands and groups | (automatic) |
| `ArgumentNameProvider` | --argument names | (automatic) |
| `ArgumentAliasProvider` | -a aliases | (automatic) |

### Shortcut Attributes

For ergonomics, common providers have shortcut attributes (all include "Completion" in their name):

```csharp
// These are equivalent:
[FilePathCompletion]
[Completion(typeof(FilePathProvider))]

// These are equivalent:
[DirectoryPathCompletion]
[Completion(typeof(DirectoryPathProvider))]
```

---

## File and Directory Completion

### File Path Completion

```csharp
public class ReadFileCommand : CommandBase
{
    [Argument("path")]
    [Alias('p')]
    [FilePathCompletion]  // Shortcut for [Completion(typeof(FilePathProvider))]
    public string FilePath { get; set; }
    
    public void Execute(CommandExecutionContext ctx)
    {
        var content = File.ReadAllText(FilePath);
        ctx.Console.WriteLine(content);
    }
}
```

### Directory Path Completion

```csharp
public class ListFilesCommand : CommandBase
{
    [Argument("directory")]
    [Alias('d')]
    [DirectoryPathCompletion]  // Shortcut for [Completion(typeof(DirectoryPathProvider))]
    public string Directory { get; set; }
    
    public void Execute(CommandExecutionContext ctx)
    {
        foreach (var file in System.IO.Directory.GetFiles(Directory))
            ctx.Console.WriteLine(file);
    }
}
```

---

## Creating a Custom Provider (Reusable)

For completion logic used across multiple commands, create a provider:

### Step 1: Implement ICompletionProvider

```csharp
using BitPantry.CommandLine.AutoComplete.Providers;

public class ConfigFileProvider : ICompletionProvider
{
    private readonly IFileSystem _fileSystem;
    
    // DI-injected dependencies
    public ConfigFileProvider(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    public int Priority => 50;
    
    // Handle any argument with our [Completion] attribute pointing here
    public bool CanHandle(CompletionContext context)
    {
        return context.CompletionAttribute?.ProviderType == typeof(ConfigFileProvider);
    }
    
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        // Find .json and .yaml files in current directory
        var files = _fileSystem.Directory
            .EnumerateFiles(".", "*.json")
            .Concat(_fileSystem.Directory.EnumerateFiles(".", "*.yaml"))
            .Where(f => f.Contains(context.PartialValue, StringComparison.OrdinalIgnoreCase))
            .Select(f => new CompletionItem
            {
                InsertText = f,
                DisplayText = Path.GetFileName(f),
                Description = "Config file",
                Kind = CompletionItemKind.File
            })
            .ToList();
        
        return Task.FromResult(new CompletionResult { Items = files });
    }
}
```

### Step 2: Register the Provider

```csharp
// In your DI setup / CommandLineApplicationBuilder
services.AddSingleton<ICompletionProvider, ConfigFileProvider>();
```

### Step 3: Use in Commands

```csharp
public class LoadConfigCommand : CommandBase
{
    [Argument("config")]
    [Alias('c')]
    [Completion(typeof(ConfigFileProvider))]  // Point to your provider
    public string ConfigPath { get; set; }
    
    public void Execute(CommandExecutionContext ctx) { /* ... */ }
}

public class ValidateConfigCommand : CommandBase
{
    [Argument("file")]
    [Completion(typeof(ConfigFileProvider))]  // Reuse same provider!
    public string FilePath { get; set; }
    
    public void Execute(CommandExecutionContext ctx) { /* ... */ }
}
```

---

## Creating Your Own Shortcut Attribute

If you use a provider frequently, create a shortcut attribute.
By convention, include "Completion" in the attribute name:

```csharp
/// <summary>
/// Enables config file completion (.json, .yaml) for an argument.
/// Shortcut for [Completion(typeof(ConfigFileProvider))].
/// </summary>
public class ConfigFileCompletionAttribute : CompletionAttribute
{
    public ConfigFileCompletionAttribute() { ProviderType = typeof(ConfigFileProvider); }
}

// Now use it just like [FilePathCompletion]:
public class LoadConfigCommand : CommandBase
{
    [Argument("config")]
    [ConfigFileCompletion]  // Your custom shortcut!
    public string ConfigPath { get; set; }
}
```

---

## Async/Remote Providers

For providers that fetch data from external sources:

```csharp
public class UserProvider : ICompletionProvider
{
    private readonly IUserService _userService;
    
    public UserProvider(IUserService userService)
    {
        _userService = userService;
    }
    
    public int Priority => 50;
    
    public bool CanHandle(CompletionContext context)
    {
        return context.CompletionAttribute?.ProviderType == typeof(UserProvider);
    }
    
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            var users = await _userService.SearchUsersAsync(
                context.PartialValue, 
                cancellationToken);
            
            var items = users.Select(u => new CompletionItem
            {
                InsertText = u.Username,
                DisplayText = u.Username,
                Description = u.FullName,
                Kind = CompletionItemKind.ArgumentValue
            }).ToList();
            
            return new CompletionResult { Items = items };
        }
        catch (OperationCanceledException)
        {
            return CompletionResult.Empty;
        }
        catch (Exception ex)
        {
            return new CompletionResult 
            { 
                IsError = true, 
                ErrorMessage = ex.Message 
            };
        }
    }
}
```

---

## Provider Priority Guide

| Priority Range | Use Case |
|---------------|----------|
| 50-99 | Custom business logic providers |
| 10-49 | Built-in providers (file, directory, enum) |
| 0-9 | Default fallback providers (command, argument) |

---

## Static Completion Values

For simple static lists (2+ values), use the `[Completion]` attribute with strings:

```csharp
public class DeployCommand : CommandBase
{
    [Argument("environment")]
    [Alias('e')]
    [Completion("development", "staging", "production")]  // 2+ values = static
    public string Environment { get; set; }
    
    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Deploying to {Environment}");
    }
}
```

This approach is ideal for:
- Fixed sets of values that don't change
- Simple enumerations without needing a full provider
- Quick prototyping

---

## Enum Arguments (Automatic)

Enum-typed arguments get completion automatically:

```csharp
public enum OutputFormat { Json, Xml, Csv, Text }

public class ExportCommand : CommandBase
{
    [Argument("format")]
    [Alias('f')]
    public OutputFormat Format { get; set; }  // Auto-completes enum values
    
    public void Execute(CommandExecutionContext ctx)
    {
        ctx.Console.WriteLine($"Exporting as {Format}");
    }
}
```

The `EnumProvider` automatically detects enum-typed arguments and provides completions.

---

## Testing Providers

```csharp
[TestClass]
public class EnvironmentProviderTests
{
    [TestMethod]
    public async Task GetCompletions_WithPrefix_FiltersResults()
    {
        // Arrange
        var provider = new EnvironmentProvider();
        var context = new CompletionContext
        {
            ArgumentName = "environment",
            PartialValue = "dev",
            ElementType = CompletionElementType.ArgumentValue
        };
        
        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);
        
        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].InsertText.Should().Be("development");
    }
    
    [TestMethod]
    public async Task GetCompletions_WithEmptyPrefix_ReturnsAll()
    {
        // Arrange
        var provider = new EnvironmentProvider();
        var context = new CompletionContext
        {
            ArgumentName = "environment",
            PartialValue = "",
            ElementType = CompletionElementType.ArgumentValue
        };
        
        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);
        
        // Assert
        result.Items.Should().HaveCount(3);
    }
    
    [TestMethod]
    public void CanHandle_WrongArgument_ReturnsFalse()
    {
        // Arrange
        var provider = new EnvironmentProvider();
        var context = new CompletionContext
        {
            ArgumentName = "other",
            ElementType = CompletionElementType.ArgumentValue
        };
        
        // Act & Assert
        provider.CanHandle(context).Should().BeFalse();
    }
}
```

---

## File System Provider Testing

Use `System.IO.Abstractions.TestingHelpers` for unit testing file providers:

```csharp
[TestClass]
public class FilePathProviderTests
{
    [TestMethod]
    public async Task GetCompletions_WithPartialPath_ReturnsMatchingFiles()
    {
        // Arrange
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            { @"C:\docs\readme.md", new MockFileData("content") },
            { @"C:\docs\readme.txt", new MockFileData("content") },
            { @"C:\docs\other.txt", new MockFileData("content") }
        });
        
        var provider = new FilePathProvider(fileSystem);
        var context = new CompletionContext
        {
            PartialValue = @"C:\docs\read",
            ElementType = CompletionElementType.ArgumentValue
        };
        
        // Act
        var result = await provider.GetCompletionsAsync(context, CancellationToken.None);
        
        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(i => i.InsertText.EndsWith("readme.md"));
        result.Items.Should().Contain(i => i.InsertText.EndsWith("readme.txt"));
    }
}
```
