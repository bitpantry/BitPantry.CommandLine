# Data Model: Extension-Based Autocomplete System

**Feature**: 008-autocomplete-extensions  
**Date**: January 18, 2026

## Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                       AUTOCOMPLETE HANDLER SYSTEM                            │
└─────────────────────────────────────────────────────────────────────────────┘

                    ┌───────────────────────────────────────┐
                    │       IAutoCompleteHandler            │
                    │       «interface»                     │
                    │       (Core Capability)               │
                    ├───────────────────────────────────────┤
                    │ + GetOptionsAsync(...): Task<List<>>  │
                    └───────────────────────────────────────┘
                                        ▲
                                        │ extends
            ┌───────────────────────────┼───────────────────────────┐
            │                           │                           │
            │                           │                           │
┌───────────┴───────────┐   ┌──────────┴──────────────┐   (Used directly by
│ Syntax Handlers       │   │ ITypeAutoCompleteHandler│    Attribute Handlers)
│ (internal)            │   │ «interface»             │
│ CommandSyntaxHandler  │   │ (Runtime Matching)      │
│ ArgumentNameHandler   │   ├─────────────────────────┤
│ ArgumentAliasHandler  │   │ + CanHandle(Type): bool │  ← Matching logic here
└───────────────────────┘   └─────────────────────────┘
                                        ▲
                                        │ implements
                  ┌─────────────────────┼─────────────────────┐
                  │                     │                     │
         ┌────────┴────────┐   ┌────────┴────────┐   ┌────────┴────────┐
         │ EnumAutoComplete│   │ Boolean         │   │ (Custom Type    │
         │ Handler         │   │ Handler         │   │  Handler)       │
         ├─────────────────┤   ├─────────────────┤   ├─────────────────┤
         │ CanHandle:      │   │ CanHandle:      │   │ CanHandle:      │
         │ type.IsEnum     │   │ type==bool      │   │ (custom logic)  │
         └─────────────────┘   └─────────────────┘   └─────────────────┘


┌─────────────────────────────────────────────────────────────────────────────┐
│  [AutoComplete<THandler>] where THandler : IAutoCompleteHandler              │
│  «generic attribute - compile-time verified binding»                         │
│                                                                              │
│  Generic constraint ensures type safety at compile time.                     │
│  Overrides any Type Handler for that argument.                               │
└─────────────────────────────────────────────────────────────────────────────┘


┌─────────────────────────────────────┐     ┌──────────────────────────────┐
│  AutoCompleteHandlerRegistry        │     │  AutoCompleteContext         │
│  «class»                            │     │  «class»                     │
├─────────────────────────────────────┤     ├──────────────────────────────┤
│ - _typeHandlers: List<ITypeAuto...>│     │ + QueryString: string        │
│ - _serviceProvider: IServiceProvider│     │ + FullInput: string          │
├─────────────────────────────────────┤     │ + CursorPosition: int        │
│ + Register<T>(): void               │────▶│ + ArgumentInfo: ArgumentInfo │
│ + GetHandler(ArgumentInfo): ?       │     │ + ProvidedValues: Dictionary │
│                                     │     │ + CommandInfo: CommandInfo   │
│ 1. Check for Attribute Handler      │     └──────────────────────────────┘
│ 2. Iterate Type Handlers (reverse)  │
│    → Ask CanHandle(type)            │
│    → First match wins               │
│ 3. Return null if not found         │
└─────────────────────────────────────┘
```

---

## Terminology

| Term | Definition |
|------|------------|
| **Handler** | Any class implementing `IAutoCompleteHandler` (core capability) |
| **Type Handler** | Handler that also implements `ITypeAutoCompleteHandler` for runtime matching |
| **Attribute Handler** | Handler explicitly referenced via `[AutoComplete<T>]` attribute |
| **Syntax Handler** | Internal handler for command structure completion (groups, commands, arg names/aliases) |

---

## Entities

### IAutoCompleteHandler

**Purpose**: Core interface for autocomplete capability. Used by ALL handlers (Attribute, Type, and Syntax).

| Member | Type | Description |
|--------|------|-------------|
| `GetOptionsAsync(context, ct)` | `Task<List<AutoCompleteOption>>` | Returns autocomplete suggestions |

**Key Design**: Handlers filter their results based on `QueryString` in context.

---

### ITypeAutoCompleteHandler

**Purpose**: Extended interface for Type Handlers with runtime matching logic.

| Member | Type | Description |
|--------|------|-------------|
| `CanHandle(Type)` | `bool` | Returns true if this handler can handle the argument type |
| *(inherited)* `GetOptionsAsync(...)` | `Task<List<...>>` | Returns autocomplete suggestions |

**Key Design**: Matching logic is encapsulated in handler, not registry.

---

### AutoCompleteAttribute<THandler>

**Purpose**: Explicitly reference which handler to use for an argument (Attribute Handler).

| Member | Type | Description |
|--------|------|-------------|
| `HandlerType` | `Type` | Returns `typeof(THandler)` |

**Constraints**:
- `[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]`
- Generic constraint: `where THandler : IAutoCompleteHandler` (compile-time verified)
- Implements `IAutoCompleteAttribute` marker interface for reflection

**Syntactic Sugar via Inheritance**:
```csharp
public class EnvironmentAutoCompleteAttribute : AutoCompleteAttribute<EnvironmentHandler> { }
```

---

### AutoCompleteHandlerRegistry

**Purpose**: Registry for Type Handlers with resolution logic.

| Member | Type | Description |
|--------|------|-------------|
| `_typeHandlers` | `List<ITypeAutoCompleteHandler>` | Type Handlers in registration order |
| `_serviceProvider` | `IServiceProvider` | For resolving Attribute Handlers |
| `Register<T>()` | `void` | Adds Type Handler to end of list |
| `GetHandler(ArgumentInfo)` | `IAutoCompleteHandler?` | Returns handler for argument |

**Resolution Algorithm**:
1. Check for `[AutoComplete<T>]` attribute → resolve handler from DI
2. Iterate `_typeHandlers` in **reverse** (last registered first)
   - Ask each: `handler.CanHandle(argumentType)`
   - Return first that returns true
3. Return null if no handler matches

**Last-registered-wins semantics, like command overrides.**

---

### AutoCompleteContext

**Purpose**: Context passed to handlers when generating suggestions.

| Property | Type | Description |
|----------|------|-------------|
| `QueryString` | `string` | Current partial input being typed (handlers filter by this) |
| `FullInput` | `string` | Complete input line |
| `CursorPosition` | `int` | Position in input (1-based) |
| `ArgumentInfo` | `ArgumentInfo` | Metadata about argument being completed |
| `ProvidedValues` | `IReadOnlyDictionary<ArgumentInfo, string>` | Already-provided argument values |
| `CommandInfo` | `CommandInfo` | Command being executed |

**Note**: Handlers receive dependencies via constructor injection, not from context. This follows proper DI principles - resolution happens at the composition root.

---

## Built-in Type Handlers

### EnumAutoCompleteHandler

**Implements**: `ITypeAutoCompleteHandler`

| Method | Implementation |
|--------|----------------|
| `CanHandle(Type t)` | `return t.IsEnum;` |
| `GetOptionsAsync(...)` | Returns filtered enum names |

**Key**: Single instance handles ALL enum types. Gets actual enum type from `context.ArgumentInfo.PropertyType`. Handler filters based on `QueryString`.

```csharp
public class EnumAutoCompleteHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type type) => type.IsEnum;
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var enumType = Nullable.GetUnderlyingType(context.ArgumentInfo.PropertyType) 
                    ?? context.ArgumentInfo.PropertyType;
        
        var query = context.QueryString ?? "";
        return Task.FromResult(Enum.GetNames(enumType)
            .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Select(n => new AutoCompleteOption(n))
            .ToList());
    }
}
```

---

### BooleanAutoCompleteHandler

**Implements**: `ITypeAutoCompleteHandler`

| Method | Implementation |
|--------|----------------|
| `CanHandle(Type t)` | `return t == typeof(bool);` |
| `GetOptionsAsync(...)` | Returns filtered ["false", "true"] |

```csharp
public class BooleanAutoCompleteHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type type) => type == typeof(bool);
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        var query = context.QueryString ?? "";
        return Task.FromResult(new[] { "false", "true" }
            .Where(o => o.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(o => new AutoCompleteOption(o))
            .ToList());
    }
}
```

---

## Registration Flow

```
1. CommandLineApplicationBuilder.Build() called
   └─ AddAutoCompleteServices() internal method
      ├─ Register<EnumAutoCompleteHandler>()     // Index 0
      └─ Register<BooleanAutoCompleteHandler>()  // Index 1

2. Consumer calls RegisterAutoCompleteHandler<T>()
   └─ Register<LogLevelHandler>()                // Index 2

3. Resolution for LogLevel argument:
   ├─ Check [AutoComplete<T>] attribute → not present
   └─ Iterate reverse:
      ├─ [2] LogLevelHandler.CanHandle(LogLevel) → true → RETURN
      └─ (never reached) EnumAutoCompleteHandler
```

---

## Resolution Flow

```
User types → AutoCompleteController.Begin()
  → AutoCompleteOptionSetBuilder.BuildOptions()
    → For argument value completion:
      → AutoCompleteHandlerRegistry.GetHandler(argumentInfo)
        → Check for [AutoComplete<T>] attribute on property
          → If found: Resolve from DI (IAutoCompleteHandler)
        → Iterate Type Handlers (reverse order)
          → Ask each: handler.CanHandle(argumentType)
          → First that returns true: Return it
        → No match: Return null
      → If handler found: Call handler.GetOptionsAsync(context)
        → Handler filters based on QueryString
    → For command syntax completion:
      → Use Syntax Handlers (CommandSyntaxHandler, ArgumentNameHandler, etc.)
      → Syntax Handlers implement IAutoCompleteHandler for consistency
```
