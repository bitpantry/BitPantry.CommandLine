# Implementation Plan: VirtualConsole Autocomplete Tests

**Branch**: `012-virtualconsole-autocomplete-tests` | **Date**: January 6, 2026 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/012-virtualconsole-autocomplete-tests/spec.md`

## Summary

Complete replacement of the existing autocomplete test suite with new VirtualConsole-based testing approach. The implementation removes all legacy visual/UX testing infrastructure (StepwiseTestRunner, ConsolidatedTestConsole, Verify.MSTest snapshots) and implements 283 test cases using the pure VirtualConsole project with a new testing extension layer.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers (all existing)  
**Storage**: N/A (testing infrastructure only)  
**Testing**: MSTest with FluentAssertions (existing pattern)  
**Target Platform**: Windows/Linux/macOS (cross-platform tests)
**Project Type**: Test infrastructure refactoring  
**Performance Goals**: Full autocomplete test suite executes in <60 seconds  
**Constraints**: No modifications to autocomplete production code; tests validate documented behavior  
**Scale/Scope**: 283 test cases across 35 categories

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| **I. Test-Driven Development** | ✅ PASS | Tests written to validate documented hypothesis, not match existing code |
| **II. Dependency Injection** | ✅ PASS | Test harness uses constructor injection; providers are mocked via DI |
| **III. Security by Design** | N/A | Testing infrastructure, no security concerns |
| **IV. Follow Existing Patterns** | ✅ PASS | Reuses MockFileSystem, Moq patterns, TestEnvironment for client/server |
| **V. Integration Testing** | ✅ PASS | Remote completion tests use existing SignalR test infrastructure |

### Existing Infrastructure to Reuse

| Component | Location | Purpose |
|-----------|----------|---------|
| `MockFileSystem` | TestableIO.System.IO.Abstractions.TestingHelpers | File path completion tests (TC-9.x) |
| `TestEnvironment` | BitPantry.CommandLine.Tests.Remote.SignalR/Environment/ | Client-server integration tests |
| `Mock<ICompletionProvider>` | Moq | Async provider mocking, delay simulation |
| `VirtualConsole` | BitPantry.VirtualConsole/ | Core terminal emulation (ANSI processing, screen buffer) |
| Existing test commands | BitPantry.CommandLine.Tests/Commands/ | Test command definitions |

### Infrastructure to Remove (Phase 0)

| Component | Location | Reason |
|-----------|----------|--------|
| `StepwiseTestRunner` | Tests/VirtualConsole/ | Replaced by AutoCompleteTestHarness |
| `ConsolidatedTestConsole` | Tests/VirtualConsole/ | Replaced by VirtualConsole |
| `CursorTracker` | Tests/VirtualConsole/ | VirtualConsole tracks cursor natively |
| `StepwiseTestRunnerAssertions` | Tests/VirtualConsole/ | Replaced by new assertion extensions |
| `VirtualConsoleInput` | Tests/VirtualConsole/ | Replaced by KeyboardSimulator |
| `Verify.MSTest` package | Tests.csproj | Snapshot testing not needed |
| `Spectre.Console.Testing` package | Tests.csproj | No longer needed for visual tests |
| All `AutoComplete/` tests | Tests/AutoComplete/ | 66 files completely replaced |
| All `Snapshots/` | Tests/Snapshots/ | Snapshot baselines removed |

## Project Structure

### Documentation (this feature)

```text
specs/012-virtualconsole-autocomplete-tests/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 output
├── checklists/
│   └── requirements.md  # Quality checklist
└── tasks.md             # Phase 2 output
```

### Source Code Changes

```text
# REMOVED - Legacy Test Infrastructure
BitPantry.CommandLine.Tests/
├── VirtualConsole/                    # DELETE ENTIRE DIRECTORY (7 files)
│   ├── StepwiseTestRunner.cs
│   ├── StepwiseTestRunnerAssertions.cs
│   ├── ConsolidatedTestConsole.cs
│   ├── ConsolidatedTestConsoleTests.cs
│   ├── CursorTracker.cs
│   ├── CursorTrackerTests.cs
│   └── VirtualConsoleInput.cs
├── Snapshots/                         # DELETE ENTIRE DIRECTORY (10 files)
│   ├── ModuleInitializer.cs
│   ├── RenderableSnapshotTests.cs
│   └── *.verified.txt
└── AutoComplete/                      # DELETE ENTIRE DIRECTORY (66 files)
    ├── Attributes/
    ├── Cache/
    ├── EdgeCases/
    ├── Feedback/
    ├── Ghost/
    ├── Integration/
    ├── Matching/
    ├── Orchestrator/
    ├── Providers/
    ├── Regression/
    ├── Rendering/
    ├── ResultLimiting/
    ├── Unit/
    └── Visual/

# ADDED - VirtualConsole Testing Project (NEW PROJECT)
BitPantry.VirtualConsole.Testing/          # NEW PROJECT (preserves zero-dependency core)
├── BitPantry.VirtualConsole.Testing.csproj
├── README.md                              # Testing documentation
├── AutoCompleteTestHarness.cs             # Main test harness
├── IKeyboardSimulator.cs                  # Keyboard input interface
├── KeyboardSimulator.cs                   # Keyboard input implementation
├── VirtualConsoleAnsiAdapter.cs           # IAnsiConsole → VirtualConsole bridge
├── VirtualConsoleAssertions.cs            # FluentAssertions for VirtualConsole
├── HarnessAssertions.cs                   # FluentAssertions for harness state
└── TestCommandBase.cs                     # Base class for test commands

# ADDED - New Test Files
BitPantry.CommandLine.Tests/
└── AutoComplete/                      # RECREATED with new tests
    ├── GhostTextTests.cs              # TC-1.1 through TC-1.16 (16 tests)
    ├── MenuNavigationTests.cs         # TC-2.1 through TC-2.18 (18 tests)
    ├── MenuFilteringTests.cs          # TC-3.1 through TC-3.15 (15 tests)
    ├── InputEditingTests.cs           # TC-4.1 through TC-4.10 (10 tests)
    ├── CommandCompletionTests.cs      # TC-5.1 through TC-5.4 (4 tests)
    ├── ArgumentNameTests.cs           # TC-6.1 through TC-6.10 (10 tests)
    ├── ArgumentValueTests.cs          # TC-7.1 through TC-7.10 (10 tests)
    ├── PositionalTests.cs             # TC-8.1 through TC-8.11 (11 tests)
    ├── FilePathTests.cs               # TC-9.1 through TC-9.12 (12 tests)
    ├── ViewportScrollingTests.cs      # TC-10.1 through TC-10.5 (5 tests)
    ├── GhostMenuInteractionTests.cs   # TC-11.1 through TC-11.3 (3 tests)
    ├── WorkflowTests.cs               # TC-12.1 through TC-12.4 (4 tests)
    ├── HistoryNavigationTests.cs      # TC-13.1 through TC-13.4 (4 tests)
    ├── EdgeCaseTests.cs               # TC-14.1 through TC-14.27 (27 tests)
    ├── VisualRenderingTests.cs        # TC-15.1 through TC-15.5 (5 tests)
    ├── SubmissionTests.cs             # TC-16.1 through TC-16.3 (3 tests)
    ├── RemoteCompletionTests.cs       # TC-17.x (existing infrastructure reused)
    ├── CachingTests.cs                # TC-18.1 through TC-18.7 (7 tests)
    ├── ProviderConfigTests.cs         # TC-19.1 through TC-19.20 (20 tests)
    ├── MatchRankingTests.cs           # TC-20.1 through TC-20.5 (5 tests)
    ├── ResultLimitingTests.cs         # TC-21.1 through TC-21.5 (5 tests)
    ├── TerminalEdgeCaseTests.cs       # TC-22.1 through TC-22.6 (6 tests)
    ├── KeyboardVariationTests.cs      # TC-23.1 through TC-23.8 (8 tests)
    ├── ContextSensitivityTests.cs     # TC-24.1 through TC-24.6 (6 tests)
    ├── AsyncBehaviorTests.cs          # TC-25.1 through TC-25.5 (5 tests)
    ├── QuotingEscapingTests.cs        # TC-26.1 through TC-26.8 (8 tests)
    ├── StatePersistenceTests.cs       # TC-30.1 through TC-30.5 (5 tests)
    ├── ProviderInteractionTests.cs    # TC-31.1 through TC-31.5 (5 tests)
    ├── VirtualConsoleIntegrationTests.cs # TC-32.1 through TC-32.6 (6 tests)
    ├── ConfigurationTests.cs          # TC-33.1 through TC-33.5 (5 tests)
    ├── ErrorFeedbackTests.cs          # TC-34.1 through TC-34.5 (5 tests)
    └── BoundaryValueTests.cs          # TC-35.1 through TC-35.6 (6 tests)
```

## Implementation Phases

### Phase 0: Legacy Removal & Stabilization

**Goal**: Clean slate - remove all legacy infrastructure, verify solution builds

**Tasks**:
1. Delete `BitPantry.CommandLine.Tests/AutoComplete/` directory (66 files)
2. Delete `BitPantry.CommandLine.Tests/VirtualConsole/` directory (7 files)
3. Delete `BitPantry.CommandLine.Tests/Snapshots/` directory (10 files)
4. Remove `Verify.MSTest` and `Spectre.Console.Testing` package references from test .csproj
5. Update any files that reference the deleted infrastructure (may be in other test files)
6. Build solution - verify no compilation errors
7. Run remaining tests - verify they still pass
8. Update CLAUDE.md testing section - remove references to deleted infrastructure

**Output**: Clean solution with no legacy autocomplete test infrastructure

### Phase 1: VirtualConsole Testing Project

**Goal**: Create separate testing project and build harness/assertion extensions

**Tasks**:
1. Create `BitPantry.VirtualConsole.Testing/` project with references to VirtualConsole, FluentAssertions, Spectre.Console
2. Implement `VirtualConsoleAnsiAdapter : IAnsiConsole` that:
   - Implements Spectre.Console's IAnsiConsole interface
   - Routes all Write operations through VirtualConsole.Write()
   - Converts Spectre renderables to ANSI strings for VirtualConsole
3. Implement `IKeyboardSimulator` interface
4. Implement `KeyboardSimulator` class
5. Implement `AutoCompleteTestHarness` class that:
   - Creates a VirtualConsole instance
   - Creates VirtualConsoleAnsiAdapter wrapping the VirtualConsole
   - Creates and configures AutoCompleteController with the adapter
   - Provides methods: `TypeText()`, `PressKey()`, `PressTab()`, `PressEnter()`
   - Exposes `VirtualConsole` for assertions
   - Exposes controller state (`IsMenuVisible`, `SelectedItem`, `Buffer`)
6. Implement `VirtualConsoleAssertions` FluentAssertions extensions:
   - `Should().ContainText("text")`
   - `Should().HaveCellWithStyle(row, col, attributes)`
   - `Should().HaveTextAt(row, col, "text")`
7. Implement `HarnessAssertions` FluentAssertions extensions:
   - `Should().HaveMenuVisible()` / `HaveMenuHidden()`
   - `Should().HaveSelectedItem("text")`
   - `Should().HaveGhostText("text")`
   - `Should().HaveBuffer("text")`
   - `Should().HaveBufferPosition(n)`
8. Create `TestCommandBase` for defining test commands
9. Create `README.md` documentation in project root
10. Add project to solution file
11. Add project reference from BitPantry.CommandLine.Tests to BitPantry.VirtualConsole.Testing
12. Build and verify no errors

**Output**: Complete testing infrastructure ready for test implementation

### Phase 2: Core Test Implementation (Incremental, Phased)

**Goal**: Implement tests in priority order, stabilizing each batch before proceeding

The implementation is divided into sub-phases based on test category priority and complexity:

#### Phase 2A: Foundation Tests (P1 - Critical Path)

Implement and stabilize before proceeding:

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| GhostTextTests.cs | TC-1.1 through TC-1.16 | 16 | Core harness |
| MenuNavigationTests.cs | TC-2.1 through TC-2.18 | 18 | Core harness |
| MenuFilteringTests.cs | TC-3.1 through TC-3.15 | 15 | Core harness |
| InputEditingTests.cs | TC-4.1 through TC-4.10 | 10 | Core harness |

**Total**: 59 tests
**Stabilization Gate**: All 59 tests pass before proceeding

#### Phase 2B: Command & Argument Tests (P2)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| CommandCompletionTests.cs | TC-5.1 through TC-5.4 | 4 | Phase 2A |
| ArgumentNameTests.cs | TC-6.1 through TC-6.10 | 10 | Phase 2A |
| ArgumentValueTests.cs | TC-7.1 through TC-7.10 | 10 | Phase 2A, Mock providers |
| PositionalTests.cs | TC-8.1 through TC-8.11 | 11 | Phase 2A |

**Total**: 35 tests
**Stabilization Gate**: All 94 tests pass (59 + 35)

#### Phase 2C: File System & Integration Tests (P2)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| FilePathTests.cs | TC-9.1 through TC-9.12 | 12 | MockFileSystem |
| ViewportScrollingTests.cs | TC-10.1 through TC-10.5 | 5 | Phase 2A |
| GhostMenuInteractionTests.cs | TC-11.1 through TC-11.3 | 3 | Phase 2A |
| WorkflowTests.cs | TC-12.1 through TC-12.4 | 4 | Phase 2B |
| HistoryNavigationTests.cs | TC-13.1 through TC-13.4 | 4 | InputLog |

**Total**: 28 tests
**Stabilization Gate**: All 122 tests pass (94 + 28)

#### Phase 2D: Edge Cases & Visual Tests (P2-P3)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| EdgeCaseTests.cs | TC-14.1 through TC-14.27 | 27 | Phases 2A-2C |
| VisualRenderingTests.cs | TC-15.1 through TC-15.5 | 5 | VirtualConsole style assertions |
| SubmissionTests.cs | TC-16.1 through TC-16.3 | 3 | Phase 2A |

**Total**: 35 tests
**Stabilization Gate**: All 157 tests pass (122 + 35)

#### Phase 2E: Provider & Caching Tests (P2-P3)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| RemoteCompletionTests.cs | TC-17.x | Variable | TestEnvironment (reuse existing) |
| CachingTests.cs | TC-18.1 through TC-18.7 | 7 | Mock cache |
| ProviderConfigTests.cs | TC-19.1 through TC-19.20 | 20 | Mock providers |
| MatchRankingTests.cs | TC-20.1 through TC-20.5 | 5 | Orchestrator |
| ResultLimitingTests.cs | TC-21.1 through TC-21.5 | 5 | Orchestrator |

**Total**: 37+ tests
**Stabilization Gate**: All 194+ tests pass

#### Phase 2F: Environment & Async Tests (P3)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| TerminalEdgeCaseTests.cs | TC-22.1 through TC-22.6 | 6 | VirtualConsole sizing |
| KeyboardVariationTests.cs | TC-23.1 through TC-23.8 | 8 | KeyboardSimulator |
| ContextSensitivityTests.cs | TC-24.1 through TC-24.6 | 6 | CommandRegistry |
| AsyncBehaviorTests.cs | TC-25.1 through TC-25.5 | 5 | Mock async providers |
| QuotingEscapingTests.cs | TC-26.1 through TC-26.8 | 8 | Parser integration |

**Total**: 33 tests
**Stabilization Gate**: All 227+ tests pass

#### Phase 2G: Final Tests (P3)

| Test File | Test Cases | Count | Dependencies |
|-----------|------------|-------|--------------|
| StatePersistenceTests.cs | TC-30.1 through TC-30.5 | 5 | State management |
| ProviderInteractionTests.cs | TC-31.1 through TC-31.5 | 5 | Multi-provider |
| VirtualConsoleIntegrationTests.cs | TC-32.1 through TC-32.6 | 6 | Full integration |
| ConfigurationTests.cs | TC-33.1 through TC-33.5 | 5 | Options pattern |
| ErrorFeedbackTests.cs | TC-34.1 through TC-34.5 | 5 | Error handling |
| BoundaryValueTests.cs | TC-35.1 through TC-35.6 | 6 | Limits testing |

**Total**: 32 tests
**Final Gate**: All 283 tests pass

### Phase 3: Documentation Update

**Goal**: Update all testing documentation to reflect new approach

**Tasks**:
1. Update CLAUDE.md testing section completely
2. Verify VirtualConsole/Testing/README.md is complete
3. Remove any stale documentation references to old infrastructure
4. Add examples of writing VirtualConsole-based tests

## Key Design Decisions

### Test Harness Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     AutoCompleteTestHarness                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌─────────────────┐    ┌──────────────────────────────────┐   │
│  │ KeyboardSimulator│───▶│      AutoCompleteController       │   │
│  └─────────────────┘    └──────────────────────────────────┘   │
│                                      │                          │
│                                      ▼                          │
│                         ┌──────────────────────────────────┐   │
│                         │      IAnsiConsoleOutput          │   │
│                         │   (writes ANSI to VirtualConsole)│   │
│                         └──────────────────────────────────┘   │
│                                      │                          │
│                                      ▼                          │
│                         ┌──────────────────────────────────┐   │
│                         │        VirtualConsole             │   │
│                         │   (80x24 buffer, ANSI parsing)   │   │
│                         └──────────────────────────────────┘   │
│                                      │                          │
│                                      ▼                          │
│                         ┌──────────────────────────────────┐   │
│                         │    Assertions (FluentAssertions)  │   │
│                         │ - ContainText, HaveCellWithStyle │   │
│                         │ - HaveGhostText, HaveMenuVisible │   │
│                         └──────────────────────────────────┘   │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Test Pattern

```csharp
[TestMethod]
public async Task TC_1_1_SingleCharacter_ShowsGhostCompletion()
{
    // Arrange: Create harness with "server" command registered
    using var harness = new AutoCompleteTestHarness()
        .WithCommand<ServerCommand>();
    
    // Act: Type "s"
    await harness.TypeText("s");
    
    // Assert: Ghost text "erver" appears in dim style
    harness.Should().HaveGhostText("erver");
    harness.VirtualConsole.GetCell(0, harness.PromptLength + 1)
        .Style.Attributes.Should().HaveFlag(CellAttributes.Dim);
}
```

### Failure Diagnostics

When assertions fail, include:
- ASCII-rendered VirtualConsole buffer (`VirtualConsole.GetScreenContent()`)
- Harness state (buffer content, cursor position, menu state)
- Standard stack trace

Example failure output:
```
Expected harness to have ghost text "erver" but found "".

VirtualConsole Buffer:
┌────────────────────────────────────────────────────────────────────────────────┐
│> s                                                                              │
│                                                                                 │
│                                                                                 │
└────────────────────────────────────────────────────────────────────────────────┘

Harness State:
  Buffer: "s"
  BufferPosition: 1
  IsMenuVisible: false
  GhostText: ""
```

## VirtualConsole Capability Verification

Based on analysis of `BitPantry.VirtualConsole`:

| Capability | Status | Notes |
|------------|--------|-------|
| ANSI color parsing | ✅ Supported | SGR sequences for 8/256/TrueColor |
| Dim/gray style (ghost text) | ✅ Supported | `CellAttributes.Dim` |
| Reverse style (selection) | ✅ Supported | `CellAttributes.Reverse` |
| Cursor position tracking | ✅ Supported | `CursorRow`, `CursorColumn` |
| Screen buffer query | ✅ Supported | `GetCell()`, `GetRow()`, `GetScreenContent()` |
| Line wrapping | ✅ Supported | Automatic at width boundary |
| Clear screen/line | ✅ Supported | ED (J) / EL (K) sequences |
| Cursor movement | ✅ Supported | CSI sequences A/B/C/D/H/f |

**No core VirtualConsole modifications required** for the documented test scenarios.

## Complexity Tracking

No constitution violations requiring justification.

## Risk Mitigation

| Risk | Mitigation |
|------|------------|
| Tests fail due to code bugs (expected) | Document bugs found; tests validate hypothesis not code |
| Harness doesn't integrate cleanly with AutoCompleteController | Phase 1 includes integration verification before test implementation |
| Performance regression | Track test execution time; target <60s for full suite |
| Missing test infrastructure | Stabilization gates between phases catch issues early |

## Success Criteria Verification

| Criterion | Verification Method |
|-----------|---------------------|
| SC-001: 283 tests implemented | Count test methods in AutoComplete/ directory |
| SC-002: Legacy removed | Verify directories don't exist |
| SC-003: Solution builds | `dotnet build` succeeds |
| SC-004: <60s execution | `dotnet test` timing |
| SC-005: 1:1 test mapping | Test method names match TC-X.Y |
| SC-006: Separate namespace | VirtualConsole.Testing namespace exists |
| SC-007: CLAUDE.md updated | Manual review |
| SC-008: 5+ bugs found | Track failed tests due to code bugs |
| SC-009: Tests correct | Failed tests are code bugs not test bugs |
