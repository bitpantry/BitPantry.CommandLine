# Implementation Plan: Extension-Based Autocomplete System

**Branch**: `008-autocomplete-extensions` | **Date**: January 18, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-autocomplete-extensions/spec.md`

## Summary

**Complete replacement** of the existing autocomplete system. The legacy `AutoCompleteFunctionName` pattern, existing command syntax completion logic, and all associated infrastructure will be entirely **removed and reimplemented** using a unified handler-based architecture.

The new system uses **two handler interfaces**:

- **`IAutoCompleteHandler`** - Core interface with just `GetOptionsAsync`. Used by Attribute Handlers (explicit binding) AND Command Syntax Handlers (internal).
- **`ITypeAutoCompleteHandler`** - Extends handler with `CanHandle(Type)`. Used for runtime type matching (implicit binding).

**Three handler categories**:
1. **Attribute Handlers** - Explicitly referenced via `[AutoComplete<THandler>]` attribute
2. **Type Handlers** - Auto-discovered based on argument type via `CanHandle(Type)` 
3. **Syntax Handlers** - Internal handlers for command structure (groups, commands, argument names)

**Scope of reimplementation**:
- Value autocomplete (enum, boolean, custom types) → Handler system
- Command syntax autocomplete (groups, commands, argument names) → Built-in syntax handlers (implementing same `IAutoCompleteHandler` interface)
- Legacy code → Complete removal

This unified architecture ensures all handlers share the same interface, enabling consistent testing, invocation, and future extensibility.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Microsoft.Extensions.DependencyInjection, Spectre.Console, Microsoft.Extensions.Logging  
**Storage**: N/A (in-memory registry, no persistence)  
**Testing**: MSTest with FluentAssertions and Moq  
**Target Platform**: Cross-platform (.NET 8.0)  
**Project Type**: Library (multi-project solution)  
**Constraints**: Single-threaded autocomplete (cancel previous on new input), Case-insensitive matching  
**Scale/Scope**: Supports commands with any number of arguments, menu shows 5 visible items (scrollable)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence / Notes |
|-----------|--------|------------------|
| **I. Test-Driven Development** | ✅ PASS | Tests will be written first for all new interfaces, handlers, and registry. Existing `AutoCompleteSetBuilderTests_*` patterns to be followed. |
| **II. Dependency Injection** | ✅ PASS | All handlers registered in DI container. Auto-discovered via assembly scanning like commands. |
| **III. Security by Design** | ✅ PASS (N/A) | No tokens, secrets, or trust boundaries involved in autocomplete. Remote support uses existing SignalR security. |
| **IV. Follow Existing Patterns** | ✅ PASS | Follows existing `CommandRegistry` pattern. Last registered Type Handler wins (like command overrides). |
| **V. Integration Testing** | ✅ PASS | Remote autocomplete (P3) will include integration tests using existing SignalR test infrastructure. |
| **Testing Standards** | ✅ PASS | MSTest + FluentAssertions + Moq as per constitution. Naming: `MethodUnderTest_Scenario_ExpectedBehavior`. |

**Gate Result**: ✅ PASSED - Proceed to Phase 0

## Test File Convention

Test case IDs use the `###:XX-###` format (e.g., `008:TC-1.1`) for global uniqueness across specs.

**In test files:**
```csharp
/// <summary>
/// Tests for AutoCompleteHandlerRegistry.
/// </summary>
[TestClass]
public class AutoCompleteHandlerRegistryTests
{
    #region Spec 008-autocomplete-extensions

    /// <summary>
    /// Implements: 008:TC-1.1
    /// Register adds handler to the list.
    /// </summary>
    [TestMethod]
    public void Register_WithValidHandler_AddsToRegistry() { }

    #endregion
}
```

**If future specs add tests to the same file**, use separate regions:
```csharp
#region Spec 012-autocomplete-enhancements
/// <summary>
/// Implements: 012:TC-1.1
/// </summary>
#endregion
```

## Project Structure

### Documentation (this feature)

```text
specs/008-autocomplete-extensions/
├── spec.md              # Feature specification
├── experience.md        # UX specification with console mockups
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (internal interfaces)
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
BitPantry.CommandLine/
├── AutoComplete/
│   ├── AutoCompleteController.cs        # KEEP: Minor updates for new option building
│   ├── AutoCompleteContext.cs           # REMOVE: Replaced by Handlers/AutoCompleteContext.cs
│   ├── AutoCompleteOption.cs            # KEEP: Existing option model
│   ├── AutoCompleteOptionSet.cs         # KEEP: Existing option set
│   ├── AutoCompleteOptionSetBuilder.cs  # REWRITE: Central routing, delegates to handlers
│   ├── AutoCompleteConstants.cs         # NEW: Constants (DefaultVisibleMenuItems)
│   ├── UsedArgumentTracker.cs           # NEW: Tracks which args are already provided
│   ├── Handlers/                        # NEW: Handler infrastructure
│   │   ├── IAutoCompleteHandler.cs          # Core handler interface
│   │   ├── IAutoCompleteAttribute.cs        # Marker interface for attribute discovery
│   │   ├── AutoCompleteAttribute.cs         # Generic attribute [AutoComplete<T>]
│   │   ├── ITypeAutoCompleteHandler.cs      # Type Handler interface (extends handler)
│   │   ├── AutoCompleteHandlerRegistry.cs   # Registry with CanHandle matching
│   │   ├── AutoCompleteContext.cs           # Context for handlers
│   │   ├── EnumAutoCompleteHandler.cs       # Built-in: all enums
│   │   └── BooleanAutoCompleteHandler.cs    # Built-in: booleans
│   └── Syntax/                          # NEW: Command syntax autocomplete
│       ├── CommandSyntaxHandler.cs          # Groups, commands, command aliases (implements IAutoCompleteHandler)
│       ├── ArgumentNameHandler.cs           # --argName suggestions (implements IAutoCompleteHandler)
│       └── ArgumentAliasHandler.cs          # -alias suggestions (implements IAutoCompleteHandler)
├── API/
│   └── ArgumentAttribute.cs             # MODIFY: Remove AutoCompleteFunctionName
└── Component/
    └── ArgumentInfo.cs                  # MODIFY: Remove AutoCompleteFunctionName, IsAutoCompleteFunctionAsync

BitPantry.CommandLine.Tests/
├── AutoComplete/
│   ├── Handlers/                        # NEW: Handler tests
│   │   ├── EnumAutoCompleteHandlerTests.cs
│   │   ├── BooleanAutoCompleteHandlerTests.cs
│   │   └── AutoCompleteHandlerRegistryTests.cs
│   ├── Syntax/                          # NEW: Command syntax tests
│   │   ├── CommandSyntaxHandlerTests.cs
│   │   ├── ArgumentNameHandlerTests.cs
│   │   ├── ArgumentAliasHandlerTests.cs
│   │   └── UsedArgumentTrackerTests.cs
│   └── Integration/                     # NEW: Integration tests
│       └── AutoCompleteIntegrationTests.cs
└── Commands/
    └── AutoCompleteCommands/            # REMOVE: Legacy test commands deleted

BitPantry.CommandLine.Remote.SignalR/
├── Envelopes/
│   ├── AutoCompleteRequest.cs           # NEW: RPC request for remote autocomplete
│   └── AutoCompleteResponse.cs          # NEW: RPC response with options

BitPantry.CommandLine.Remote.SignalR.Server/
├── CommandLineHub.cs                    # MODIFY: Add GetAutoCompleteOptionsAsync method

BitPantry.CommandLine.Remote.SignalR.Client/
├── SignalRServerProxy.cs                # MODIFY: Add GetAutoCompleteOptionsAsync method
├── IServerProxy.cs                      # MODIFY: Add GetAutoCompleteOptionsAsync to interface

BitPantry.CommandLine.Tests.Remote.SignalR/
├── AutoComplete/                        # NEW: Remote autocomplete tests
│   └── RemoteAutoCompleteTests.cs
```

## Complexity Tracking

> No violations to justify - design follows all constitution principles.

## Technical Design

### Core Interface (Shared Capability)

```csharp
/// <summary>
/// Core interface for autocomplete capability.
/// Used by ALL autocomplete handlers: Attribute Handlers, Type Handlers, and Syntax Handlers.
/// </summary>
public interface IAutoCompleteHandler
{
    Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default);
}
```

### Type Handler Interface (Runtime Matching)

```csharp
/// <summary>
/// Extended interface for Type Handlers with runtime type matching.
/// Matching logic is encapsulated in CanHandle method.
/// </summary>
public interface ITypeAutoCompleteHandler : IAutoCompleteHandler
{
    /// <summary>
    /// Determines if this handler can handle the given argument type.
    /// Called at runtime to find matching handler.
    /// </summary>
    bool CanHandle(Type argumentType);
}
```

### Why Two Interfaces?

| Concern | Type Handler | Attribute Handler | Syntax Handler |
|---------|--------------|-------------------|----------------|
| Binding | Implicit (runtime matching) | Explicit (`[AutoComplete<THandler>]`) | Internal (position-based) |
| Matching | Via `CanHandle(Type)` method | Attribute IS the binding | Builder routes by position |
| Interface | `ITypeAutoCompleteHandler` | `IAutoCompleteHandler` | `IAutoCompleteHandler` |
| `CanHandle` needed? | ✅ Yes | ❌ No | ❌ No |
| In Registry? | ✅ Yes | ❌ No (resolved from DI) | ❌ No (internal) |

Attribute Handlers don't need `CanHandle` because the attribute explicitly declares the binding.
Syntax Handlers are internal and invoked directly by the builder based on parsed input position.

### Handler Registry

```csharp
/// <summary>
/// Registry for Type Handlers.
/// Iterates handlers and asks who can handle the type.
/// Last registered wins (like command overrides).
/// </summary>
public class AutoCompleteHandlerRegistry
{
    private readonly List<ITypeAutoCompleteHandler> _typeHandlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoCompleteHandlerRegistry> _logger;
    
    /// <summary>
    /// Registers a Type Handler. Added to end of list (last wins).
    /// </summary>
    public void Register<THandler>() where THandler : ITypeAutoCompleteHandler
    {
        var handler = (ITypeAutoCompleteHandler)_serviceProvider.GetRequiredService<THandler>();
        _typeHandlers.Add(handler);
        _logger.LogDebug("Registered Type Handler {Type}", typeof(THandler).Name);
    }
    
    /// <summary>
    /// Gets the handler for an argument.
    /// 1. Check for Attribute Handler ([AutoComplete] attribute)
    /// 2. Ask Type Handlers who can handle (last registered wins)
    /// 3. Return null if no handler found
    /// </summary>
    public IAutoCompleteHandler? GetHandler(ArgumentInfo argumentInfo)
    {
        // 1. Check for Attribute Handler (explicit binding)
        var explicitAttr = argumentInfo.PropertyInfo
            .GetCustomAttributes(inherit: true)
            .OfType<IAutoCompleteAttribute>()
            .FirstOrDefault();
        
        if (explicitAttr != null)
        {
            _logger.LogDebug("Using Attribute Handler {Type} for {Arg}",
                explicitAttr.HandlerType.Name, argumentInfo.Name);
            return (IAutoCompleteHandler)_serviceProvider
                .GetRequiredService(explicitAttr.HandlerType);
        }
        
        // 2. Find Type Handler (last registered wins)
        var argType = argumentInfo.PropertyInfo.PropertyType;
        var lookupType = Nullable.GetUnderlyingType(argType) ?? argType;
        
        // Iterate in reverse: last registered wins
        for (int i = _typeProviders.Count - 1; i >= 0; i--)
        {
            if (_typeProviders[i].CanHandle(lookupType))
            {
                _logger.LogDebug("Using Type Provider {Type} for {Arg}",
                    _typeProviders[i].GetType().Name, argumentInfo.Name);
                return _typeProviders[i];
            }
        }
        
        // 3. No provider found
        return null;
    }
}
```

### Built-in Type Handlers

```csharp
/// <summary>
/// Type Handler for ALL enum types.
/// Single instance handles all enums via CanHandle.
/// Filters values based on QueryString - handlers are responsible for filtering.
/// </summary>
public class EnumAutoCompleteHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type type) => type.IsEnum;
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var enumType = context.ArgumentInfo.PropertyType;
        var actualType = Nullable.GetUnderlyingType(enumType) ?? enumType;
        
        // Handlers filter based on QueryString
        var query = context.QueryString ?? "";
        var options = Enum.GetNames(actualType)
            .Where(n => n.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .Select(n => new AutoCompleteOption(n))
            .ToList();
            
        return Task.FromResult(options);
    }
}

/// <summary>
/// Type Handler for boolean arguments.
/// Filters values based on QueryString - handlers are responsible for filtering.
/// </summary>
public class BooleanAutoCompleteHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type type) => type == typeof(bool);
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        // Handlers filter based on QueryString
        var query = context.QueryString ?? "";
        var options = new[] { "false", "true" }
            .Where(o => o.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(o => new AutoCompleteOption(o))
            .ToList();
            
        return Task.FromResult(options);
    }
}
```

### AutoCompleteAttribute (for Attribute Providers)

```csharp
/// <summary>
/// Marker interface for reflection-based discovery of generic attributes.
/// </summary>
public interface IAutoCompleteAttribute
{
    Type HandlerType { get; }
}

/// <summary>
/// Explicitly specifies which handler to use for this argument.
/// Overrides any Type Provider for this argument.
/// Generic constraint provides compile-time type safety.
/// Supports inheritance for syntactic-sugar custom attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
    where THandler : IAutoCompleteHandler
{
    public Type HandlerType => typeof(THandler);
}
```

### Handler Context

```csharp
public class AutoCompleteContext
{
    public required string QueryString { get; init; }
    public required string FullInput { get; init; }
    public required int CursorPosition { get; init; }
    public required ArgumentInfo ArgumentInfo { get; init; }
    public required IReadOnlyDictionary<ArgumentInfo, string> ProvidedValues { get; init; }
    public required CommandInfo CommandInfo { get; init; }
}
```

### Registration

Autocomplete is **enabled by default** when building a `CommandLineApplication`. The handler registry is automatically configured with built-in Type Handlers. No explicit `AddAutoComplete()` call is required.

```csharp
/// <summary>
/// Standalone registry for autocomplete handlers.
/// Separate from CommandRegistry - follows similar patterns but independent.
/// </summary>
public class AutoCompleteHandlerRegistry
{
    private readonly List<ITypeAutoCompleteHandler> _typeHandlers = new();
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AutoCompleteHandlerRegistry> _logger;
    
    public AutoCompleteHandlerRegistry(
        IServiceProvider serviceProvider,
        ILogger<AutoCompleteHandlerRegistry> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Built-in Type Handlers auto-registered
        Register<EnumAutoCompleteHandler>();
        Register<BooleanAutoCompleteHandler>();
    }
    
    public void Register<THandler>() where THandler : ITypeAutoCompleteHandler
    {
        var handler = (ITypeAutoCompleteHandler)_serviceProvider
            .GetRequiredService<THandler>();
        _typeHandlers.Add(handler);
        _logger.LogDebug("Registered Type Handler {Type}", typeof(THandler).Name);
    }
    
    public void RegisterFromAssemblies(params Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            var handlerTypes = assembly.GetTypes()
                .Where(t => typeof(ITypeAutoCompleteHandler).IsAssignableFrom(t)
                         && !t.IsAbstract
                         && !t.IsInterface
                         && t != typeof(EnumAutoCompleteHandler)
                         && t != typeof(BooleanAutoCompleteHandler));
            
            foreach (var handlerType in handlerTypes)
            {
                var handler = (ITypeAutoCompleteHandler)ActivatorUtilities
                    .CreateInstance(_serviceProvider, handlerType);
                _typeHandlers.Add(handler);
                _logger.LogDebug("Registered Type Handler {Type}", handlerType.Name);
            }
        }
    }
    
    // GetHandler method as defined earlier...
}
```

#### DI Registration (Internal)

Autocomplete is registered automatically during `CommandLineApplicationBuilder.Build()`:

```csharp
// Internal - called by CommandLineApplicationBuilder.Build()
internal static IServiceCollection AddAutoCompleteServices(this IServiceCollection services)
{
    services.AddSingleton<AutoCompleteHandlerRegistry>();
    services.AddTransient<EnumAutoCompleteHandler>();
    services.AddTransient<BooleanAutoCompleteHandler>();
    return services;
}
```

#### Consumer Extensions (Custom Handlers)

Two extension methods are provided for registering custom Type Handlers, following the same pattern as command registration:

```csharp
/// <summary>
/// Extension methods for registering custom autocomplete handlers.
/// Follows same patterns as command registration.
/// </summary>
public static class AutoCompleteExtensions
{
    /// <summary>
    /// Registers a single custom Type Handler.
    /// </summary>
    public static CommandLineApplicationBuilder RegisterAutoCompleteHandler<THandler>(
        this CommandLineApplicationBuilder builder)
        where THandler : class, ITypeAutoCompleteHandler
    {
        builder.Services.AddTransient<THandler>();
        builder.OnBuild(sp =>
        {
            var registry = sp.GetRequiredService<AutoCompleteHandlerRegistry>();
            registry.Register<THandler>();
        });
        return builder;
    }
    
    /// <summary>
    /// Scans assemblies for all ITypeAutoCompleteHandler implementations and registers them.
    /// Follows same pattern as command assembly scanning.
    /// </summary>
    public static CommandLineApplicationBuilder RegisterAutoCompleteHandlersFromAssemblies(
        this CommandLineApplicationBuilder builder,
        params Assembly[] assemblies)
    {
        builder.OnBuild(sp =>
        {
            var registry = sp.GetRequiredService<AutoCompleteHandlerRegistry>();
            registry.RegisterFromAssemblies(assemblies);
        });
        return builder;
    }
}

// Usage - single handler
var app = new CommandLineApplicationBuilder()
    .RegisterAutoCompleteHandler<MyCustomTypeHandler>()
    .Build();

// Usage - assembly scanning (like command registration)
var app = new CommandLineApplicationBuilder()
    .RegisterAutoCompleteHandlersFromAssemblies(typeof(MyCommands).Assembly)
    .Build();

// Usage - combined
var app = new CommandLineApplicationBuilder()
    .RegisterAutoCompleteHandlersFromAssemblies(typeof(MyCommands).Assembly)
    .RegisterAutoCompleteHandler<SpecialHandler>()  // Added last, wins for matching types
    .Build();
```

### Integration Architecture

The autocomplete system integrates into the existing input pipeline:

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Existing Infrastructure                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│  InputBuilder                                                                   │
│    └─► AutoCompleteController (owns, coordinates UI)                            │
│          └─► AutoCompleteOptionSetBuilder (REWRITE - routing logic)             │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              New Handler System                                  │
├─────────────────────────────────────────────────────────────────────────────────┤
│  AutoCompleteOptionSetBuilder.BuildOptions(parsedElement)                       │
│    │                                                                            │
│    ├─► Position Type Detection (based on CommandElementType)                    │
│    │     - Command/PositionalValue → might be group/command OR value            │
│    │     - ArgumentName → syntax completion (--name)                            │
│    │     - ArgumentAlias → syntax completion (-a)                               │
│    │     - ArgumentValue/Empty → value completion                               │
│    │                                                                            │
│    ├─► Syntax Handlers (for command structure, implement IAutoCompleteHandler)  │
│    │     ├── CommandSyntaxHandler (groups, commands, aliases)                   │
│    │     ├── ArgumentNameHandler (--argName suggestions)                        │
│    │     └── ArgumentAliasHandler (-alias suggestions)                          │
│    │                                                                            │
│    └─► Value Handler System (for argument values)                               │
│          └── AutoCompleteHandlerRegistry                                        │
│                ├── Check [AutoComplete<T>] attribute (explicit)                 │
│                └── Query Type Handlers via CanHandle (implicit)                 │
│                      ├── EnumAutoCompleteHandler                                │
│                      ├── BooleanAutoCompleteHandler                             │
│                      └── Custom Type Handlers...                                │
└─────────────────────────────────────────────────────────────────────────────────┘
```

**Key Design Points:**

1. **`AutoCompleteOptionSetBuilder` is the single integration point** - It already owns the routing logic based on `CommandElementType`. We rewrite this class to delegate to handlers.

2. **All handlers share `IAutoCompleteHandler` interface** - Syntax handlers, Attribute handlers, and Type handlers all implement the same `GetOptionsAsync()` method. The `AutoCompleteOptionSetBuilder` normalizes results regardless of source.

3. **Filtering happens in handlers** - Handlers receive `QueryString` in `AutoCompleteContext` and are responsible for filtering their results. This allows handlers to implement custom filtering logic (e.g., fuzzy matching) if desired.

4. **Existing `ParsedInput` / `ParsedCommandElement` are kept** - The parsing infrastructure already determines position context (`CommandElementType`). We reuse this rather than duplicating detection logic.

#### AutoCompleteOptionSetBuilder Rewrite

```csharp
public class AutoCompleteOptionSetBuilder : IDisposable
{
    private readonly CommandRegistry _registry;
    private readonly AutoCompleteHandlerRegistry _handlerRegistry;
    private readonly IServerProxy _serverProxy;
    private readonly ILogger<AutoCompleteOptionSetBuilder> _logger;
    
    // Syntax handlers (implement IAutoCompleteHandler for consistency)
    private readonly CommandSyntaxHandler _commandSyntaxHandler;
    private readonly ArgumentNameHandler _argNameHandler;
    private readonly ArgumentAliasHandler _argAliasHandler;
    
    public async Task<AutoCompleteOptionSet> BuildOptions(
        ParsedCommandElement parsedElement,
        CancellationToken token = default)
    {
        List<AutoCompleteOption> options = parsedElement.ElementType switch
        {
            // Command syntax completion (handlers filter internally)
            CommandElementType.Command => await GetCommandOptions(parsedElement, token),
            CommandElementType.PositionalValue => await GetPositionalOrCommandOptions(parsedElement, token),
            CommandElementType.ArgumentName => await GetArgumentNameOptions(parsedElement, token),
            CommandElementType.ArgumentAlias => await GetArgumentAliasOptions(parsedElement, token),
            CommandElementType.EndOfOptions => await GetArgumentNameOptions(parsedElement, token),
            
            // Value completion (handlers filter internally)
            CommandElementType.ArgumentValue => await GetValueOptions(parsedElement, token),
            CommandElementType.Empty => await GetEmptyPositionOptions(parsedElement, token),
            
            CommandElementType.Unexpected => await GetUnexpectedOptions(parsedElement, token),
            _ => null
        };
        
        if (options == null || options.Count == 0)
            return null;
        
        // Handlers already filtered - just build option set
        return new AutoCompleteOptionSet(options);
    }
    
    private async Task<List<AutoCompleteOption>> GetValueOptions(
        ParsedCommandElement parsedElement,
        CancellationToken token)
    {
        var argInfo = ResolveArgumentInfo(parsedElement);
        if (argInfo == null) return null;
        
        var handler = _handlerRegistry.GetHandler(argInfo);
        if (handler == null) return null;
        
        var context = BuildHandlerContext(parsedElement, argInfo);
        
        try
        {
            // Handler filters based on QueryString in context
            return await handler.GetOptionsAsync(context, token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Handler {Type} threw exception", handler.GetType().Name);
            return null;
        }
    }
    
    // Syntax handlers also implement IAutoCompleteHandler
    private async Task<List<AutoCompleteOption>> GetCommandOptions(
        ParsedCommandElement parsedElement,
        CancellationToken token)
    {
        var context = BuildSyntaxContext(parsedElement);
        return await _commandSyntaxHandler.GetOptionsAsync(context, token);
    }
}
```

### Resolution Flow

```
User types -> AutoCompleteController.Begin()
  -> Determine autocomplete position type
  -> Route to appropriate handler (all implement IAutoCompleteHandler):
    
    1. COMMAND POSITION (no command parsed yet):
       -> CommandSyntaxHandler.GetOptionsAsync(context)
       -> Handler filters by QueryString, returns: matching groups + commands + aliases
    
    2. ARGUMENT NAME POSITION (after "--" or "-"):
       -> ArgumentNameHandler.GetOptionsAsync(context)
       -> Handler filters by QueryString, returns: matching "--argName" (unused only)
    
    3. ARGUMENT VALUE POSITION:
       -> AutoCompleteHandlerRegistry.GetHandler(argumentInfo)
         -> Check for [AutoComplete<T>] attribute (explicit)
         -> Iterate Type Handlers (implicit, last wins)
       -> If handler found: handler.GetOptionsAsync(context), handler filters
       -> No handler: No suggestions
    
    4. POSITIONAL VALUE POSITION:
       -> Determine which positional parameter based on cursor position
       -> AutoCompleteHandlerRegistry.GetHandler(positionalArgumentInfo)
       -> Same logic as argument value
```

### Command Syntax Autocomplete (New Implementation)

The command syntax autocomplete system is **reimplemented from scratch** using a unified approach. **Syntax handlers implement `IAutoCompleteHandler`** for consistency with value handlers.

#### Syntax Handler Interface Consistency

All syntax handlers implement the same interface as value handlers:

```csharp
/// <summary>
/// All syntax handlers implement IAutoCompleteHandler for consistency.
/// They receive context with QueryString and filter internally.
/// </summary>
public class CommandSyntaxHandler : IAutoCompleteHandler
{
    private readonly CommandRegistry _registry;
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var query = context.QueryString ?? "";
        return Task.FromResult(GetGroupsAndCommands(query));
    }
    
    private List<AutoCompleteOption> GetGroupsAndCommands(string queryString)
    {
        // Implementation as before, with filtering
    }
}
```

#### Reusing Existing Position Detection

The existing `ParsedInput` and `ParsedCommandElement` infrastructure already provides position context via `CommandElementType`. We **reuse this** rather than duplicating detection logic:

```csharp
// Existing enum - already handles position detection
public enum CommandElementType
{
    Command,         // Group or command name
    PositionalValue, // Could be path element OR positional argument value
    ArgumentName,    // After "--"
    ArgumentAlias,   // After "-"
    ArgumentValue,   // Value for a named argument
    Empty,           // Whitespace position (could be value position)
    EndOfOptions,    // Bare "--"
    Unexpected       // Prefix characters like "-" or "--"
}
```

The `AutoCompleteOptionSetBuilder.BuildOptions()` switch already routes based on this enum. The new implementation rewrites the handler methods, not the routing.

#### Command/Group Suggestions

When cursor is at command position, the handler filters and returns matching suggestions:

```csharp
/// <summary>
/// Implements IAutoCompleteHandler for consistency with value handlers.
/// </summary>
public class CommandSyntaxHandler : IAutoCompleteHandler
{
    private readonly CommandRegistry _registry;
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";
        var options = new List<AutoCompleteOption>();
        
        // 1. Add matching command group names
        foreach (var group in _registry.GetGroups())
        {
            if (group.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
            {
                options.Add(new AutoCompleteOption(group.Name, group.Description));
            }
        }
        
        // 2. Add matching command names (at current group level)
        foreach (var command in _registry.GetCommands())
        {
            if (command.Name.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
            {
                options.Add(new AutoCompleteOption(command.Name, command.Description));
            }
            
            // 3. Add matching command aliases
            foreach (var alias in command.Aliases)
            {
                if (alias.StartsWith(queryString, StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(alias, $"Alias for {command.Name}"));
                }
            }
        }
        
        // Sort alphabetically
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
```

#### Argument Name Suggestions

When cursor is after "--", handler filters and suggests unused argument names:

```csharp
/// <summary>
/// Implements IAutoCompleteHandler for consistency.
/// </summary>
public class ArgumentNameHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";  // Text after "--" (e.g., "tar" from "--tar")
        var usedArgs = GetUsedArguments(context);     // Arguments already provided in input
        var command = context.CommandInfo;
        var options = new List<AutoCompleteOption>();
        
        foreach (var arg in command.Arguments)
        {
            // Skip if already used
            if (usedArgs.Contains(arg.Name))
                continue;
            
            var fullName = $"--{arg.Name}";
            if (fullName.StartsWith($"--{queryString}", StringComparison.OrdinalIgnoreCase))
            {
                options.Add(new AutoCompleteOption(fullName, arg.Description));
            }
        }
        
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
```

#### Argument Alias Suggestions

When cursor is after single "-", handler filters and suggests unused argument aliases:

```csharp
/// <summary>
/// Implements IAutoCompleteHandler for consistency.
/// </summary>
public class ArgumentAliasHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context,
        CancellationToken cancellationToken = default)
    {
        var queryString = context.QueryString ?? "";  // Text after "-" (e.g., "t" from "-t")
        var usedArgs = GetUsedArguments(context);     // Arguments already provided (by name OR alias)
        var command = context.CommandInfo;
        var options = new List<AutoCompleteOption>();
        
        foreach (var arg in command.Arguments)
        {
            // Skip if already used
            if (usedArgs.Contains(arg.Name))
                continue;
            
            if (arg.Alias.HasValue)
            {
                var aliasStr = $"-{arg.Alias}";
                if (aliasStr.StartsWith($"-{queryString}", StringComparison.OrdinalIgnoreCase))
                {
                    options.Add(new AutoCompleteOption(aliasStr, $"{arg.Description} (alias for --{arg.Name})"));
                }
            }
        }
        
        return Task.FromResult(
            options.OrderBy(o => o.Value, StringComparer.OrdinalIgnoreCase).ToList());
    }
}
```

#### Used Argument Tracking

The system tracks which arguments have been provided to filter them from suggestions:

```csharp
public class UsedArgumentTracker
{
    public IReadOnlySet<string> GetUsedArguments(string input, CommandInfo command)
    {
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // Parse input tokens looking for:
        // 1. "--argName" or "--argName=value" → mark argName as used
        // 2. "-a" or "-a value" → resolve alias to name, mark as used
        // 3. Positional values → mark corresponding positional arg as used
        
        // Implementation uses same tokenization as command parser
        
        return used;
    }
}
```

#### Positional Parameter Handling

For positional parameters, determine which positional index the cursor is at:

```csharp
public ArgumentInfo? GetPositionalArgumentAtCursor(
    string input,
    int cursorPosition,
    CommandInfo command,
    IReadOnlySet<string> usedArgs)
{
    // 1. Count positional values before cursor
    // 2. Skip positional params that are satisfied by name (--name)
    // 3. Return the positional param at that index, if any
    
    // Rule: Once a named arg appears, no more positional values after it
    
    int positionalIndex = 0;
    foreach (var token in ParseTokensBeforeCursor(input, cursorPosition))
    {
        if (token.StartsWith("-"))
            break; // Named arg encountered, stop counting positionals
        
        positionalIndex++;
    }
    
    // Find the positional param at this index (excluding used ones)
    var positionalParams = command.Arguments
        .Where(a => a.IsPositional && !usedArgs.Contains(a.Name))
        .OrderBy(a => a.Position)
        .ToList();
    
    return positionalIndex < positionalParams.Count 
        ? positionalParams[positionalIndex] 
        : null;
}
```

### Legacy Removal (Complete)

**All existing autocomplete code is removed and reimplemented:**

| Removed | Location | Replacement |
|---------|----------|-------------|
| `AutoCompleteFunctionName` property | `ArgumentAttribute` | `[AutoComplete<T>]` attribute or Type Handler |
| `AutoCompleteFunctionName` property | `ArgumentInfo` | No longer needed |
| `IsAutoCompleteFunctionAsync` property | `ArgumentInfo` | No longer needed |
| `AutoCompleteContext` record | `AutoComplete/AutoCompleteContext.cs` | `AutoComplete/Handlers/AutoCompleteContext` class |
| Legacy function invocation code | `AutoCompleteOptionSetBuilder` | Handler system |
| Existing command syntax completion | `AutoCompleteOptionSetBuilder` | `AutoComplete/Syntax/` handlers |
| Existing argument name suggestions | `AutoCompleteOptionSetBuilder` | `ArgumentNameHandler` + `ArgumentAliasHandler` |
| Existing used-argument tracking | `AutoCompleteOptionSetBuilder` | `UsedArgumentTracker` |

### Usage Examples

```csharp
// Built-in Type Handlers (auto-registered)
[Command]
public class MyCommand : CommandBase
{
    [Argument]
    public LogLevel Level { get; set; }  // EnumAutoCompleteHandler.CanHandle(LogLevel) = true
    
    [Argument]
    public bool Verbose { get; set; }    // BooleanAutoCompleteHandler.CanHandle(bool) = true
}

// Custom Type Handler (added last, wins over built-in)
public class LogLevelHandler : ITypeAutoCompleteHandler
{
    public bool CanHandle(Type t) => t == typeof(LogLevel);  // Specific match
    
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        // Custom behavior - handler filters based on QueryString
        var query = context.QueryString ?? "";
        var options = new[] { "Debug", "Info", "Warn", "Error" }
            .Where(o => o.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(o => new AutoCompleteOption(o))
            .ToList();
        return Task.FromResult(options);
    }
}

// Attribute Handler (explicit reference, just implements core handler)
public class EnvironmentHandler : IAutoCompleteHandler  // NOT ITypeAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        // Handler filters based on QueryString
        var query = context.QueryString ?? "";
        return Task.FromResult(new[] { "dev", "staging", "prod" }
            .Where(e => e.StartsWith(query, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

[Command]
public class DeployCommand : CommandBase
{
    [Argument]
    [AutoComplete<EnvironmentHandler>]  // Explicit binding, compile-time verified
    public string Target { get; set; }
}

// Custom attribute (syntactic sugar)
public class EnvironmentAutoCompleteAttribute : AutoCompleteAttribute<EnvironmentHandler> { }

[Command]
public class BuildCommand : CommandBase
{
    [Argument]
    [EnvironmentAutoComplete]  // Cleaner than [AutoComplete<EnvironmentHandler>]
    public string Env { get; set; }
}
```

### Constants

```csharp
public static class AutoCompleteConstants
{
    public const int DefaultVisibleMenuItems = 5;
}
```

---

## Remote Command Autocomplete (User Story 5 - Priority P3)

*This section defines implementation for US5 - Remote Command Support. Deferred to a later phase after core handler system is complete.*

### Overview

Remote autocomplete allows commands registered on a server to provide autocomplete suggestions to the client. When a user triggers autocomplete for a remote command, the client sends an RPC request to the server, which executes the handler logic and returns suggestions.

### Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Client Side                                         │
├─────────────────────────────────────────────────────────────────────────────────┤
│  AutoCompleteOptionSetBuilder                                                   │
│    └─► Detects remote command (via CommandInfo.IsRemote)                        │
│          └─► Delegates to IServerProxy.GetAutoCompleteOptionsAsync()            │
└─────────────────────────────────────────────────────────────────────────────────┘
                                        │
                                        ▼ SignalR RPC
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Server Side                                         │
├─────────────────────────────────────────────────────────────────────────────────┤
│  CommandLineHub                                                                 │
│    └─► Receives autocomplete request                                            │
│          └─► AutoCompleteHandlerRegistry.GetHandler(argumentInfo)               │
│                └─► Handler.GetOptionsAsync(context)                             │
│                      └─► Returns List<AutoCompleteOption> to client             │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### RPC Message Envelope

Extends existing RPC infrastructure in `BitPantry.CommandLine.Remote.SignalR`:

```csharp
// Request (Client → Server)
public class AutoCompleteRequest : MessageBase
{
    public required string CommandName { get; init; }
    public required string ArgumentName { get; init; }
    public required string QueryString { get; init; }
    public required string FullInput { get; init; }      // Full input line for parity with local
    public required int CursorPosition { get; init; }    // Cursor position for parity with local
    public required IReadOnlyDictionary<string, string> ProvidedValues { get; init; }
}

// Response (Server → Client)
public class AutoCompleteResponse : MessageBase
{
    public required List<AutoCompleteOptionDto> Options { get; init; }
    public string? Error { get; init; }
}

public class AutoCompleteOptionDto
{
    public required string Value { get; init; }
    public string? Description { get; init; }
}
```

### Server Implementation

```csharp
// In CommandLineHub.cs
public async Task<AutoCompleteResponse> GetAutoCompleteOptionsAsync(
    AutoCompleteRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var command = _commandRegistry.GetCommand(request.CommandName);
        if (command == null)
            return new AutoCompleteResponse { Options = [], Error = "Command not found" };
        
        var argument = command.Arguments.FirstOrDefault(a => a.Name == request.ArgumentName);
        if (argument == null)
            return new AutoCompleteResponse { Options = [], Error = "Argument not found" };
        
        var handler = _handlerRegistry.GetHandler(argument);
        if (handler == null)
            return new AutoCompleteResponse { Options = [] };
        
        var context = new AutoCompleteContext
        {
            QueryString = request.QueryString,
            FullInput = request.FullInput,
            CursorPosition = request.CursorPosition,
            ArgumentInfo = argument,
            CommandInfo = command,
            ProvidedValues = BuildProvidedValues(request.ProvidedValues, command)
        };
        
        var options = await handler.GetOptionsAsync(context, cancellationToken);
        
        return new AutoCompleteResponse
        {
            Options = options.Select(o => new AutoCompleteOptionDto
            {
                Value = o.Value,
                Description = o.Description
            }).ToList()
        };
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Remote autocomplete failed for {Command}.{Arg}",
            request.CommandName, request.ArgumentName);
        return new AutoCompleteResponse { Options = [], Error = ex.Message };
    }
}
```

### Client Integration

```csharp
// In AutoCompleteOptionSetBuilder.cs
private async Task<List<AutoCompleteOption>> GetValueOptions(
    ParsedCommandElement parsedElement,
    CancellationToken token)
{
    var command = ResolveCommand(parsedElement);
    var argInfo = ResolveArgumentInfo(parsedElement);
    
    // Check if this is a remote command
    if (command?.IsRemote == true && _serverProxy != null)
    {
        return await GetRemoteValueOptions(command, argInfo, parsedElement, token);
    }
    
    // Local handler resolution (existing logic)
    var handler = _handlerRegistry.GetHandler(argInfo);
    // ...
}

private async Task<List<AutoCompleteOption>> GetRemoteValueOptions(
    CommandInfo command,
    ArgumentInfo argInfo,
    ParsedCommandElement parsedElement,
    CancellationToken token)
{
    try
    {
        var response = await _serverProxy.GetAutoCompleteOptionsAsync(
            new AutoCompleteRequest
            {
                CommandName = command.FullName,
                ArgumentName = argInfo.Name,
                QueryString = GetQueryString(parsedElement),
                FullInput = parsedElement.FullInput,
                CursorPosition = parsedElement.CursorPosition,
                ProvidedValues = GetProvidedValuesDict(parsedElement)
            },
            token);
        
        if (response.Error != null)
        {
            _logger.LogWarning("Remote autocomplete error: {Error}", response.Error);
            return [];
        }
        
        return response.Options
            .Select(o => new AutoCompleteOption(o.Value, o.Description))
            .ToList();
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        _logger.LogWarning(ex, "Remote autocomplete request failed");
        return [];
    }
}
```

### Error Handling

| Scenario | Client Behavior |
|----------|-----------------|
| Server disconnected | Return empty options, log warning |
| Request timeout | Cancel via CancellationToken, return empty |
| Server returns error | Log error, return empty options |
| Server throws exception | Caught on server, returns error message |

### Project Structure Additions

```text
BitPantry.CommandLine.Remote.SignalR/
├── Envelopes/
│   ├── AutoCompleteRequest.cs    # NEW
│   └── AutoCompleteResponse.cs   # NEW

BitPantry.CommandLine.Remote.SignalR.Server/
├── CommandLineHub.cs             # MODIFY: Add GetAutoCompleteOptionsAsync

BitPantry.CommandLine.Remote.SignalR.Client/
├── SignalRServerProxy.cs         # MODIFY: Add GetAutoCompleteOptionsAsync

BitPantry.CommandLine.Tests.Remote.SignalR/
├── AutoComplete/                 # NEW
│   └── RemoteAutoCompleteTests.cs
```

### Test Cases (Remote)

| ID | Test Case | Description |
|----|-----------|-------------|
| REM-001 | Remote enum autocomplete | Remote command with enum arg returns server-side enum values |
| REM-002 | Remote custom handler | Remote command uses server-registered custom handler |
| REM-003 | Server disconnected | Client gracefully handles disconnection (empty options) |
| REM-004 | Request cancellation | Typing new char cancels pending remote request |
| REM-005 | Server exception | Server handler throws, client receives empty options |
| REM-006 | Context forwarding | Server receives ProvidedValues, FullInput, CursorPosition from client |

### Dependencies

- Requires core handler system complete (US1, US2, US3)
- Requires existing SignalR RPC infrastructure
- Requires `IServerProxy` interface extension

### Implementation Order

1. Define RPC message envelopes
2. Implement server-side hub method
3. Extend `IServerProxy` interface and client implementation
4. Integrate into `AutoCompleteOptionSetBuilder`
5. Add integration tests using existing SignalR test infrastructure
