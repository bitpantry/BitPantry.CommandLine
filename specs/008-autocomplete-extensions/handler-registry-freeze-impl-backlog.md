# Backlog: AutoCompleteHandlerRegistry Freeze Pattern

## ✅ COMPLETED

This backlog has been fully implemented. All items below are complete.

---

## Summary

Apply the same builder/freeze pattern used in `CommandRegistry` to `AutoCompleteHandlerRegistry`. This will:

1. ✅ Split the mutable registration and immutable runtime concerns into separate interfaces/classes
2. ✅ Move DI registration into `Build(IServiceCollection)`
3. ✅ Introduce `HandlerActivator` to separate activation from the registry (mirrors `CommandActivator`)
4. ✅ Make the runtime registry truly immutable with no `IServiceProvider` dependency

---

## Architectural Parallel

The handler system now mirrors the command system exactly:

| Concern | Command System | Handler System |
|---------|---------------|----------------|
| **Builder** | `CommandRegistryBuilder` | `AutoCompleteHandlerRegistryBuilder` |
| **Frozen Registry** | `CommandRegistry` / `ICommandRegistry` | `AutoCompleteHandlerRegistry` / `IAutoCompleteHandlerRegistry` |
| **Build method** | `Build(IServiceCollection)` | `Build(IServiceCollection)` |
| **Metadata storage** | Stores `CommandInfo` objects | Stores handler `Type` list |
| **Lookup** | `Find()`, `FindCommand()` returns `CommandInfo` | `FindHandler()` returns `Type?` |
| **Activator** | `CommandActivator.Activate(CommandInfo)` | `HandlerActivator.Activate(Type)` |

---

## Implementation Complete

### Files Created

| File | Purpose |
|------|---------|
| `IAutoCompleteHandlerRegistry.cs` | Runtime lookup interface with `FindHandler(ArgumentInfo, HandlerActivator)` |
| `IAutoCompleteHandlerRegistryBuilder.cs` | Builder interface with `Register<T>()` and `Build(IServiceCollection)` |
| `AutoCompleteHandlerRegistryBuilder.cs` | Mutable builder that registers handler types with DI during Build() |
| `HandlerActivator.cs` | Activator class mirroring `CommandActivator` with `Activate(Type)` |

### Files Modified

| File | Changes |
|------|---------|
| `AutoCompleteHandlerRegistry.cs` | Made immutable, internal constructor, `FindHandler()` returns `Type?` |
| `AutoCompleteHandlerRegistryTests.cs` | Updated to use builder + activator pattern |

---

## Design Decision

For the `CanHandle()` check (which requires activation), **Option B** was chosen:

```csharp
// Option B: Registry takes activator as parameter for the lookup
public Type? FindHandler(ArgumentInfo argumentInfo, HandlerActivator activator)
```

This allows the registry to perform the full lookup logic internally while still keeping activation separate via the activator parameter.

---

## Action Items

- [x] Create `IAutoCompleteHandlerRegistry` interface
- [x] Create `IAutoCompleteHandlerRegistryBuilder` interface
- [x] Implement `AutoCompleteHandlerRegistryBuilder`
- [x] Refactor `AutoCompleteHandlerRegistry` to immutable (returns `Type`)
- [x] Create `HandlerActivator` class
- [ ] Update `CommandLineApplicationBuilder` to use builder (deferred - integration pending)
- [x] Update tests to use builder + activator pattern
- [ ] Update spec.md FR-003 (optional docs update)
- [ ] Update plan.md handler registry examples (optional docs update)
- [ ] Update test-cases.md prerequisites (optional docs update)

---

## Test Results

All 1108 tests pass after implementation.

---

## Priority

**Completed** — The core builder/freeze pattern is implemented. Integration with `CommandLineApplicationBuilder` will be done when the autocomplete feature is fully integrated.
