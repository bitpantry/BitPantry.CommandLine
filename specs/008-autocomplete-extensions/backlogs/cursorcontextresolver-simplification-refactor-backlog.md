# CursorContextResolver Simplification Refactor Backlog

**Status:** Deferred  
**Created:** 2026-01-25  
**Last Updated:** 2026-01-25

## Overview

`CursorContextResolver` has grown to ~800 lines as part of implementing spec 008 (Option A: Positional-First Modal Autocomplete). While the current implementation is fully functional with 473 passing tests, there are opportunities to further decompose the class for improved maintainability, testability, and adherence to single-responsibility principle.

## What Was Completed

### ResolutionState Refactor ✅

Extracted a `ResolutionState` private class that encapsulates all intermediate computed values during resolution. This eliminated the 6-8 parameter method signatures that were threading state through the call chain.

```csharp
private class ResolutionState
{
    public string Input { get; init; }
    public int CursorPosition { get; init; }
    public ParsedInput ParsedInput { get; init; }
    public ParsedCommand ParsedCommand { get; init; }
    public ParsedCommandElement Element { get; init; }
    public GroupInfo ResolvedGroup { get; init; }
    public CommandInfo ResolvedCommand { get; init; }
    public int PathEndPosition { get; init; }
    public HashSet<ArgumentInfo> UsedArguments { get; set; }
    public int ConsumedPositionalCount { get; set; }
}
```

**Benefits achieved:**
- Parameter count reduced from 8 → 1 for most methods
- Dependencies between methods are now explicit
- Easier to test individual methods in isolation
- Natural stepping stone for further decomposition

---

## Deferred Refactoring Opportunities

### Option 2: Extract CursorContextFactory

**Motivation:** The resolver contains ~250 lines of `CursorContext` construction code spread across 9 factory methods. These are pure data transformation functions.

**Current State:**
```
CursorContextResolver.cs (~800 lines)
├── Entry point: Resolve()
├── Path resolution: ResolvePath()
├── Context determination: DetermineContext(), DetermineCommandArgumentContext(), DetermineEmptySlotContext()
├── Factory methods: CreateArgumentNameContext(), CreateArgumentAliasContext(), 
│   CreateArgumentValueContext(), CreatePositionalValueContext(), CreateEndOfOptionsContext(),
│   CreatePositionalContextForEmptySlot(), CreateGroupChildContext(), CreateRootLevelContext(),
│   CheckForPartialPrefix()
└── Helpers: Has*, Collect*, Count*, Get*, Find*
```

**Proposed Structure:**
```
AutoComplete/Context/
├── CursorContext.cs           (exists - data class)
├── CursorContextType.cs       (exists - enum)
├── CursorContextResolver.cs   (~400 lines - orchestration only)
└── Internal/
    └── CursorContextFactory.cs (~300 lines - all Create* methods)
```

**Factory Interface:**
```csharp
internal static class CursorContextFactory
{
    public static CursorContext CreateArgumentNameContext(ResolutionState state);
    public static CursorContext CreateArgumentAliasContext(ResolutionState state);
    public static CursorContext CreateArgumentValueContext(ResolutionState state);
    public static CursorContext CreatePositionalValueContext(ResolutionState state);
    public static CursorContext CreateEndOfOptionsContext(ResolutionState state);
    public static CursorContext CreatePositionalContextForEmptySlot(ResolutionState state);
    public static CursorContext CreateGroupChildContext(ResolutionState state);
    public static CursorContext CreateRootLevelContext(ResolutionState state);
    public static CursorContext CreateRootContext(string input, int cursorPosition);
    public static CursorContext CheckForPartialPrefix(ResolutionState state);
}
```

**Prerequisites:**
- Move `ResolutionState` to its own file or make it internal/accessible
- Extract helper methods that factory needs (`GetPositionalIndex`, `FindPendingArgumentForValue`)

**Effort:** ~2 hours

---

### Option 3: Extract PathResolver

**Motivation:** The `ResolvePath()` method and `HasUnresolvedCommittedToken()` form a cohesive unit responsible for navigating the group/command hierarchy. This is logically separate from context determination.

**Current Methods:**
- `ResolvePath(ParsedCommand, int cursorPosition)` - 60 lines
- `HasUnresolvedCommittedToken(ParsedCommand, int cursorPosition)` - 25 lines

**Proposed Structure:**
```csharp
internal class CommandPathResolver
{
    private readonly ICommandRegistry _registry;
    
    public CommandPathResolver(ICommandRegistry registry) { ... }
    
    public PathResolutionResult Resolve(ParsedCommand parsedCommand, int cursorPosition);
    public bool HasUnresolvedCommittedToken(ParsedCommand parsedCommand, int cursorPosition);
}

internal class PathResolutionResult
{
    public GroupInfo ResolvedGroup { get; init; }
    public CommandInfo ResolvedCommand { get; init; }
    public int PathEndPosition { get; init; }
}
```

**Benefits:**
- Path resolution logic becomes unit-testable in isolation
- Clear separation between "what command are we on" vs "what can be typed next"
- Reusable for other features (e.g., help system, validation)

**Effort:** ~1.5 hours

---

### Option 4: Strategy Pattern for Element Types

**Motivation:** The `element.ElementType switch` in `DetermineCommandArgumentContext` delegates to different context creation logic based on element type. This could be formalized as a strategy pattern.

**Current Code:**
```csharp
return state.Element.ElementType switch
{
    CommandElementType.ArgumentName => CreateArgumentNameContext(state),
    CommandElementType.ArgumentAlias => CreateArgumentAliasContext(state),
    CommandElementType.ArgumentValue => CreateArgumentValueContext(state),
    CommandElementType.PositionalValue => CreatePositionalValueContext(state),
    CommandElementType.Unexpected => CheckForPartialPrefix(state),
    CommandElementType.EndOfOptions => CreateEndOfOptionsContext(state),
    _ => CursorContext.Empty(state.Input, state.CursorPosition)
};
```

**Proposed Structure:**
```csharp
internal interface IElementContextStrategy
{
    CommandElementType HandledType { get; }
    CursorContext CreateContext(ResolutionState state);
}

internal class ArgumentNameStrategy : IElementContextStrategy { ... }
internal class ArgumentAliasStrategy : IElementContextStrategy { ... }
internal class PositionalValueStrategy : IElementContextStrategy { ... }
// etc.
```

**Benefits:**
- Open/closed principle - add new element types without modifying resolver
- Each strategy is independently testable
- Clear extension point for custom behaviors

**Considerations:**
- May be over-engineering for current needs
- Only valuable if we expect frequent additions of new element types
- Consider only if Options 2 & 3 prove insufficient

**Effort:** ~3 hours

---

## Recommended Approach

1. **Defer all further refactoring** until the autocomplete algorithm stabilizes
2. **If revisiting:** Start with Option 2 (CursorContextFactory) as lowest-risk
3. **Consider Option 3** (PathResolver) if reuse need emerges
4. **Avoid Option 4** unless element type proliferation occurs

---

## File Reference

**Primary file:** [CursorContextResolver.cs](../../../BitPantry.CommandLine/AutoComplete/Context/CursorContextResolver.cs)

**Test file:** [CursorContextResolverTests.cs](../../../BitPantry.CommandLine.Tests/AutoComplete/Context/CursorContextResolverTests.cs)

**Related spec:** [008-autocomplete-extensions/spec.md](../spec.md)

---

## Current Method Inventory

| Category | Method | Lines | Responsibility |
|----------|--------|-------|----------------|
| Entry | `Resolve` | 45 | Main entry point, builds ResolutionState |
| Path | `ResolvePath` | 60 | Navigates group → command hierarchy |
| Determination | `DetermineContext` | 25 | Dispatches based on resolved path |
| Determination | `DetermineCommandArgumentContext` | 35 | Determines argument-phase context |
| Determination | `DetermineEmptySlotContext` | 45 | Handles whitespace/empty positions |
| Factory | `CreateArgumentNameContext` | 15 | Builds ArgumentName context |
| Factory | `CreateArgumentAliasContext` | 15 | Builds ArgumentAlias context |
| Factory | `CreateArgumentValueContext` | 25 | Builds ArgumentValue context |
| Factory | `CreatePositionalValueContext` | 20 | Builds PositionalValue context |
| Factory | `CreateEndOfOptionsContext` | 25 | Handles -- marker |
| Factory | `CreatePositionalContextForEmptySlot` | 20 | Builds positional for empty slot |
| Factory | `CreateGroupChildContext` | 12 | Builds group child context |
| Factory | `CreateRootLevelContext` | 12 | Builds root level context |
| Factory | `CreateRootContext` | 10 | Builds initial empty context |
| Factory | `CheckForPartialPrefix` | 35 | Handles -- and - prefixes |
| Helper | `HasImmediatelyFollowingToken` | 12 | Detects single-space gap |
| Helper | `HasFollowingNonEmptyElement` | 12 | Detects adjacent token |
| Helper | `HasUnresolvedCommittedToken` | 25 | Detects invalid command path |
| Helper | `CollectUsedArguments` | 20 | Gathers already-used args |
| Helper | `GetNextPositionalArgument` | 6 | Finds next positional slot |
| Helper | `CountConsumedPositionals` | 20 | Counts positionals before cursor |
| Helper | `GetPositionalIndex` | 18 | Gets index for specific element |
| Helper | `FindPendingArgumentForValue` | 30 | Finds arg awaiting value |

**Total:** ~800 lines across 23 methods

---

## Acceptance Criteria for Future Work

When picking up any of these refactoring items:

1. ✅ All 473+ existing tests must pass unchanged
2. ✅ No public API changes to `CursorContextResolver`
3. ✅ `ResolutionState` remains internal implementation detail
4. ✅ Performance characteristics unchanged (no extra allocations in hot path)
5. ✅ Code coverage maintained or improved
