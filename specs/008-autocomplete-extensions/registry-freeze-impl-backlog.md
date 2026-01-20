# Registry Freeze Pattern - Implementation Complete ✅

## Summary

The registry freeze pattern has been fully implemented. The `CommandRegistry` is now split into:

- **`ICommandRegistryBuilder` / `CommandRegistryBuilder`** — Mutable builder used during configuration
- **`ICommandRegistry` / `CommandRegistry`** — Immutable runtime registry (with exception for remote commands)

---

## Implemented Design

### Phase Separation

| Phase | Interface | Class | Purpose |
|-------|-----------|-------|---------|
| **Configuration** | `ICommandRegistryBuilder` | `CommandRegistryBuilder` | Register commands/groups, set options |
| **Runtime** | `ICommandRegistry` | `CommandRegistry` | Resolve commands, immutable local commands |

### Key Design Decisions

#### 1. DI Registration in Build()

The `Build(IServiceCollection services)` method performs three operations atomically:
1. Validates the registry (empty groups, name collisions, reserved names)
2. Registers all command types with DI as transient services
3. Returns the frozen immutable registry

```csharp
public ICommandRegistry Build(IServiceCollection services)
{
    ThrowIfBuilt();
    Validate();
    
    foreach (var cmd in _commands)
        services.AddTransient(cmd.Type);
    
    _isBuilt = true;
    return new CommandRegistry(_commands, _groups, CaseSensitive);
}
```

A parameterless `Build()` overload exists for testing scenarios where DI registration isn't needed.

#### 2. Remote Command Mutation (Option C)

The `CommandRegistry` internally holds separate collections for local and remote commands:

```csharp
public class CommandRegistry : ICommandRegistry
{
    private readonly List<CommandInfo> _localCommands;   // Immutable after build
    private readonly List<GroupInfo> _localGroups;       // Immutable after build
    private readonly List<CommandInfo> _remoteCommands;  // Mutable for server connections
    private readonly List<GroupInfo> _remoteGroups;      // Mutable for server connections
    
    public IReadOnlyCollection<CommandInfo> Commands => 
        _localCommands.Concat(_remoteCommands).ToList().AsReadOnly();
}
```

This allows:
- Local commands to be truly immutable after `Build()`
- Remote commands to be added/removed dynamically via `RegisterCommandsAsRemote()` and `DropRemoteCommands()`

#### 3. CommandRegistryApplicationBuilder Retained

The `CommandRegistryApplicationBuilder<TType>` base class was retained to:
- Provide fluent API with CRTP pattern for method chaining
- Provide assembly scanning via `RegisterCommands(params Type[])`
- Allow future extensibility for feature builders

The `CommandRegistry` property was marked `[Obsolete]` with guidance to use `CommandRegistryBuilder` instead.

---

## File Structure

### New Files

| File | Purpose |
|------|---------|
| `ICommandRegistry.cs` | Runtime interface for command resolution |
| `ICommandRegistryBuilder.cs` | Builder interface for configuration phase |
| `CommandRegistryBuilder.cs` | Mutable builder implementation |

### Modified Files

| File | Changes |
|------|---------|
| `CommandRegistry.cs` | Made immutable with internal constructor, split local/remote collections |
| `CommandRegistryApplicationBuilder.cs` | Exposes `CommandRegistryBuilder`, deprecated `CommandRegistry` property |
| `CommandLineApplicationBuilder.cs` | Calls `CommandRegistryBuilder.Build(Services)` |

---

## Interface Definitions

### ICommandRegistry (Runtime)

```csharp
public interface ICommandRegistry
{
    IReadOnlyCollection<CommandInfo> Commands { get; }
    IReadOnlyList<GroupInfo> Groups { get; }
    IReadOnlyList<CommandInfo> RootCommands { get; }
    IReadOnlyList<GroupInfo> RootGroups { get; }
    bool CaseSensitive { get; }
    
    GroupInfo FindGroup(string nameOrPath);
    CommandInfo FindCommand(string name, GroupInfo group = null);
    CommandInfo Find(string fullyQualifiedCommandName);
    
    // Remote command management (runtime mutation for server proxy)
    void RegisterCommandsAsRemote(IReadOnlyList<CommandInfo> infos);
    void DropRemoteCommands();
}
```

### ICommandRegistryBuilder (Configuration)

```csharp
public interface ICommandRegistryBuilder
{
    bool ReplaceDuplicateCommands { get; set; }
    bool CaseSensitive { get; set; }
    
    GroupInfo RegisterGroup(string path);
    void RegisterCommand(Type commandType);
    
    ICommandRegistry Build(IServiceCollection services);
    ICommandRegistry Build();  // For testing without DI
}
```

---

## Validation Rules

The `Validate()` method in `CommandRegistryBuilder` enforces:

1. **FR-022: Empty group validation** — Groups must have at least one command or subgroup
2. **Name collision detection** — Commands and groups at the same level cannot share names
3. **FR-027: Reserved name validation** — Arguments cannot be named "help" or use alias 'h'

---

## Usage Examples

### Application Builder

```csharp
var builder = new CommandLineApplicationBuilder()
    .RegisterCommand<MyCommand>()
    .RegisterCommands(typeof(SomeAssemblyMarker));

var app = builder.Build();  // Internally calls CommandRegistryBuilder.Build(Services)
```

### Direct Builder Usage (Tests)

```csharp
var builder = new CommandRegistryBuilder();
builder.RegisterCommand<TestCommand>();
var registry = builder.Build();  // Uses parameterless overload
```

### With DI Registration

```csharp
var services = new ServiceCollection();
var builder = new CommandRegistryBuilder();
builder.RegisterCommand<TestCommand>();
var registry = builder.Build(services);  // Registers TestCommand as transient
```

---

## Migration Notes

All consumers updated to use `ICommandRegistry` at runtime:

- `CommandLineApplicationCore`
- `CommandResolver`
- `HelpHandler` / `IHelpFormatter` / `HelpFormatter`
- `AutoCompleteOptionSetBuilder`
- `CommandExecutionContext`
- `SignalRServerProxy`
- `ServerLogic`
- `ClientLogic`

All test files updated to use `CommandRegistryBuilder` instead of direct `CommandRegistry` instantiation.

---

## Benefits Achieved

| Aspect | Before (Flag) | After (Freeze) |
|--------|---------------|----------------|
| **Type safety** | Runtime exception on late registration | Compile-time: can't call register on `ICommandRegistry` |
| **Intent clarity** | Implicit state via `_areServicesConfigured` flag | Explicit `.Build()` call |
| **Separation of concerns** | One class, two modes | Builder for config, Registry for runtime |
| **Testing** | Must set up full registry | Can mock `ICommandRegistry` easily |
| **DI timing** | Separate `ConfigureServices()` call | Atomic in `Build(services)` |

---

## Action Items - COMPLETED

- [x] Define `ICommandRegistry` interface
- [x] Define `ICommandRegistryBuilder` interface  
- [x] Implement `CommandRegistryBuilder` (mutable)
- [x] Implement `CommandRegistry` (immutable with remote mutation)
- [x] Update `CommandLineApplicationBuilder`
- [x] Update `CommandRegistryApplicationBuilder<T>` base class
- [x] Update all runtime consumers to use `ICommandRegistry`
- [x] Update tests
- [x] Move `ConfigureServices` logic into `Build(IServiceCollection)`
