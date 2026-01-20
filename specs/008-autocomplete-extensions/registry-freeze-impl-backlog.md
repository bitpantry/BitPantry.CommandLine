# Backlog: Registry Freeze Pattern

## Current State

The `CommandRegistry` uses a boolean flag `_areServicesConfigured` to enforce immutability after the DI container is built:

```csharp
public class CommandRegistry
{
    private bool _areServicesConfigured = false;

    public void RegisterCommand(Type type)
    {
        if (_areServicesConfigured)
            throw new InvalidOperationException(
                "Services have already been configured for this registry. Add commands before configuring services");
        
        // ... registration logic ...
    }

    public void ConfigureServices(IServiceCollection services)
    {
        Validate();
        foreach (var cmd in _commands)
        {
            if (!cmd.IsRemote)
                services.AddTransient(cmd.Type);
        }
        _areServicesConfigured = true;
    }
}
```

---

## Analysis: Is This Legitimate?

**Yes.** The flag enforces a real constraint inherent to DI containers:

```
Register types → Build container → Resolve types
       ↑                               ↓
       └─────── Cannot add here ───────┘
```

Without this guard, a user could call:

```csharp
var app = builder.Build();
builder.RegisterCommand<LateCommand>();  // In registry but NOT in DI → runtime explosion
```

### Why Two-Stage Registration Exists

The separation between `RegisterCommand()` and `ConfigureServices()` is **intentional** for three reasons:

| Reason | Requirement |
|--------|-------------|
| **Cross-command validation** | `Validate()` checks empty groups, name collisions, reserved names — needs complete list |
| **Remote commands** | Commands from SignalR server register metadata only; `IsRemote` types are NOT added to DI (class doesn't exist locally) |
| **Group hierarchy linking** | Commands link to groups bidirectionally; must complete before locking container |

---

## Current Issues

1. **Mutable state** — The registry is a mutable object that changes behavior based on internal flag
2. **Implicit contract** — Users must know not to register after `Build()`, enforced only at runtime
3. **Flag-based logic** — `if (_areServicesConfigured)` checks scattered through the code

---

## Proposed Solution: Freeze Pattern

Replace the mutable registry with an explicit build step that returns an immutable interface:

### Registry Interfaces

```csharp
/// <summary>
/// Mutable registry used during builder configuration phase.
/// </summary>
public interface ICommandRegistryBuilder
{
    void RegisterCommand<T>() where T : CommandBase;
    void RegisterCommand(Type type);
    void RegisterGroup<T>();
    void RegisterGroup(Type type);
    void RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos);
    
    /// <summary>
    /// Freezes the registry, performs validation, and returns an immutable view.
    /// </summary>
    ICommandRegistry Build(IServiceCollection services);
}

/// <summary>
/// Immutable registry used at runtime for command resolution.
/// </summary>
public interface ICommandRegistry
{
    IReadOnlyList<CommandInfo> Commands { get; }
    IReadOnlyList<GroupInfo> Groups { get; }
    IReadOnlyList<CommandInfo> RootCommands { get; }
    IReadOnlyList<GroupInfo> RootGroups { get; }
    
    CommandInfo? Resolve(string[] commandPath);
    // ... other read-only operations ...
}
```

### Implementation

```csharp
public class CommandRegistryBuilder : ICommandRegistryBuilder
{
    private readonly List<CommandInfo> _commands = new();
    private readonly List<GroupInfo> _groups = new();
    private bool _isBuilt = false;

    public void RegisterCommand(Type type)
    {
        if (_isBuilt)
            throw new InvalidOperationException("Registry has already been built.");
        
        // ... registration logic ...
    }

    public ICommandRegistry Build(IServiceCollection services)
    {
        if (_isBuilt)
            throw new InvalidOperationException("Registry has already been built.");
        
        Validate();
        
        foreach (var cmd in _commands.Where(c => !c.IsRemote))
            services.AddTransient(cmd.Type);
        
        _isBuilt = true;
        
        return new CommandRegistry(_commands, _groups);  // Immutable snapshot
    }
}

public class CommandRegistry : ICommandRegistry
{
    private readonly IReadOnlyList<CommandInfo> _commands;
    private readonly IReadOnlyList<GroupInfo> _groups;

    internal CommandRegistry(List<CommandInfo> commands, List<GroupInfo> groups)
    {
        _commands = commands.AsReadOnly();
        _groups = groups.AsReadOnly();
    }

    public IReadOnlyList<CommandInfo> Commands => _commands;
    // ... read-only accessors ...
}
```

### Builder Integration

```csharp
public class CommandLineApplicationBuilder
{
    public ICommandRegistryBuilder CommandRegistryBuilder { get; } = new CommandRegistryBuilder();

    public CommandLineApplicationBuilder RegisterCommand<T>() where T : CommandBase
    {
        CommandRegistryBuilder.RegisterCommand<T>();
        return this;
    }

    public CommandLineApplication Build()
    {
        // Build freezes the registry AND registers with DI in one atomic operation
        ICommandRegistry registry = CommandRegistryBuilder.Build(Services);
        
        var svcProvider = Services.BuildServiceProvider();
        
        // Runtime code only sees immutable ICommandRegistry
        var core = new CommandLineApplicationCore(registry, ...);
        
        return new CommandLineApplication(...);
    }
}
```

---

## Benefits

| Aspect | Current (Flag) | Proposed (Freeze) |
|--------|----------------|-------------------|
| **Type safety** | Runtime exception | Compile-time: can't call `RegisterCommand` on `ICommandRegistry` |
| **Intent clarity** | Implicit state transition | Explicit `.Build()` call |
| **Separation of concerns** | One class, two modes | Builder for config, Registry for runtime |
| **Testing** | Must set up full registry | Can mock `ICommandRegistry` easily |

---

## Migration Path

1. Extract `ICommandRegistry` interface with read-only operations
2. Create `ICommandRegistryBuilder` interface with mutation operations
3. Split current `CommandRegistry` into `CommandRegistryBuilder` + immutable `CommandRegistry`
4. Update `CommandLineApplicationBuilder` to expose `ICommandRegistryBuilder`
5. Update consumers to use `ICommandRegistry` at runtime

---

## Action Items

- [ ] Define `ICommandRegistry` interface
- [ ] Define `ICommandRegistryBuilder` interface
- [ ] Implement `CommandRegistryBuilder` (mutable)
- [ ] Implement `CommandRegistry` (immutable)
- [ ] Update `CommandLineApplicationBuilder`
- [ ] Update `CommandRegistryApplicationBuilder<T>` base class
- [ ] Update all runtime consumers to use `ICommandRegistry`
- [ ] Update tests

---

## Priority

**Low** — The current flag-based approach works correctly and fails fast with clear errors. This refactor improves design purity but doesn't fix bugs. Consider tackling after higher-priority items (scope leak, autocomplete handler registration).
