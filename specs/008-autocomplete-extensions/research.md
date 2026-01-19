# Research: Extension-Based Autocomplete System

**Feature**: 008-autocomplete-extensions  
**Date**: January 18, 2026

## Overview

This document consolidates research findings for implementing the extension-based autocomplete system. All technical decisions have been resolved.

---

## Decision 1: Two Separate Interfaces

**Question**: Should Type Providers and Attribute Providers share the same interface?

**Decision**: Two interfaces - core `IAutoCompleteHandler` and extended `ITypeAutoCompleteProvider`

**Rationale**:
- Attribute Providers are explicitly bound via `[AutoComplete]` attribute - no matching needed
- Type Providers require `CanHandle(Type)` for runtime matching
- Forcing Attribute Providers to implement `CanHandle` is wasteful and conceptually wrong
- Clean separation: core capability vs binding vehicle

**Interfaces**:
```csharp
// Core capability (used by both vehicles)
public interface IAutoCompleteHandler
{
    Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct);
}

// Type Provider vehicle (runtime matching)
public interface ITypeAutoCompleteProvider : IAutoCompleteHandler
{
    bool CanHandle(Type argumentType);
}
```

---

## Decision 2: `CanHandle(Type)` Method (Not `HandlesType` Property)

**Question**: How do Type Providers declare what they handle?

**Decision**: `bool CanHandle(Type argumentType)` method

**Rationale**:
- Method allows internal matching logic (e.g., `type.IsEnum` for EnumAutoCompleteProvider)
- Solves the "one provider per enum" problem - single EnumProvider handles all enums
- Providers encapsulate their own matching logic
- More flexible for future providers (interface matching, inheritance matching, etc.)

**Examples**:
```csharp
// EnumAutoCompleteProvider
public bool CanHandle(Type t) => t.IsEnum;  // Handles ALL enums

// BooleanAutoCompleteProvider  
public bool CanHandle(Type t) => t == typeof(bool);  // Strict match

// Custom LogLevelProvider
public bool CanHandle(Type t) => t == typeof(LogLevel);  // Specific override
```

---

## Decision 3: Registry with Last-Registered-Wins

**Question**: When multiple Type Providers match, which one wins?

**Decision**: Last registered wins (iterate in reverse, first match returns)

**How It Works**:
```csharp
List<ITypeAutoCompleteProvider> _typeProviders = new();

// Registration adds to end
_typeProviders.Add(provider);

// Resolution iterates in reverse
for (int i = _typeProviders.Count - 1; i >= 0; i--)
{
    if (_typeProviders[i].CanHandle(argType))
        return _typeProviders[i];
}
```

**Override Example**:
```csharp
// Built-in registered first:
// 1. EnumAutoCompleteProvider (CanHandle: t.IsEnum)

// User registers custom:
// 2. LogLevelProvider (CanHandle: t == typeof(LogLevel))

// Resolution for LogLevel:
// Check [2] LogLevelProvider.CanHandle(LogLevel) → true → return
// Never reaches EnumAutoCompleteProvider
```

---

## Decision 4: Generic Attribute with Compile-Time Constraints

**Question**: How should the `[AutoComplete]` attribute reference a handler?

**Decision**: Generic attribute `AutoCompleteAttribute<THandler>` with `where THandler : IAutoCompleteHandler`

**Rationale**:
- Compile-time type safety via generic constraint
- No runtime validation needed in constructor
- Cleaner syntax: `[AutoComplete<MyHandler>]` instead of `[AutoComplete(typeof(MyHandler))]`
- Better IDE autocomplete and refactoring support
- .NET 8 fully supports generic attributes (C# 11+)

**Implementation**:
```csharp
// Marker interface for reflection
public interface IAutoCompleteAttribute
{
    Type HandlerType { get; }
}

// Generic attribute with compile-time constraint
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
    where THandler : IAutoCompleteHandler
{
    public Type HandlerType => typeof(THandler);
}
```

---

## Decision 5: IAutoCompleteAttribute Marker Interface

**Question**: Why is the `IAutoCompleteAttribute` marker interface needed?

**Decision**: Required for reflection-based discovery of generic attributes

**Rationale**:
- Generic attributes like `AutoCompleteAttribute<THandler>` cannot be directly discovered via reflection using `GetCustomAttribute<AutoCompleteAttribute<>>()`
- C#/.NET reflection requires a concrete closed type to query for generic attributes
- The marker interface provides a non-generic handle that reflection can find: `GetCustomAttributes().OfType<IAutoCompleteAttribute>()`
- Once found, the `HandlerType` property gives access to the generic type parameter

**Implementation**:
```csharp
// Marker interface - allows reflection discovery
public interface IAutoCompleteAttribute
{
    Type HandlerType { get; }
}

// Generic attribute implements marker
public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
    where THandler : IAutoCompleteHandler
{
    public Type HandlerType => typeof(THandler);
}

// Discovery via marker interface
var attr = property.GetCustomAttributes()
    .OfType<IAutoCompleteAttribute>()
    .FirstOrDefault();

if (attr != null)
{
    var handlerType = attr.HandlerType;  // Access THandler type
    var handler = serviceProvider.GetRequiredService(handlerType);
}
```

---

## Decision 6: No Name Property on Type Handlers

**Question**: Should `ITypeAutoCompleteProvider` have a `Name` property?

**Decision**: No - class name is sufficient

**Rationale**:
- Name was only for logging/debugging
- `provider.GetType().Name` provides the same information
- Reduces interface surface area
- No functional use for the name

---

## Decision 7: Built-in Handlers Registered First

**Question**: How are built-in providers registered?

**Decision**: Registered during `RegisterAutoCompleteProviders()`, user providers added after

**Order**:
1. `EnumAutoCompleteProvider` (handles all enums via `CanHandle`)
2. `BooleanAutoCompleteProvider` (handles bool)
3. User providers added via assembly scanning (last wins)

**Key**: Single `EnumAutoCompleteProvider` handles all enum types - no per-enum instances needed.

---

## Decision 8: Legacy Removal (Complete)

**Question**: How do we handle migration from `AutoCompleteFunctionName`?

**Decision**: Remove completely with no backward compatibility

**Removed**:
| Item | Location |
|------|----------|
| `AutoCompleteFunctionName` property | `ArgumentAttribute` |
| `AutoCompleteFunctionName` property | `ArgumentInfo` |
| `IsAutoCompleteFunctionAsync` property | `ArgumentInfo` |
| `AutoCompleteContext` record | `AutoCompleteContext.cs` |
| Legacy invocation code | `AutoCompleteOptionSetBuilder` |

---

## Decision 9: Context Information for Handlers

**Question**: What information should handlers receive?

**Decision**: `AutoCompleteContext` class with request-scoped data only (no ServiceProvider)

| Property | Type | Purpose |
|----------|------|---------||
| `QueryString` | `string` | Current partial input |
| `FullInput` | `string` | Complete input line |
| `CursorPosition` | `int` | Position in input (1-based) |
| `ArgumentInfo` | `ArgumentInfo` | Metadata about argument |
| `ProvidedValues` | `IReadOnlyDictionary` | Already-entered values |
| `CommandInfo` | `CommandInfo` | Command being executed |

**Why no ServiceProvider?**
- Passing `IServiceProvider` is the Service Locator anti-pattern
- Handlers should declare dependencies via constructor injection
- Resolution happens at composition root (command input loop)
- Makes dependencies explicit and testable

---

## Summary: Interface Comparison

| Aspect | `IAutoCompleteHandler` | `ITypeAutoCompleteProvider` |
|--------|------------------------|----------------------------|
| Purpose | Core autocomplete capability | Runtime type matching |
| Members | `GetOptionsAsync(...)` | `CanHandle(Type)` + inherited |
| Used for | Attribute Providers | Type Providers |
| Binding | Explicit (attribute) | Implicit (runtime) |
| Example | `EnvironmentHandler` | `EnumAutoCompleteProvider` |
