# Implementation Plan: Command Groups

**Branch**: `003-command-groups` | **Date**: 2025-01-XX | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/003-command-groups/spec.md`

## Summary

Replace the current namespace-based command organization with a hierarchical, type-safe group system. Groups are defined via marker classes decorated with `[Group]`, commands reference groups via `[Command(Group = typeof(X))]`, and invocation uses space-separated syntax (`myapp group command`). The framework intercepts `--help`/`-h` flags to display auto-generated help without polluting command classes.

**Key Technical Approaches:**
1. New `GroupAttribute` and `GroupInfo` entities
2. Modified `CommandAttribute` (remove `Namespace`, add `Group` type reference)
3. Updated parsing to handle space-separated group paths
4. Help flag interception in `CommandLineApplicationCore.Run()` between resolution and execution
5. New `IHelpFormatter` service for consistent help display

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console (IAnsiConsole for rich console output)  
**Storage**: N/A (in-memory command registry)  
**Testing**: MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers  
**Target Platform**: Cross-platform (.NET runtime)
**Project Type**: Single library with test projects  
**Performance Goals**: Startup registration < 100ms for 1000 commands  
**Constraints**: No runtime dependencies beyond Spectre.Console  
**Scale/Scope**: Library supporting 10-1000 commands per application

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| TDD Required | ✅ | All changes will have tests written first |
| DI Patterns | ✅ | `IHelpFormatter` injected via DI, no service location |
| Security by Design | ✅ | Type-safe group references prevent injection; input validation on group paths |
| Follow Existing Patterns | ✅ | `GroupInfo` mirrors `CommandInfo`; uses existing `IAnsiConsole` |
| No Unnecessary Abstractions | ✅ | Single `IHelpFormatter` interface; groups are simple marker classes |

## Project Structure

### Documentation (this feature)

```text
specs/003-command-groups/
├── plan.md              # This file
├── research.md          # Phase 0 output (architecture analysis)
├── data-model.md        # Phase 1 output (entity definitions)
├── quickstart.md        # Phase 1 output (developer guide)
└── tasks.md             # Phase 2 output (implementation tasks)
```

### Source Code (repository root)

```text
BitPantry.CommandLine/
├── API/
│   ├── CommandAttribute.cs      # MODIFY: Remove Namespace, add Group
│   ├── GroupAttribute.cs        # NEW: Group marker attribute
│   └── ...
├── Component/
│   ├── CommandInfo.cs           # MODIFY: Replace Namespace with Group
│   ├── GroupInfo.cs             # NEW: Group runtime metadata
│   └── ...
├── Help/                        # NEW DIRECTORY
│   ├── IHelpFormatter.cs        # NEW: Help display interface
│   └── HelpFormatter.cs         # NEW: Spectre.Console implementation
├── Processing/
│   ├── Parsing/
│   │   ├── ParsedInput.cs       # MODIFY: Space-separated parsing
│   │   └── ParsedCommand.cs     # MODIFY: Add GroupPath
│   ├── Resolution/
│   │   ├── CommandResolver.cs   # MODIFY: Group-aware resolution
│   │   └── ResolvedCommand.cs   # MODIFY: Add ResolvedType enum
│   └── Execution/
│       └── CommandLineApplicationCore.cs  # MODIFY: Help interception
├── Commands/
│   └── ListCommandsCommand.cs   # MODIFY: Display groups
└── CommandRegistry.cs           # MODIFY: Group tracking

BitPantry.CommandLine.Tests/
├── GroupTests.cs                # NEW: Group registration tests
├── HelpFormatterTests.cs        # NEW: Help display tests
├── DescribeCommandsTests.cs     # MODIFY: Update for groups
├── ResolveCommandsTests.cs      # MODIFY: Update for groups
└── ...
```

**Structure Decision**: Single library project with test project. All changes are within existing project structure, adding a new `Help/` directory for help-related functionality.

## Complexity Tracking

> No constitution violations identified. Implementation follows existing patterns.

## Implementation Phases

### Phase 1: Data Model & Core Types (Foundation)

Create new types and modify existing ones to support the group model.

**Files:**
1. `API/GroupAttribute.cs` - NEW
2. `Component/GroupInfo.cs` - NEW  
3. `API/CommandAttribute.cs` - MODIFY (remove Namespace, add Group)
4. `Component/CommandInfo.cs` - MODIFY (replace Namespace with Group)

**Tests:**
- `GroupAttributeTests.cs` - Attribute validation
- `GroupInfoTests.cs` - Entity behavior

### Phase 2: Registry & Resolution (Core Logic)

Update registry to track groups and resolver to navigate group hierarchy.

**Files:**
1. `CommandRegistry.cs` - MODIFY (add group tracking, update Find methods)
2. `Processing/Resolution/CommandResolver.cs` - MODIFY (group-aware resolution)
3. `Processing/Resolution/ResolvedCommand.cs` - MODIFY (add ResolvedType)

**Tests:**
- `GroupRegistrationTests.cs` - Group registration validation
- `GroupResolutionTests.cs` - Group path navigation

### Phase 3: Parsing (Syntax Change)

Update parsing to handle space-separated group paths.

**Files:**
1. `Processing/Parsing/ParsedInput.cs` - MODIFY
2. `Processing/Parsing/ParsedCommand.cs` - MODIFY (add GroupPath)

**Tests:**
- `GroupParsingTests.cs` - Space-separated syntax parsing

### Phase 4: Help System (New Feature)

Implement help flag interception and display.

**Files:**
1. `Help/IHelpFormatter.cs` - NEW
2. `Help/HelpFormatter.cs` - NEW
3. `Processing/Execution/CommandLineApplicationCore.cs` - MODIFY (help interception)

**Tests:**
- `HelpFormatterTests.cs` - Help output formatting
- `HelpInterceptionTests.cs` - Flag detection and handling

### Phase 5: Built-in Commands & Migration

Update built-in commands and existing tests.

**Files:**
1. `Commands/ListCommandsCommand.cs` - MODIFY (display groups)
2. All test command classes - MODIFY (Namespace → Group)

**Tests:**
- Update all existing tests to use group model

### Phase 6: Documentation

Update all documentation to reflect new group model.

**Files:**
1. `README.md`
2. `Docs/getting-started.md`
3. `Docs/syntax.md`
4. `Docs/advanced-topics.md`
5. XML documentation in source files

## Dependencies Graph

```
Phase 1 (Data Model)
    ↓
Phase 2 (Registry & Resolution)
    ↓
Phase 3 (Parsing)
    ↓
Phase 4 (Help System)
    ↓
Phase 5 (Migration)
    ↓
Phase 6 (Documentation)
```

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Breaking change for all existing users | Clear migration guide; version bump (major) |
| Parsing ambiguity (group vs argument) | Registry lookup during parsing to disambiguate |
| Test file churn | Update tests incrementally per phase |
| Help display consistency | Single `IHelpFormatter` implementation |

## Success Criteria (from spec)

- [ ] SC-001: Commands can be organized into groups using `[Group]` marker classes
- [ ] SC-002: Nested groups work via C# nested classes
- [ ] SC-003: Root-level commands work without groups
- [ ] SC-004: Invoking a group alone displays its contents
- [ ] SC-005: `--help` and `-h` flags are reserved and handled by framework
- [ ] SC-006: All existing tests pass after migration
- [ ] SC-007: Documentation updated for new syntax
- [ ] SC-008: No `Namespace` references remain in codebase
