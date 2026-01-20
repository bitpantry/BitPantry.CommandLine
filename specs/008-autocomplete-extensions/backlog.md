# Backlog: Registration & Activation Code Smells

## Problem Statement

The current patterns for registering and activating both commands and handlers exhibit code smells that make the API unintuitive and error-prone.

---

## Smell #1: Split Registration

### Current Pattern
Types are registered in two places:
1. With the registry (`CommandRegistry.Register<T>()`, `AutoCompleteHandlerRegistry.Register<T>()`)
2. With the DI container (`services.AddTransient<T>()`)

```csharp
// Registration happens here...
builder.RegisterCommand<MyCommand>();

// ...but DI configuration happens separately
builder.ConfigureServices(services => {
    services.AddTransient<MyCommand>();
    services.AddTransient<MyHandler>();
});
```

### Why It Smells
- **Duplication**: Same type referenced in two places
- **Easy to forget**: Register with one but not the other → runtime failures
- **No single source of truth**: Registry knows about types, DI knows about types, but they're not connected

---

## Smell #2: Service Locator Pattern

### Current Pattern
The registry receives `IServiceProvider` and calls `GetRequiredService<T>()` at runtime to resolve instances.

```csharp
public class AutoCompleteHandlerRegistry
{
    private IServiceProvider _serviceProvider;
    
    public void SetServiceProvider(IServiceProvider sp) => _serviceProvider = sp;
    
    public ITypeAutoCompleteHandler? GetHandler(ArgumentInfo info)
    {
        foreach (var type in _typeHandlers.Reverse())
        {
            var handler = (ITypeAutoCompleteHandler)_serviceProvider.GetRequiredService(type);
            if (handler.CanHandle(info)) return handler;
        }
        return null;
    }
}
```

### Why It Smells
- **Service Locator anti-pattern**: Dependencies are hidden, not declared in constructor
- **Two-phase initialization**: Object isn't fully constructed until `SetServiceProvider()` is called
- **Temporal coupling**: Must call `SetServiceProvider()` before `GetHandler()` or runtime explosion
- **Testing friction**: Tests must set up DI infrastructure even for simple cases

---

## Smell #3: Lifecycle Confusion

### Current Pattern
```
Register types → Build app → Set service provider → Runtime resolution
```

This multi-phase lifecycle is:
- Hard to discover (not obvious from API)
- Easy to call methods in wrong order
- Requires understanding internal state machine

---

## Recommended Approach: Unified Registration with Factory Pattern

### Option A: Registry Owns DI Registration

The registry receives `IServiceCollection` during configuration and handles both tracking and DI registration:

```csharp
public class AutoCompleteHandlerRegistry
{
    private readonly List<Type> _handlerTypes = new();
    
    // Called during configuration phase
    public void Register<T>(IServiceCollection services) where T : class, ITypeAutoCompleteHandler
    {
        _handlerTypes.Add(typeof(T));
        services.AddTransient<T>();
    }
}
```

**Pros**: Single call does both jobs  
**Cons**: Registry now depends on `IServiceCollection` (Microsoft.Extensions.DependencyInjection)

### Option B: Handler Factory Injection

Instead of service locator, inject a factory that the DI container provides:

```csharp
public class AutoCompleteHandlerRegistry
{
    private readonly Func<Type, ITypeAutoCompleteHandler> _handlerFactory;
    
    public AutoCompleteHandlerRegistry(Func<Type, ITypeAutoCompleteHandler> handlerFactory)
    {
        _handlerFactory = handlerFactory;
    }
    
    public ITypeAutoCompleteHandler? GetHandler(ArgumentInfo info)
    {
        foreach (var type in _handlerTypes.Reverse())
        {
            var handler = _handlerFactory(type);
            if (handler.CanHandle(info)) return handler;
        }
        return null;
    }
}

// Registration:
services.AddSingleton<Func<Type, ITypeAutoCompleteHandler>>(sp => 
    type => (ITypeAutoCompleteHandler)sp.GetRequiredService(type));
```

**Pros**: Constructor injection, no temporal coupling  
**Cons**: Func<> factory pattern can be confusing

### Option C: Activator Pattern (Current CommandActivator approach)

Separate the registry (knows what) from the activator (knows how to create):

```csharp
public class AutoCompleteHandlerRegistry
{
    public IReadOnlyList<Type> HandlerTypes => _handlerTypes;
    public void Register<T>() => _handlerTypes.Add(typeof(T));
}

public class AutoCompleteHandlerActivator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly AutoCompleteHandlerRegistry _registry;
    
    public AutoCompleteHandlerActivator(
        IServiceProvider serviceProvider,
        AutoCompleteHandlerRegistry registry)
    {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }
    
    public ITypeAutoCompleteHandler? GetHandler(ArgumentInfo info)
    {
        foreach (var type in _registry.HandlerTypes.Reverse())
        {
            var handler = (ITypeAutoCompleteHandler)_serviceProvider.GetRequiredService(type);
            if (handler.CanHandle(info)) return handler;
        }
        return null;
    }
}
```

**Pros**: 
- Clear separation of concerns
- Activator gets proper constructor injection
- Registry stays simple (just a list of types)
- Follows existing CommandRegistry/CommandActivator pattern

**Cons**: 
- Still need to register handler types with DI separately
- More classes to manage

### Option D: Builder Extension Methods (Recommended)

Combine Option C with fluent builder extensions that handle both registration steps:

```csharp
public static class AutoCompleteBuilderExtensions
{
    public static CommandLineApplicationBuilder AddAutoCompleteHandler<T>(
        this CommandLineApplicationBuilder builder) 
        where T : class, ITypeAutoCompleteHandler
    {
        // Register with the handler registry
        builder.HandlerRegistry.Register<T>();
        
        // Also configure DI
        builder.ConfigureServices(services => services.AddTransient<T>());
        
        return builder;
    }
}

// Usage - single call does both:
builder.AddAutoCompleteHandler<EnumAutoCompleteHandler>();
```

**Pros**:
- Single method call for users
- Implementation details hidden
- Follows existing patterns (`AddCommand<T>()` if it exists)
- Extension method keeps core clean

**Cons**: 
- Requires builder infrastructure to support this pattern

---

## Action Items

1. [ ] Refactor `AutoCompleteHandlerRegistry` to separate concerns (Option C)
2. [ ] Create `AutoCompleteHandlerActivator` with proper DI
3. [ ] Add builder extension methods (Option D) for clean API
4. [ ] Apply same pattern to `CommandRegistry` / `CommandActivator` if not already
5. [ ] Update tests to use the cleaner patterns

---

## References

- [Service Locator is an Anti-Pattern](https://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/) - Mark Seemann
- [Composition Root](https://blog.ploeh.dk/2011/07/28/CompositionRoot/) - Mark Seemann
