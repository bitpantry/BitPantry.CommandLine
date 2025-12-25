# Quickstart: Autocomplete Provider Implementation

**Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md) | **Data Model**: [data-model.md](data-model.md)

## Overview

This guide shows command implementers how to add custom autocomplete to their commands using the new provider-based system.

---

## Built-in Providers

The following providers are available out of the box:

| Provider | Use Case | Attribute |
|----------|----------|-----------|
| `FilePathProvider` | Complete file paths | `[FilePath]` |
| `DirectoryPathProvider` | Complete directory paths | `[DirectoryPath]` |
| `EnumValueProvider` | Complete enum argument values | (automatic for enum types) |
| `CommandCompletionProvider` | Commands and groups | (automatic) |
| `ArgumentNameProvider` | --argument names | (automatic) |
| `ArgumentAliasProvider` | -a aliases | (automatic) |

---

## Using Built-in File/Directory Completion

### File Path Completion

```csharp
public class ReadFileCommand
{
    [Argument("path", Alias = "p")]
    [FilePath]  // ← Enables file path autocompletion
    public string FilePath { get; set; }
    
    public void Execute()
    {
        var content = File.ReadAllText(FilePath);
        Console.WriteLine(content);
    }
}
```

### Directory Path Completion

```csharp
public class ListFilesCommand
{
    [Argument("directory", Alias = "d")]
    [DirectoryPath]  // ← Enables directory path autocompletion
    public string Directory { get; set; }
    
    public void Execute()
    {
        foreach (var file in Directory.GetFiles(Directory))
            Console.WriteLine(file);
    }
}
```

---

## Creating a Custom Provider

### Step 1: Implement ICompletionProvider

```csharp
using BitPantry.CommandLine.AutoComplete.Providers;

public class EnvironmentProvider : ICompletionProvider
{
    // Higher priority = checked first
    public int Priority => 50;
    
    // Determine if this provider handles the context
    public bool CanHandle(CompletionContext context)
    {
        return context.ElementType == CompletionElementType.ArgumentValue &&
               context.ArgumentName == "environment";
    }
    
    // Return completion suggestions
    public Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        var environments = new[] { "development", "staging", "production" };
        var prefix = context.PartialValue;
        
        var items = environments
            .Where(e => e.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            .Select(e => new CompletionItem
            {
                InsertText = e,
                DisplayText = e,
                Description = GetDescription(e),
                Kind = CompletionItemKind.ArgumentValue
            })
            .ToList();
        
        return Task.FromResult(new CompletionResult { Items = items });
    }
    
    private static string GetDescription(string env) => env switch
    {
        "development" => "Local development",
        "staging" => "Pre-production testing",
        "production" => "Live environment",
        _ => string.Empty
    };
}
```

### Step 2: Register the Provider

```csharp
// In your DI setup / CommandLineApplicationBuilder
services.AddSingleton<ICompletionProvider, EnvironmentProvider>();
```

### Step 3: Use in Command

```csharp
public class DeployCommand
{
    [Argument("environment", Alias = "e")]
    public string Environment { get; set; }  // Will use EnvironmentProvider
    
    [Argument("version", Alias = "v")]
    public string Version { get; set; }
    
    public void Execute()
    {
        Console.WriteLine($"Deploying {Version} to {Environment}");
    }
}
```

---

## Async/Remote Providers

For providers that need to fetch data from external sources:

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
        return context.ArgumentName == "user" || 
               context.ArgumentName == "assignee";
    }
    
    public async Task<CompletionResult> GetCompletionsAsync(
        CompletionContext context, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Fetch users from service (respects cancellation)
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

For simple static lists, use the `[CompletionValues]` attribute:

```csharp
public class DeployCommand : CommandBase
{
    [Argument("environment")]
    [Alias('e')]
    [CompletionValues("development", "staging", "production")]
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

The `EnumValueProvider` automatically detects enum-typed arguments and provides completions.

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
