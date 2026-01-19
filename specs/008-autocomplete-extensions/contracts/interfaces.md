# Internal Contracts: Extension-Based Autocomplete System

**Feature**: 008-autocomplete-extensions  
**Date**: January 18, 2026

This document defines the internal interfaces and contracts for the autocomplete provider system.

---

## Terminology

| Term | Definition |
|------|------------|
| **Handler** | Any class implementing `IAutoCompleteHandler` (core capability) |
| **Type Provider** | Handler implementing `ITypeAutoCompleteProvider` with runtime matching |
| **Attribute Provider** | Handler explicitly referenced via `[AutoComplete]` attribute |

---

## Core Interface

### IAutoCompleteHandler.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Core interface for autocomplete capability.
    /// Used directly by Attribute Providers (explicit binding via [AutoComplete] attribute).
    /// Type Providers extend this with CanHandle for runtime matching.
    /// </summary>
    public interface IAutoCompleteHandler
    {
        /// <summary>
        /// Gets autocomplete options for the argument.
        /// </summary>
        Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context, 
            CancellationToken cancellationToken = default);
    }
}
```

---

## Type Provider Interface

### ITypeAutoCompleteProvider.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Extended interface for Type Providers with runtime type matching.
    /// Matching logic is encapsulated in CanHandle method.
    /// </summary>
    public interface ITypeAutoCompleteProvider : IAutoCompleteHandler
    {
        /// <summary>
        /// Determines if this provider can handle the given argument type.
        /// Called at runtime to find matching provider.
        /// </summary>
        /// <param name="argumentType">The CLR type of the argument (nullable unwrapped)</param>
        /// <returns>True if this provider can handle the type</returns>
        bool CanHandle(Type argumentType);
    }
}
```

---

## Attribute

### IAutoCompleteAttribute.cs (Marker Interface)

```csharp
namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marker interface for AutoComplete attributes.
    /// Enables reflection-based discovery of generic attributes.
    /// </summary>
    public interface IAutoCompleteAttribute
    {
        Type HandlerType { get; }
    }
}
```

### AutoCompleteAttribute.cs

```csharp
namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Explicitly specifies which handler to use for this argument (Attribute Provider).
    /// Overrides any Type Provider.
    /// Generic constraint provides compile-time type safety.
    /// Supports inheritance for syntactic-sugar custom attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class AutoCompleteAttribute<THandler> : Attribute, IAutoCompleteAttribute
        where THandler : IAutoCompleteHandler
    {
        public Type HandlerType => typeof(THandler);
    }
}
```

**Usage Examples**:
```csharp
// Direct usage with compile-time verification
[AutoComplete<EnvironmentHandler>]  // ✅ Compiles only if EnvironmentHandler : IAutoCompleteHandler
public string Target { get; set; }

[AutoComplete<string>]  // ❌ Compile error: string doesn't implement IAutoCompleteHandler
public string Bad { get; set; }

// Syntactic sugar via inheritance
public class EnvironmentAutoCompleteAttribute : AutoCompleteAttribute<EnvironmentHandler> { }
```

---

## Context

### AutoCompleteContext.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Context information passed to autocomplete handlers.
    /// </summary>
    public class AutoCompleteContext
    {
        /// <summary>Current partial input being typed.</summary>
        public required string QueryString { get; init; }
        
        /// <summary>Complete input line.</summary>
        public required string FullInput { get; init; }
        
        /// <summary>Cursor position (1-based).</summary>
        public required int CursorPosition { get; init; }
        
        /// <summary>Metadata about argument being completed.</summary>
        public required ArgumentInfo ArgumentInfo { get; init; }
        
        /// <summary>Already-provided argument values.</summary>
        public required IReadOnlyDictionary<ArgumentInfo, string> ProvidedValues { get; init; }
        
        /// <summary>Command being executed.</summary>
        public required CommandInfo CommandInfo { get; init; }
    }
}
```

---

## Provider Registry

### AutoCompleteProviderRegistry.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Registry for Type Providers.
    /// Iterates providers and asks who can handle the type.
    /// Last registered wins (like command overrides).
    /// </summary>
    public class AutoCompleteProviderRegistry
    {
        private readonly List<ITypeAutoCompleteProvider> _typeProviders = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoCompleteProviderRegistry> _logger;
        
        public AutoCompleteProviderRegistry(
            IServiceProvider serviceProvider,
            ILogger<AutoCompleteProviderRegistry> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        
        /// <summary>
        /// Registers a Type Provider. Added to end of list (last wins).
        /// </summary>
        public void Register(ITypeAutoCompleteProvider provider)
        {
            _typeProviders.Add(provider);
            _logger.LogDebug("Registered Type Provider {Type}", provider.GetType().Name);
        }
        
        /// <summary>
        /// Gets the handler for an argument.
        /// 1. Check for Attribute Provider ([AutoComplete<T>] attribute)
        /// 2. Ask Type Providers who can handle (last registered wins)
        /// 3. Return null if no provider found
        /// </summary>
        public IAutoCompleteHandler? GetHandler(ArgumentInfo argumentInfo)
        {
            // 1. Check for Attribute Provider (explicit binding via marker interface)
            var explicitAttr = argumentInfo.PropertyInfo
                .GetCustomAttributes(inherit: true)
                .OfType<IAutoCompleteAttribute>()
                .FirstOrDefault();
            
            if (explicitAttr != null)
            {
                _logger.LogDebug("Using Attribute Provider {Type} for {Arg}",
                    explicitAttr.HandlerType.Name, argumentInfo.Name);
                return (IAutoCompleteHandler)_serviceProvider
                    .GetRequiredService(explicitAttr.HandlerType);
            }
            
            // 2. Find Type Provider (last registered wins)
            var type = argumentInfo.PropertyInfo.PropertyType;
            var lookupType = Nullable.GetUnderlyingType(type) ?? type;
            
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
}
```

---

## Built-in Type Providers

### EnumAutoCompleteProvider.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Type Provider for ALL enum types.
    /// Single instance handles all enums via CanHandle.
    /// </summary>
    public class EnumAutoCompleteProvider : ITypeAutoCompleteProvider
    {
        public bool CanHandle(Type type) => type.IsEnum;
        
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var enumType = context.ArgumentInfo.PropertyType;
            var actualType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            
            var options = Enum.GetNames(actualType)
                .Where(n => n.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Select(n => new AutoCompleteOption(n))
                .ToList();
                
            return Task.FromResult(options);
        }
    }
}
```

### BooleanAutoCompleteProvider.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Type Provider for boolean arguments.
    /// </summary>
    public class BooleanAutoCompleteProvider : ITypeAutoCompleteProvider
    {
        public bool CanHandle(Type type) => type == typeof(bool);
        
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var options = new[] { "false", "true" }
                .Where(o => o.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
                .Select(o => new AutoCompleteOption(o))
                .ToList();
                
            return Task.FromResult(options);
        }
    }
}
```

---

## Registration Extensions

### AutoCompleteServiceCollectionExtensions

The autocomplete system has its own DI registration, separate from `CommandRegistry`:

```csharp
namespace BitPantry.CommandLine.AutoComplete
{
    public static class AutoCompleteServiceCollectionExtensions
    {
        /// <summary>
        /// Adds autocomplete services with built-in providers only.
        /// </summary>
        public static IServiceCollection AddAutoComplete(this IServiceCollection services)
        {
            services.AddSingleton<AutoCompleteProviderRegistry>();
            return services;
        }
        
        /// <summary>
        /// Adds autocomplete services with custom configuration.
        /// </summary>
        public static IServiceCollection AddAutoComplete(
            this IServiceCollection services,
            Action<AutoCompleteProviderRegistry> configure)
        {
            services.AddSingleton<AutoCompleteProviderRegistry>(sp =>
            {
                var registry = new AutoCompleteProviderRegistry(
                    sp,
                    sp.GetRequiredService<ILogger<AutoCompleteProviderRegistry>>());
                configure(registry);
                return registry;
            });
            return services;
        }
    }
}
```

### AutoCompleteProviderRegistry (Standalone)

```csharp
namespace BitPantry.CommandLine.AutoComplete.Providers
{
    /// <summary>
    /// Standalone registry for autocomplete providers.
    /// Separate from CommandRegistry - follows similar patterns but independent.
    /// </summary>
    public class AutoCompleteProviderRegistry
    {
        private readonly List<ITypeAutoCompleteProvider> _typeProviders = new();
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoCompleteProviderRegistry> _logger;
        
        public AutoCompleteProviderRegistry(
            IServiceProvider serviceProvider,
            ILogger<AutoCompleteProviderRegistry> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            // Register built-in Type Providers
            Register(new EnumAutoCompleteProvider());
            Register(new BooleanAutoCompleteProvider());
        }
        
        public void Register(ITypeAutoCompleteProvider provider)
        {
            _typeProviders.Add(provider);
            _logger.LogDebug("Registered Type Provider {Type}", provider.GetType().Name);
        }
        
        public void RegisterFromAssemblies(params Assembly[] assemblies)
        {
            foreach (var assembly in assemblies)
            {
                var providerTypes = assembly.GetTypes()
                    .Where(t => typeof(ITypeAutoCompleteProvider).IsAssignableFrom(t)
                             && !t.IsAbstract
                             && !t.IsInterface
                             && t != typeof(EnumAutoCompleteProvider)
                             && t != typeof(BooleanAutoCompleteProvider));
                
                foreach (var providerType in providerTypes)
                {
                    var provider = (ITypeAutoCompleteProvider)ActivatorUtilities
                        .CreateInstance(_serviceProvider, providerType);
                    Register(provider);  // Added last, wins for matching types
                }
            }
        }
        
        public void RegisterFromAssemblies<T>()
        {
            RegisterFromAssemblies(typeof(T).Assembly);
        }
        
        // GetHandler implementation as defined in plan.md...
    }
}
```

---

## Constants

### AutoCompleteConstants.cs

```csharp
namespace BitPantry.CommandLine.AutoComplete
{
    public static class AutoCompleteConstants
    {
        public const int DefaultVisibleMenuItems = 5;
    }
}
```

---

## Usage Examples

### Type Provider (Runtime Matching)

```csharp
// Single EnumAutoCompleteProvider handles all enums
public class EnumAutoCompleteProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t.IsEnum;  // Matches LogLevel, Severity, etc.
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// Custom provider for specific type
public class LogLevelProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t == typeof(LogLevel);  // Only LogLevel
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}
```

### Attribute Provider (Explicit Binding)

```csharp
// Handler implements only core interface - no CanHandle needed
public class EnvironmentHandler : IAutoCompleteHandler
{
    public Task<List<AutoCompleteOption>> GetOptionsAsync(
        AutoCompleteContext context, CancellationToken ct)
    {
        return Task.FromResult(new[] { "dev", "staging", "prod" }
            .Where(e => e.StartsWith(context.QueryString, StringComparison.OrdinalIgnoreCase))
            .Select(e => new AutoCompleteOption(e))
            .ToList());
    }
}

[Command]
public class DeployCommand : CommandBase
{
    [Argument]
    [AutoComplete(typeof(EnvironmentHandler))]  // Explicit binding
    public string Target { get; set; }
}
```

### Syntactic Sugar

```csharp
public class EnvironmentAutoCompleteAttribute : AutoCompleteAttribute
{
    public EnvironmentAutoCompleteAttribute() : base(typeof(EnvironmentHandler)) { }
}

[Command]
public class DeployCommand : CommandBase
{
    [Argument]
    [EnvironmentAutoComplete]  // Cleaner syntax
    public string Target { get; set; }
}
```

### Using Type Provider as Attribute Provider

```csharp
// Type Provider can also be used via attribute
public class LogLevelProvider : ITypeAutoCompleteProvider
{
    public bool CanHandle(Type t) => t == typeof(LogLevel);
    public Task<List<AutoCompleteOption>> GetOptionsAsync(...) { ... }
}

// Used as Type Provider (auto-matched):
[Argument]
public LogLevel Level { get; set; }

// OR used via attribute (ignores CanHandle):
[Argument]
[AutoComplete(typeof(LogLevelProvider))]
public string CustomLevel { get; set; }
```
