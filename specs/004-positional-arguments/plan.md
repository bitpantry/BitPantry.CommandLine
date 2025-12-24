# Implementation Plan: Positional Arguments

**Branch**: `004-positional-arguments` | **Date**: December 24, 2025 | **Spec**: [spec.md](spec.md)

## Summary

Add positional argument support to BitPantry.CommandLine, enabling CLI users to invoke commands with natural syntax like `copy source.txt dest.txt` instead of requiring `--source source.txt --dest dest.txt`. The implementation extends the existing attribute-based argument definition system, modifies the parsing pipeline to recognize positional values, updates resolution to map values by position order, and enhances auto-complete and help display for positional arguments. Additionally, repeated named options will be supported for collection types.

## Technical Context

**Language/Version**: C# / .NET (matches existing solution)  
**Primary Dependencies**: BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq  
**Storage**: N/A (library component)  
**Testing**: MSTest with FluentAssertions and Moq (per constitution)  
**Target Platform**: Cross-platform .NET library  
**Project Type**: Existing multi-project solution (library + tests)  
**Performance Goals**: Parsing overhead negligible; no measurable regression from current behavior  
**Constraints**: Backward compatible - existing commands without positional arguments must continue to work unchanged  
**Scale/Scope**: ~15 files modified across 2 projects (main library + tests)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | Tests written FIRST for each component; comprehensive coverage of all happy paths AND error/exception paths |
| **II. Dependency Injection** | ✅ PASS | No new services; extends existing DI-compatible classes |
| **III. Security by Design** | ✅ N/A | No security-sensitive changes (local parsing only) |
| **IV. Follow Existing Patterns** | ✅ PASS | Extends existing `ArgumentAttribute`, `ArgumentInfo`, `CommandElement` patterns |
| **V. Integration Testing** | ✅ PASS | End-to-end tests for parsing→resolution→activation flow required |

## Project Structure

### Documentation (this feature)

```text
specs/004-positional-arguments/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── checklists/          # Quality checklists
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (files to modify)

```text
BitPantry.CommandLine/
├── API/
│   └── ArgumentAttribute.cs          # Add Position, IsRest properties
├── Component/
│   └── ArgumentInfo.cs               # Add Position, IsRest, IsPositional
├── Processing/
│   ├── Parsing/
│   │   └── ParsedCommandElement.cs   # Add PositionalValue element type, -- handling
│   ├── Resolution/
│   │   └── CommandResolver.cs        # Positional matching logic, repeated options
│   └── Activation/
│       └── CommandActivator.cs       # Positional value binding, multi-value collections
├── AutoComplete/
│   ├── AutoCompleteOptionsBuilder.cs # Positional slot completion
│   └── AutoCompleteContext.cs        # Extend for positional context
├── Help/
│   └── HelpFormatter.cs              # Positional synopsis display
├── Commands/
│   └── CommandInfo.cs                # Positional validation
└── CommandRegistry.cs                # Registration-time validation

BitPantry.CommandLine.Tests/
├── ParsedCommandTests.cs             # Positional parsing tests
├── ResolveCommandTests.cs            # Positional resolution tests
├── CommandActivatorTests.cs          # Positional binding tests
├── DescribeCommandTests.cs           # Reflection tests for Position/IsRest
├── AutoCompleteSetBuilderTests_Positional.cs  # NEW: Positional autocomplete
├── Commands/
│   ├── PositionalCommands/           # NEW: Test command classes
│   └── RepeatedOptionCommands/       # NEW: Test command classes
└── Help/
    └── HelpFormatterTests.cs         # Positional help format tests

Docs/
├── EndUserGuide.md                   # Update with positional syntax
└── ImplementerGuide.md               # Update with positional attribute usage
```

**Structure Decision**: Extends existing project structure. No new projects needed.

## Complexity Tracking

No constitution violations requiring justification.

## Test Plan

Per Constitution Principle I (TDD) and Principle V (Integration Testing), all tests must be written FIRST and cover both happy paths AND error/exception paths.

### Unit Test Coverage Matrix

#### Parsing Tests (ParsedCommandTests.cs)

| Test ID | Scenario | Input | Expected | Type |
|---------|----------|-------|----------|------|
| PARSE-001 | Single positional value | `cmd value1` | Element[1] = PositionalValue | Happy |
| PARSE-002 | Multiple positional values | `cmd val1 val2 val3` | Elements[1-3] = PositionalValue | Happy |
| PARSE-003 | Positional then named | `cmd pos1 --opt val` | pos1=Positional, --opt=ArgName | Happy |
| PARSE-004 | Named then positional-like | `cmd --opt val pos1` | val=ArgValue, pos1=Unexpected | Happy |
| PARSE-005 | End-of-options separator | `cmd -- -dash` | -dash=PositionalValue | Happy |
| PARSE-006 | Multiple after -- | `cmd -- -a -b -c` | All three = PositionalValue | Happy |
| PARSE-007 | Empty positional region | `cmd --opt val` | No positional elements | Happy |
| PARSE-008 | Quoted positional value | `cmd "hello world"` | Single PositionalValue | Happy |
| PARSE-009 | Mixed quotes and bare | `cmd "a b" c "d e"` | Three PositionalValues | Happy |
| PARSE-010 | Bare -- with no following | `cmd --` | EndOfOptions element only | Edge |
| PARSE-011 | Mid-positional -- | `cmd pos1 -- -dashval` | pos1=Positional, -dashval=Positional | Edge |

#### Validation Tests (DescribeCommandTests.cs)

| Test ID | Scenario | Configuration | Expected Error | Type |
|---------|----------|---------------|----------------|------|
| VAL-001 | Valid single positional | Position=0 | No error | Happy |
| VAL-002 | Valid multiple positional | Position=0,1,2 | No error | Happy |
| VAL-003 | Valid IsRest on array | Position=1, IsRest, string[] | No error | Happy |
| VAL-004 | IsRest on scalar | Position=0, IsRest, string | Error: not collection | Error |
| VAL-005 | IsRest without Position | IsRest=true, Position=-1 | Error: not positional | Error |
| VAL-006 | Multiple IsRest | Two args with IsRest | Error: multiple IsRest | Error |
| VAL-007 | IsRest not last | Position=0 IsRest, Position=1 | Error: not last | Error |
| VAL-008 | Gap in positions | Position=0, Position=2 | Error: non-contiguous | Error |
| VAL-009 | Duplicate positions | Two Position=1 | Error: duplicate | Error |
| VAL-010 | Negative position | Position=-5 | Error: invalid position | Error |
| VAL-011 | Mixed positional and named | Position=0, Position=-1 | No error | Happy |
| VAL-012 | Error message content | Position=0, IsRest, string | Message contains command & property name | Error |

#### Resolution Tests (ResolveCommandTests.cs)

| Test ID | Scenario | Input | Expected | Type |
|---------|----------|-------|----------|------|
| RES-001 | Single positional resolved | `cmd val1` | InputMap[pos0] = val1 | Happy |
| RES-002 | Multiple positional resolved | `cmd a b c` | InputMap has all 3 | Happy |
| RES-003 | Positional + named | `cmd pos --opt val` | Both in InputMap | Happy |
| RES-004 | IsRest collects remaining | `cmd a b c d` | IsRest=[b,c,d] | Happy |
| RES-005 | IsRest with zero extra | `cmd a` | IsRest=[] | Happy |
| RES-006 | Missing required positional | `cmd` (pos0 required) | Error: missing required | Error |
| RES-007 | Excess positional (no IsRest) | `cmd a b c` (only 2 defined) | Error: excess values | Error |
| RES-008 | Repeated option collection | `cmd --opt a --opt b` | InputMap[opt] = [a,b] | Happy |
| RES-009 | Repeated option scalar | `cmd --opt a --opt b` (scalar) | Error: duplicate | Error |
| RES-010 | Mixed delimiter + repeated | `cmd --opt "a;b" --opt c` | InputMap[opt] = [a,b,c] | Happy |
| RES-010b | Repeated then delimiter | `cmd --opt c --opt "a;b"` | InputMap[opt] = [c,a,b] | Happy |
| RES-011 | Positional after -- | `cmd -- -a -b` | Both as positional | Happy |

#### Activation Tests (CommandActivatorTests.cs)

| Test ID | Scenario | Resolution | Expected Property Value | Type |
|---------|----------|------------|------------------------|------|
| ACT-001 | String positional | pos0="hello" | Property = "hello" | Happy |
| ACT-002 | Int positional | pos0="42" | Property = 42 | Happy |
| ACT-003 | IsRest string array | [a,b,c] | Property = ["a","b","c"] | Happy |
| ACT-004 | IsRest int array | [1,2,3] | Property = [1,2,3] | Happy |
| ACT-005 | IsRest List<string> | [a,b,c] | Property = List with 3 items | Happy |
| ACT-006 | Repeated option array | [a,b,c] | Property = ["a","b","c"] | Happy |
| ACT-007 | Positional type mismatch | pos0="abc" for int | Parsing error | Error |
| ACT-008 | Empty IsRest | [] | Property = empty array | Happy |
| ACT-009 | Mixed positional + named | pos0,opt1 | Both properties set | Happy |

#### AutoComplete Tests (AutoCompleteSetBuilderTests_Positional.cs - NEW)

| Test ID | Scenario | Cursor Position | Expected | Type |
|---------|----------|-----------------|----------|------|
| AC-001 | First positional slot | `cmd ` | Invoke pos0 autocomplete | Happy |
| AC-002 | Second positional slot | `cmd val1 ` | Invoke pos1 autocomplete | Happy |
| AC-003 | IsRest continues | `cmd val1 val2 ` | Invoke IsRest autocomplete | Happy |
| AC-004 | No autocomplete function | `cmd ` (no func) | No suggestions | Happy |
| AC-005 | Context has prior values | `cmd val1 ` | Context.Values has pos0 | Happy |
| AC-006 | After named option | `cmd --opt ` | Named arg completion | Happy |
| AC-007 | Partial positional | `cmd va` | Filter by "va" | Happy |

#### Help Display Tests (HelpFormatterTests.cs)

| Test ID | Scenario | Command Config | Expected Synopsis | Type |
|---------|----------|----------------|-------------------|------|
| HELP-001 | Required positional | Position=0, Required | `cmd <arg>` | Happy |
| HELP-002 | Optional positional | Position=0, !Required | `cmd [<arg>]` | Happy |
| HELP-003 | Variadic positional | IsRest=true | `cmd <args>...` | Happy |
| HELP-004 | Mixed pos + named | pos0 + --opt | `cmd <arg> [--opt]` | Happy |
| HELP-005 | Multiple positional | pos0, pos1 | `cmd <arg1> <arg2>` | Happy |
| HELP-006 | Repeated option note | Collection --opt | Shows "can be repeated" | Happy |

### Integration Tests

| Test ID | Scenario | End-to-End Flow | Type |
|---------|----------|-----------------|------|
| INT-001 | Full positional execution | Parse→Resolve→Activate→Execute | Happy |
| INT-002 | Full IsRest execution | Multiple values through pipeline | Happy |
| INT-003 | Full repeated option | --opt a --opt b through pipeline | Happy |
| INT-004 | Backward compatibility | Existing named-only command works | Regression |
| INT-005 | Help with positional | --help shows positional synopsis | Happy |
| INT-006 | Validation error at startup | Invalid config throws on register | Error |

### Test Command Classes Required

```text
BitPantry.CommandLine.Tests/Commands/
├── PositionalCommands/
│   ├── SinglePositionalCommand.cs
│   ├── MultiplePositionalCommand.cs
│   ├── PositionalWithNamedCommand.cs
│   ├── IsRestCommand.cs
│   ├── IsRestWithPrecedingCommand.cs
│   ├── RequiredPositionalCommand.cs
│   ├── OptionalPositionalCommand.cs
│   ├── PositionalWithAutoCompleteCommand.cs
│   ├── InvalidIsRestScalarCommand.cs
│   ├── InvalidIsRestNotPositionalCommand.cs
│   ├── InvalidMultipleIsRestCommand.cs
│   ├── InvalidIsRestNotLastCommand.cs
│   ├── InvalidGapPositionCommand.cs
│   └── InvalidDuplicatePositionCommand.cs
└── RepeatedOptionCommands/
    ├── RepeatedOptionArrayCommand.cs
    ├── RepeatedOptionListCommand.cs
    └── RepeatedOptionScalarCommand.cs
```

## Documentation Updates

| Document | Section | Changes |
|----------|---------|---------|
| EndUserGuide.md | Command Syntax | Add "Positional Arguments" section |
| EndUserGuide.md | Command Syntax | Add "Repeated Options" section |
| EndUserGuide.md | Command Syntax | Add `--` separator explanation |
| ImplementerGuide.md | Defining Commands | Add `[Argument(Position=N)]` usage |
| ImplementerGuide.md | Defining Commands | Add `[Argument(Position=N, IsRest=true)]` usage |
| ImplementerGuide.md | Defining Commands | Add validation rules table |
| ImplementerGuide.md | Defining Commands | Add repeated option collection syntax |

## Implementation Order

Per TDD, each phase writes tests FIRST, verifies they fail, then implements.

1. **Phase 1: Attribute & Validation** (Tests: VAL-*, PARSE-001 through PARSE-003)
   - Extend `ArgumentAttribute` with Position, IsRest
   - Extend `ArgumentInfo` with Position, IsRest, IsPositional
   - Add validation in `CommandRegistry.Validate()`
   - Write all validation test cases first

2. **Phase 2: Parsing** (Tests: PARSE-*)
   - Add `PositionalValue` element type
   - Update `ParsedCommandElement` classification logic
   - Handle `--` separator
   - Write parsing tests first

3. **Phase 3: Resolution** (Tests: RES-*)
   - Create `ArgumentValues` wrapper
   - Update `ResolvedCommand.InputMap` type
   - Implement positional matching in `CommandResolver`
   - Implement repeated option handling
   - Write resolution tests first

4. **Phase 4: Activation** (Tests: ACT-*)
   - Update `CommandActivator` for multi-value binding
   - Handle IsRest collection population
   - Handle repeated option merging
   - Write activation tests first

5. **Phase 5: AutoComplete** (Tests: AC-*)
   - Extend `AutoCompleteContext`
   - Update `AutoCompleteOptionsBuilder` for positional slots
   - Write autocomplete tests first

6. **Phase 6: Help** (Tests: HELP-*)
   - Update `HelpFormatter` for positional synopsis
   - Write help tests first

7. **Phase 7: Integration & Docs** (Tests: INT-*)
   - Write integration tests
   - Run full regression suite
   - Update documentation

