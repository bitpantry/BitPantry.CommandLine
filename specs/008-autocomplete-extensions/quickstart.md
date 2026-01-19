# Quickstart: Extension-Based Autocomplete System

**Feature**: 008-autocomplete-extensions  
**Date**: January 18, 2026

## Overview

The extension-based autocomplete system provides intelligent command-line suggestions using a provider-based architecture with two binding modes:

- **Type Providers** - Implement `ITypeAutoCompleteProvider` with `CanHandle(Type)` for runtime matching
- **Attribute Providers** - Implement `IAutoCompleteHandler` and bind explicitly via `[AutoComplete]` attribute

---

## 1. Built-in Autocomplete (Zero Configuration)

Enum and boolean arguments automatically get autocomplete via built-in Type Providers.

```csharp
public enum LogLevel { Debug, Info, Warning, Error }

[Command]
public class SetLogCommand : CommandBase
{
    [Argument]
    public LogLevel Level { get; set; }  // ✅ Autocomplete: Debug, Error, Info, Warning
    
    [Argument]
    public bool Verbose { get; set; }     // ✅ Autocomplete: false, true
    
    [Argument]
    public LogLevel? Optional { get; set; }  // ✅ Nullable enums work too
}
```

**How it works**: 
- `EnumAutoCompleteProvider.CanHandle(LogLevel)` returns true (because `LogLevel.IsEnum`)
- Single provider handles all enum types - no per-enum registration needed

---

## 2. Creating Custom Type Providers

Create a Type Provider by implementing `ITypeAutoCompleteProvider`:

```csharp
public class LogLevelProvider : ITypeAutoCompleteProvider
{
    // Matching logic - determines which types this provider handles
    public bool CanHandle(Type type) => type == typeof(LogLevel);
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        // Custom logic: filter, add descriptions, etc.
        var options = Enum.GetNames<LogLevel>()
            .Where(n => n.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n)
            .Select(n => new AutoCompleteOption(n))
            .ToList();
            
        return Task.FromResult(options);
    }
}
```

### Registering Type Providers

```csharp
var app = new CommandLineApplicationBuilder()
    .RegisterCommands<MyCommand>()
    .RegisterAutoCompleteProviders()                  // Registers built-in providers
    .RegisterAutoCompleteProviders<MyCommand>()       // Scans for custom Type Providers
    .Build();
```

**Last-registered wins**: When you register a custom provider, it's added last and wins over built-in providers for matching types.

---

## 3. Using Attribute Providers

Use `[AutoComplete<THandler>]` to explicitly bind a handler to a specific argument:

```csharp
[Command]
public class DeployCommand : CommandBase
{
    [Argument]
    [AutoComplete<EnvironmentHandler>]  // Compile-time verified binding
    public string Target { get; set; }
}

// Handler only needs core interface - no CanHandle required
public class EnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var options = new[] { "development", "staging", "production" }
            .Where(e => e.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(e => e)
            .Select(e => new AutoCompleteOption(e))
            .ToList();
            
        return Task.FromResult(options);
    }
}
```

**Key Points**:
- Attribute Provider always wins over Type Provider
- Handler only implements `IAutoCompleteHandler` (not `ITypeAutoCompleteProvider`)
- No `CanHandle` method needed - the attribute IS the binding
- Generic constraint ensures compile-time type safety

---

## 4. Syntactic Sugar with Custom Attributes

Create custom attributes for cleaner usage:

```csharp
// Custom attribute inheriting from generic attribute
public class EnvironmentAutoCompleteAttribute : AutoCompleteAttribute<EnvironmentHandler> { }

// Usage - cleaner than [AutoComplete<EnvironmentHandler>]
[Command]
public class DeployCommand : CommandBase
{
    [Argument]
    [EnvironmentAutoComplete]  // Self-documenting, type-safe
    public string Target { get; set; }
}
```

---

## 5. Context-Aware Suggestions

Handlers can access prior argument values:

```csharp
public class CityHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        // Get the country that was already provided
        var countryArg = context.CommandInfo.Arguments
            .FirstOrDefault(a => a.Name == "Country");
        
        string? country = null;
        if (countryArg != null)
            context.ProvidedValues.TryGetValue(countryArg, out country);
        
        var cities = GetCitiesForCountry(country);
        
        return Task.FromResult(cities
            .Where(c => c.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .OrderBy(c => c)
            .Select(c => new AutoCompleteOption(c))
            .ToList());
    }
    
    private IEnumerable<string> GetCitiesForCountry(string? country) => country switch
    {
        "USA" => new[] { "Chicago", "Los Angeles", "New York" },
        "UK" => new[] { "Birmingham", "London", "Manchester" },
        _ => new[] { "London", "New York", "Tokyo" }
    };
}
```

---

## 6. Handlers with Dependencies

Handlers are resolved from DI and can have constructor dependencies:

```csharp
public class DatabaseTableHandler : IAutoCompleteHandler
{
    private readonly IDbConnection _db;
    private readonly ILogger<DatabaseTableHandler> _logger;
    
    public DatabaseTableHandler(IDbConnection db, ILogger<DatabaseTableHandler> logger)
    {
        _db = db;
        _logger = logger;
    }
    
    public async Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching tables for query: {Query}", context.QueryString);
        
        var tables = await _db.QueryAsync<string>(
            "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME LIKE @prefix + '%'",
            new { prefix = context.QueryString });
        
        return tables
            .OrderBy(t => t)
            .Select(t => new AutoCompleteOption(t))
            .ToList();
    }
}
```

---

## 7. Interface Comparison

| When to use | Interface | Example |
|-------------|-----------|---------|
| Runtime matching by type | `ITypeAutoCompleteProvider` | `EnumAutoCompleteProvider` |
| Explicit `[AutoComplete<T>]` binding | `IAutoCompleteHandler` | `EnvironmentHandler` |
| Either use case | `ITypeAutoCompleteProvider` | Can be used both ways |

```csharp
// Type Provider - used for runtime matching
public class EnumAutoCompleteProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t.IsEnum;  // Required for Type Providers
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// Attribute Handler - used for explicit binding
public class EnvironmentHandler : IAutoCompleteHandler
{
    // No CanHandle - attribute IS the binding
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// Can be used as either
public class LogLevelProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t == typeof(LogLevel);
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// As Type Provider (auto-matched):
[Argument]
public LogLevel Level { get; set; }

// As Attribute Provider (explicit binding):
[Argument]
[AutoComplete<LogLevelProvider>]
public string CustomLevel { get; set; }
```

---

## 8. Resolution Order

1. **Attribute Provider** - `[AutoComplete<X>]` on argument → resolve X from DI
2. **Type Providers** - Iterate in reverse registration order, first `CanHandle(type) == true` wins
3. **No match** - No autocomplete for this argument

```csharp
// Registration order:
// 1. EnumAutoCompleteProvider (CanHandle: t.IsEnum)
// 2. BooleanAutoCompleteProvider (CanHandle: t == bool)
// 3. LogLevelProvider (CanHandle: t == LogLevel)  ← user registered

// Resolution for LogLevel:
// → Check [AutoComplete] attribute: not present
// → Check [3] LogLevelProvider.CanHandle(LogLevel) → true → RETURN
// → Never reaches EnumAutoCompleteProvider
```

---

## 9. Quick Reference

```csharp
// Built-in autocomplete (automatic)
[Argument]
public LogLevel Level { get; set; }

// Custom Type Provider (implements CanHandle)
public class MyProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t == typeof(MyType);
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// Attribute Provider (no CanHandle needed)
public class MyHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

[Argument]
[AutoComplete<MyHandler>]
public string Value { get; set; }

// Custom attribute (syntactic sugar)
public class MyAutoCompleteAttribute : AutoCompleteAttribute<MyHandler> { }

[Argument]
[MyAutoComplete]
public string Value { get; set; }
```
