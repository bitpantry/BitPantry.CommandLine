# Research: Positional Arguments

**Feature**: 004-positional-arguments  
**Date**: December 24, 2025

## Overview

This document captures research findings for implementing positional arguments in BitPantry.CommandLine. All technical context items are resolved with no outstanding NEEDS CLARIFICATION items.

---

## 1. Existing Codebase Patterns

### Decision: Extend Existing Attribute Pattern
**Rationale**: The codebase already uses `[Argument]` attribute on properties to define CLI arguments. Adding `Position` and `IsRest` properties to this attribute maintains consistency and requires minimal learning for existing implementers.

**Alternatives Considered**:
- Separate `[Positional]` attribute: Rejected because it fragments the API and requires users to learn two attribute types
- Method parameter-based binding: Rejected because it would require major architecture changes and break existing commands

### Current ArgumentAttribute Structure
```csharp
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class ArgumentAttribute : Attribute
{
    public string Name { get; set; }
    public string AutoCompleteFunctionName { get; set; }
    public bool IsRequired { get; set; } = false;
}
```

### Proposed Extension
```csharp
public class ArgumentAttribute : Attribute
{
    public string Name { get; set; }
    public string AutoCompleteFunctionName { get; set; }
    public bool IsRequired { get; set; } = false;
    public int Position { get; set; } = -1;    // -1 = named, 0+ = positional order
    public bool IsRest { get; set; } = false;  // Captures remaining positional tokens
}
```

---

## 2. Parsing Pipeline Integration

### Decision: Add PositionalValue Element Type
**Rationale**: The existing `CommandElementType` enum has clear semantics for each token type. Adding `PositionalValue` maintains this clarity and allows the resolver to handle positional tokens distinctly.

**Current Element Types**:
- `Command` - command name tokens
- `ArgumentName` - `--argName` tokens
- `ArgumentAlias` - `-a` tokens
- `ArgumentValue` - value following a named argument
- `Empty` - whitespace
- `Unexpected` - unrecognized tokens (currently catches bare values)

**New Element Type**:
- `PositionalValue` - bare value before any named argument

### Decision: Handle `--` End-of-Options Separator
**Rationale**: POSIX standard; allows users to pass values starting with dashes (e.g., `rm -- -rf.txt`).

**Implementation**: When a bare `--` token is encountered in the positional region:
1. Mark subsequent tokens as `PositionalValue` regardless of prefix
2. The `--` token itself is consumed and not passed to the command

---

## 3. Resolution Logic

### Decision: Match by Position Order
**Rationale**: Positional arguments are defined with explicit `Position` indices. The resolver collects all `PositionalValue` elements and matches them to `ArgumentInfo` entries sorted by `Position`.

**Algorithm**:
```
1. Collect all PositionalValue elements in order
2. Get all positional ArgumentInfo sorted by Position
3. For each positional ArgumentInfo (except IsRest):
   - If corresponding PositionalValue exists, add to InputMap
   - If not and IsRequired, add error
4. For IsRest ArgumentInfo:
   - Collect all remaining PositionalValue elements
   - Store as list in InputMap (new multi-value support)
```

### Decision: InputMap Extension for Multi-Value
**Rationale**: Current `InputMap` is `Dictionary<ArgumentInfo, ParsedCommandElement>`. For `IsRest` and repeated options, we need multiple values per argument.

**Options Evaluated**:
1. Change to `Dictionary<ArgumentInfo, List<ParsedCommandElement>>` - Breaking change
2. Add parallel `Dictionary<ArgumentInfo, List<ParsedCommandElement>>` for multi-value - Complexity
3. Wrap values: `Dictionary<ArgumentInfo, ArgumentValues>` where `ArgumentValues` can be single or multi - Clean abstraction

**Decision**: Option 3 - Create `ArgumentValues` wrapper class that can hold single or multiple values.

---

## 4. Repeated Named Options

### Decision: Allow Same Option Multiple Times for Collections
**Rationale**: POSIX-standard pattern (`--file a --file b`). Complements existing delimiter-based parsing (`--files "a;b"`).

**Implementation**:
- In resolver, when processing `ArgumentName`/`ArgumentAlias`:
  - If argument already in InputMap AND argument type is collection: append value
  - If argument already in InputMap AND NOT collection: add duplicate error
- In activator, for collection types with multiple values:
  - Parse each value individually via `StringParsing.Parse(elementType, value)`
  - For delimiter values, also parse via existing logic
  - Merge all results into final collection

---

## 5. Validation Rules

### Decision: Validate at Registration Time
**Rationale**: Per constitution, fail-fast at startup. Developers get immediate feedback when command configuration is invalid.

**Validation Rules** (in CommandRegistry.Validate or CommandInfo constructor):

| Rule | Check | Error Message |
|------|-------|---------------|
| IsRest requires collection | `IsRest && !IsCollectionType(PropertyType)` | "Argument '{Name}' has IsRest=true but property type '{Type}' is not a collection" |
| IsRest requires positional | `IsRest && Position < 0` | "Argument '{Name}' has IsRest=true but is not positional (Position not set)" |
| Single IsRest per command | `Count(IsRest) > 1` | "Command '{Command}' has multiple IsRest arguments: {Names}" |
| IsRest must be last | `IsRest && any other positional has higher Position` | "Argument '{Name}' has IsRest=true but is not the last positional argument" |
| Contiguous positions | Gaps in position indices | "Command '{Command}' has non-contiguous positional indices: {Indices}" |
| No duplicate positions | `Position` collision | "Command '{Command}' has duplicate position {N}: {Names}" |
| Position non-negative | `Position < -1` | "Argument '{Name}' has invalid Position {N}; must be -1 or >= 0" |

---

## 6. Auto-Complete Integration

### Decision: Determine Slot by Counting Preceding Positional Tokens
**Rationale**: Auto-complete fires at cursor position. Count `PositionalValue` elements before cursor to determine which positional argument slot is being filled.

**Algorithm**:
```
1. Count PositionalValue elements before cursor
2. Get positional ArgumentInfo at that index
3. If exists and has AutoCompleteFunctionName, invoke it
4. If IsRest, continue using same argument's completion for subsequent slots
5. Pass already-entered values in AutoCompleteContext.Values
```

### Decision: Extend AutoCompleteContext
**Rationale**: Current `Values` dictionary holds single values per argument. For repeated options and IsRest, need to expose all entered values.

**Extension**: Add `IReadOnlyDictionary<ArgumentInfo, IReadOnlyList<string>> MultiValues` property for multi-value arguments.

---

## 7. Help Display Format

### Decision: Industry-Standard Synopsis Notation
**Rationale**: Users expect familiar CLI help formats.

| Argument Type | Format | Example |
|---------------|--------|---------|
| Required positional | `<name>` | `<source>` |
| Optional positional | `[<name>]` | `[<title>]` |
| Variadic positional | `<name>...` | `<files>...` |
| Named option | `[--name <value>]` | `[--force]` |

**Synopsis Order**: Command name → positional args (in order) → named options

**Example**:
```
copy <source> <destination>... [--force] [--verbose]
```

---

## 8. Backward Compatibility

### Decision: Default Behavior Unchanged
**Rationale**: Existing commands without `Position` set continue to work exactly as before.

**Guarantees**:
- `Position = -1` (default) means named-only argument (current behavior)
- Commands with no positional arguments parse identically to before
- All existing tests must continue to pass

---

## 9. Test Strategy

### Decision: TDD with Comprehensive Coverage
**Rationale**: Per constitution, tests written FIRST; cover all happy paths AND error/exception paths.

**Test Categories**:

#### Unit Tests
| Area | Happy Paths | Error Paths |
|------|-------------|-------------|
| Parsing | Single positional, multiple positional, mixed with named, with `--` separator | Malformed input |
| Validation | Valid configs pass | Each validation rule failure |
| Resolution | Positional binding, IsRest collection, repeated options | Missing required, excess values, duplicates |
| Activation | Single value, multi-value, collection types | Type conversion failures |
| AutoComplete | Positional slot detection, IsRest continuation | No completions available |
| Help | Synopsis format for each argument type | Edge cases |

#### Integration Tests
| Scenario | Description |
|----------|-------------|
| End-to-end positional | Parse→resolve→activate→execute with positional args |
| End-to-end mixed | Positional + named args combined |
| End-to-end IsRest | Multiple trailing values collected |
| End-to-end repeated | `--arg a --arg b` merges correctly |
| Backward compat | Existing test commands unchanged |

---

## 10. Documentation Updates

### Decision: Update Both User and Implementer Guides
**Rationale**: Users need to know the syntax; implementers need to know the attributes.

**EndUserGuide.md Updates**:
- Add "Positional Arguments" section explaining syntax
- Examples: `copy source.txt dest.txt`
- Explain `--` separator for values starting with dashes
- Note that repeated options work: `--tag red --tag blue`

**ImplementerGuide.md Updates**:
- Add `[Argument(Position = N)]` usage
- Add `[Argument(Position = N, IsRest = true)]` for variadic
- Validation rules and error messages
- Migration notes (none needed - fully additive)

---

## Summary

All research items resolved. No outstanding NEEDS CLARIFICATION items. Ready for Phase 1 design artifacts.
