# Feature Specification: VirtualConsole Integration

**Feature Branch**: `005-virtualconsole-integration`  
**Created**: 2026-01-08  
**Status**: Draft  
**Input**: User description: "Integrate VirtualConsole terminal emulator from master branch for functional UI testing"

## Overview

This spec defines the integration of the VirtualConsole terminal emulator (developed in spec 011 on master) into the rework branch. VirtualConsole provides full VT100/ANSI terminal emulation capabilities for testing CLI applications, enabling verification of rendered screen state rather than just raw ANSI output strings.

### Background

The rework branch currently has a `VirtualAnsiConsole` in the Tests project that captures output as a simple string buffer. This cannot verify:
- Actual rendered screen appearance
- Cursor positioning effects
- Color and style rendering
- ANSI escape sequence interpretation

The master branch contains a complete VirtualConsole implementation (spec 011) that addresses these gaps with:
- 2D screen buffer with cell-level queries
- Full ANSI/VT100 escape sequence parsing
- Color and style tracking per cell
- Zero external dependencies (.NET Standard 2.0)

### Reference Materials

This spec includes the original spec 011 materials in the `references/` subfolder:
- `spec-011-spec.md` - Original specification
- `spec-011-plan.md` - Implementation plan
- `spec-011-data-model.md` - Data model documentation
- `spec-011-tasks.md` - Task breakdown
- `spec-011-research.md` - Research notes
- `spec-011-checklists/` - Validation checklists
- `docs/` - VirtualConsole documentation

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cherry-Pick VirtualConsole Projects (Priority: P1)

As a developer on the rework branch, I need the VirtualConsole and VirtualConsole.Tests projects added to the solution so that I have access to the terminal emulator for testing.

**Why this priority**: This is the foundational step - without the projects, nothing else can proceed.

**Independent Test**: Can be tested by building the solution and running VirtualConsole.Tests - all tests should pass.

**Acceptance Scenarios**:

1. **Given** the rework branch solution, **When** I build, **Then** BitPantry.VirtualConsole compiles without errors
2. **Given** BitPantry.VirtualConsole.Tests is added to the solution, **When** I run the tests, **Then** all existing VirtualConsole tests pass
3. **Given** the projects are added, **When** I inspect the solution, **Then** I see both projects in appropriate solution folders

---

### User Story 2 - Migrate Existing Tests to VirtualConsoleAnsiAdapter (Priority: P2)

As a developer, I need existing tests using `VirtualAnsiConsole` migrated to use `VirtualConsoleAnsiAdapter` so that tests use the new terminal emulator infrastructure.

**Why this priority**: The old `VirtualAnsiConsole` is replaced by the superior `VirtualConsoleAnsiAdapter`. Migration ensures tests benefit from the new capabilities and eliminates duplicate infrastructure.

**Independent Test**: All existing tests that used `VirtualAnsiConsole` compile and pass using `VirtualConsoleAnsiAdapter`.

**Acceptance Scenarios**:

1. **Given** tests use `VirtualAnsiConsole`, **When** migrated to `VirtualConsoleAnsiAdapter`, **Then** tests compile successfully
2. **Given** the migration is complete, **When** I search for `VirtualAnsiConsole`, **Then** no usages remain (only the deleted files)
3. **Given** the old `VirtualConsole/` folder exists, **When** migration is complete, **Then** the folder is deleted

**Migration Scope** (4 usages in 3 files):
- `AutoCompleteControllerTests.cs` - 2 instances
- `Service/TestConsoleService.cs` - 1 instance  
- `Environment/TestEnvironment.cs` (Remote.SignalR tests) - 1 instance

**Files to Delete**:
- `BitPantry.CommandLine.Tests/VirtualConsole/VirtualAnsiConsole.cs`
- `BitPantry.CommandLine.Tests/VirtualConsole/VirtualConsoleInput.cs`
- `BitPantry.CommandLine.Tests/VirtualConsole/VirtualAnsiConsoleExtensions.cs`

---

### User Story 3 - Create VirtualConsole.Testing with General-Purpose Components (Priority: P3)

As a developer, I need a VirtualConsole.Testing project with foundational testing components so that I can write fluent assertions and integrate with Spectre.Console in tests.

**Why this priority**: Testing utilities make VirtualConsole practical to use, but the core emulator must exist first.

**Independent Test**: Can create a test that uses FluentAssertions extensions on VirtualConsole and routes Spectre.Console output to VirtualConsole via the adapter.

**Acceptance Scenarios**:

1. **Given** BitPantry.VirtualConsole.Testing is added to the solution, **When** I build, **Then** it compiles without errors
2. **Given** the project exists, **When** I use `console.Should().ContainText("expected")`, **Then** FluentAssertions work correctly
3. **Given** the project exists, **When** I create a `VirtualConsoleAnsiAdapter`, **Then** Spectre.Console output is captured in VirtualConsole
4. **Given** the project references only general-purpose components, **Then** it does NOT depend on AutoComplete or CommandLine internals

**Included Components**:
- `VirtualConsoleAssertions.cs` - FluentAssertions extensions for VirtualConsole
- `VirtualConsoleAnsiAdapter.cs` - Bridges Spectre.Console IAnsiConsole to VirtualConsole
- `IKeyboardSimulator.cs` - Generic keyboard simulation interface

**Excluded Components** (autocomplete-coupled, deferred):
- `KeyboardSimulator.cs` - Tightly coupled to AutoCompleteController
- `AutoCompleteTestHarness.cs` - Orchestrates autocomplete tests
- `HarnessAssertions.cs` - Assertions for autocomplete harness
- `TestCommandBase.cs` - 30+ test commands for autocomplete tests

---

### User Story 4 - Copy VirtualConsole Documentation (Priority: P4)

As a developer, I need the VirtualConsole documentation available in the Docs folder so that I understand how to use the terminal emulator.

**Why this priority**: Documentation supports adoption but isn't blocking for the code to work.

**Independent Test**: Documentation files exist and are accessible in the Docs/VirtualConsole folder.

**Acceptance Scenarios**:

1. **Given** the VirtualConsole is integrated, **When** I look in Docs/VirtualConsole, **Then** I find README.md, getting-started.md, and ansi-support.md
2. **Given** the documentation exists, **When** I read it, **Then** it accurately describes the VirtualConsole API

---

### Edge Cases

*All edge cases have been analyzed and resolved:*

1. ~~What if VirtualConsole.Tests has dependencies that don't exist on rework?~~
   - **Resolved**: VirtualConsole.Tests only depends on VirtualConsole + standard packages (MSTest, FluentAssertions, coverlet). No BitPantry.CommandLine dependencies. Clean cherry-pick.

2. ~~What if there are namespace conflicts between old and new VirtualConsole?~~
   - **Resolved**: The old `VirtualConsole/` folder is deleted as part of User Story 2. No coexistence = no conflict.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Solution MUST include BitPantry.VirtualConsole project from master branch
- **FR-002**: Solution MUST include BitPantry.VirtualConsole.Tests project from master branch
- **FR-003**: All existing VirtualConsole.Tests MUST pass after cherry-pick
- **FR-004**: Solution MUST include BitPantry.VirtualConsole.Testing project with general-purpose components only
- **FR-005**: VirtualConsole.Testing MUST include VirtualConsoleAssertions.cs (FluentAssertions extensions)
- **FR-006**: VirtualConsole.Testing MUST include VirtualConsoleAnsiAdapter.cs (Spectre.Console bridge)
- **FR-007**: VirtualConsole.Testing MUST include IKeyboardSimulator.cs (keyboard simulation interface)
- **FR-008**: VirtualConsole.Testing MUST NOT depend on BitPantry.CommandLine or autocomplete components
- **FR-009**: Documentation MUST be copied to Docs/VirtualConsole folder
- **FR-010**: Existing tests using VirtualAnsiConsole MUST be migrated to VirtualConsoleAnsiAdapter
- **FR-011**: The old VirtualConsole folder in Tests project MUST be deleted after migration

### Exclusions

- **EX-001**: KeyboardSimulator.cs implementation is NOT included (tightly coupled to AutoCompleteController)
- **EX-002**: AutoCompleteTestHarness.cs is NOT included (orchestrates autocomplete-specific tests)
- **EX-003**: HarnessAssertions.cs is NOT included (assertions for autocomplete harness)
- **EX-004**: TestCommandBase.cs is NOT included (30+ test commands for autocomplete tests)
- **EX-005**: Spec 012 autocomplete integration tests are NOT included (tightly coupled to autocomplete implementation)
- **EX-006**: No modifications to VirtualConsole core code - this is a direct cherry-pick

### Key Entities

- **VirtualConsole**: Main terminal emulator class with screen buffer and ANSI parsing
- **ScreenBuffer**: 2D array of ScreenCell representing terminal display
- **ScreenCell**: Single character cell with content, foreground color, background color, and style
- **AnsiSequenceParser**: Parser for VT100/ANSI escape sequences
- **VirtualConsoleAssertions**: FluentAssertions extensions for asserting on VirtualConsole state
- **VirtualConsoleAnsiAdapter**: Adapter implementing IAnsiConsole that routes to VirtualConsole
- **IKeyboardSimulator**: Interface for keyboard input simulation in tests

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Solution builds successfully with all three VirtualConsole projects
- **SC-002**: 100% of VirtualConsole.Tests pass (same as on master)
- **SC-003**: VirtualConsole.Testing compiles with only VirtualConsole and external package dependencies
- **SC-004**: FluentAssertions extensions work: `console.Should().ContainText("x")` compiles and executes
- **SC-005**: Spectre.Console adapter works: `VirtualConsoleAnsiAdapter` routes output to VirtualConsole
- **SC-006**: All existing tests pass after migration to VirtualConsoleAnsiAdapter
- **SC-007**: No references to old `VirtualAnsiConsole` remain in codebase
- **SC-008**: Documentation is accessible in Docs/VirtualConsole
- **SC-009**: Cherry-pick is traceable (commit messages reference spec 011 origin)

## Implementation Notes

### Cherry-Pick Strategy

```bash
# From the 005-virtualconsole-integration branch (based on rework)
git checkout origin/master -- BitPantry.VirtualConsole BitPantry.VirtualConsole.Tests
git checkout origin/master -- Docs/VirtualConsole
```

### VirtualConsole.Testing Project Creation

Create a new project with only the general-purpose components:

```bash
# Create project structure
mkdir BitPantry.VirtualConsole.Testing

# Cherry-pick only the general-purpose files from master
git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs > BitPantry.VirtualConsole.Testing/VirtualConsoleAssertions.cs
git show origin/master:BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs > BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs
git show origin/master:BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs > BitPantry.VirtualConsole.Testing/IKeyboardSimulator.cs
```

Create a new .csproj (NOT cherry-picked - modified to remove autocomplete dependencies):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <Description>Testing extensions for BitPantry.VirtualConsole - provides FluentAssertions extensions and Spectre.Console adapter.</Description>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\BitPantry.VirtualConsole\BitPantry.VirtualConsole.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Spectre.Console" />
  </ItemGroup>
</Project>
```

### Solution Integration

Add all three projects to solution:
```bash
dotnet sln add BitPantry.VirtualConsole/BitPantry.VirtualConsole.csproj
dotnet sln add BitPantry.VirtualConsole.Tests/BitPantry.VirtualConsole.Tests.csproj
dotnet sln add BitPantry.VirtualConsole.Testing/BitPantry.VirtualConsole.Testing.csproj
```

### Migration Strategy

Replace `VirtualAnsiConsole` with `VirtualConsoleAnsiAdapter`:

```csharp
// Before:
var console = new VirtualAnsiConsole();
// ... use console (IAnsiConsole) ...
var output = console.Output; // raw string

// After:
var virtualConsole = new VirtualConsole(80, 24);
var console = new VirtualConsoleAnsiAdapter(virtualConsole);
// ... use console (IAnsiConsole) ...
var content = virtualConsole.GetScreenContent(); // or use assertions
virtualConsole.Should().ContainText("expected");
```

**Files to migrate:**
1. `BitPantry.CommandLine.Tests/AutoCompleteControllerTests.cs`
2. `BitPantry.CommandLine.Tests/Service/TestConsoleService.cs`
3. `BitPantry.CommandLine.Tests.Remote.SignalR/Environment/TestEnvironment.cs`

**After migration, delete:**
- `BitPantry.CommandLine.Tests/VirtualConsole/` folder (entire folder)
