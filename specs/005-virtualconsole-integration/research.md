# Research: VirtualConsole Integration

**Feature**: 005-virtualconsole-integration  
**Date**: 2026-01-08  
**Status**: Complete

## Overview

This research covers the **integration** of VirtualConsole into the rework branch. For VirtualConsole's internal design, ANSI standards, and terminal emulation patterns, see [references/spec-011-research.md](references/spec-011-research.md).

## Integration Research Tasks

### 1. VirtualConsole.Tests Dependency Compatibility

**Question**: Does VirtualConsole.Tests have dependencies that don't exist on rework?

**Findings**:
```xml
<!-- VirtualConsole.Tests.csproj dependencies -->
<PackageReference Include="FluentAssertions" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="MSTest.TestAdapter" />
<PackageReference Include="MSTest.TestFramework" />
<PackageReference Include="coverlet.collector" />

<ProjectReference Include="..\BitPantry.VirtualConsole\BitPantry.VirtualConsole.csproj" />
```

**Decision**: Clean cherry-pick - no BitPantry.CommandLine dependencies  
**Rationale**: All dependencies are standard test packages already used in solution

---

### 2. VirtualConsole.Testing Component Selection

**Question**: Which files from VirtualConsole.Testing are general-purpose vs autocomplete-coupled?

**Findings**:

| File | Dependencies | Decision |
|------|--------------|----------|
| `VirtualConsoleAssertions.cs` | VirtualConsole, FluentAssertions | ✅ Include |
| `VirtualConsoleAnsiAdapter.cs` | VirtualConsole, Spectre.Console | ✅ Include |
| `IKeyboardSimulator.cs` | None (interface only) | ✅ Include |
| `KeyboardSimulator.cs` | AutoCompleteController, ConsoleLineMirror | ❌ Exclude |
| `AutoCompleteTestHarness.cs` | CommandLineApplication, AutoComplete | ❌ Exclude |
| `HarnessAssertions.cs` | AutoCompleteTestHarness | ❌ Exclude |
| `TestCommandBase.cs` | CommandBase, AutoComplete attributes | ❌ Exclude |

**Decision**: Include only 3 files; create new .csproj without CommandLine references  
**Rationale**: Maintains clean separation; autocomplete components can be added later

---

### 3. Migration Strategy for VirtualAnsiConsole

**Question**: How do existing tests use VirtualAnsiConsole and how should they migrate?

**Findings**:

| File | Current Usage | Migration |
|------|---------------|-----------|
| `AutoCompleteControllerTests.cs` | `new VirtualAnsiConsole()` + `console.Output` | Use `VirtualConsoleAnsiAdapter` + `GetScreenContent()` |
| `TestConsoleService.cs` | Wraps `VirtualAnsiConsole` | Accept `VirtualConsoleAnsiAdapter` instead |
| `TestEnvironment.cs` | `new VirtualAnsiConsole()` | Use `VirtualConsoleAnsiAdapter` |

**Decision**: Replace VirtualAnsiConsole with VirtualConsoleAnsiAdapter + VirtualConsole  
**Rationale**: Same IAnsiConsole interface, gains screen buffer capabilities

---

### 4. VirtualConsole Test Coverage Verification

**Question**: Is VirtualConsole.Tests comprehensive enough for production use?

**Findings**: 250 tests across 16 files covering all VT100/ANSI functionality.

**Decision**: Test coverage is comprehensive - proceed with cherry-pick  
**Rationale**: See [references/spec-011-research.md](references/spec-011-research.md) for detailed coverage analysis

---

## Summary

| Topic | Status | Decision |
|-------|--------|----------|
| Dependency compatibility | ✅ Resolved | Clean cherry-pick |
| Component selection | ✅ Resolved | 3 files + new .csproj |
| Migration strategy | ✅ Resolved | Replace with adapter pattern |
| Test coverage | ✅ Resolved | 250 tests - comprehensive |

## Reference

For VirtualConsole internals (ANSI standards, SGR codes, cursor sequences, etc.), see:
- [references/spec-011-research.md](references/spec-011-research.md) - Full technical research
- [references/spec-011-data-model.md](references/spec-011-data-model.md) - Entity definitions
