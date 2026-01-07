# Research: VirtualConsole Autocomplete Tests

**Date**: January 6, 2026  
**Feature**: 012-virtualconsole-autocomplete-tests

## Summary

This document captures research findings from analyzing the existing test infrastructure, VirtualConsole capabilities, and integration requirements for the test replacement effort.

---

## 1. Existing Infrastructure Analysis

### 1.1 Legacy Test Infrastructure (TO BE REMOVED)

| Component | Location | Lines of Code | Purpose |
|-----------|----------|---------------|---------|
| `StepwiseTestRunner.cs` | Tests/VirtualConsole/ | 607 | Step-by-step keystroke simulation |
| `StepwiseTestRunnerAssertions.cs` | Tests/VirtualConsole/ | ~200 | FluentAssertions extensions |
| `ConsolidatedTestConsole.cs` | Tests/VirtualConsole/ | 207 | Spectre.Console wrapper |
| `ConsolidatedTestConsoleTests.cs` | Tests/VirtualConsole/ | ~100 | Unit tests for console |
| `CursorTracker.cs` | Tests/VirtualConsole/ | ~150 | ANSI cursor position tracking |
| `CursorTrackerTests.cs` | Tests/VirtualConsole/ | ~100 | Unit tests for tracker |
| `VirtualConsoleInput.cs` | Tests/VirtualConsole/ | ~150 | Input queue simulation |

**Total**: ~1,514 lines of infrastructure code to remove

### 1.2 Legacy Test Files (TO BE REMOVED)

**AutoComplete directory breakdown:**

| Subdirectory | File Count | Purpose |
|--------------|------------|---------|
| Attributes/ | 2 | Attribute system tests |
| Cache/ | 2 | Caching behavior tests |
| EdgeCases/ | 1 | Boundary condition tests |
| Feedback/ | 1 | User feedback tests |
| Ghost/ | 5 | Ghost text behavior tests |
| Integration/ | 4 | Remote/integration tests |
| Matching/ | 1 | Match ranking tests |
| Orchestrator/ | 5 | Orchestrator behavior tests |
| Providers/ | 11 | Individual provider tests |
| Regression/ | 2 | Bug fix regression tests |
| Rendering/ | 8 | Visual rendering tests |
| ResultLimiting/ | 1 | Result limit tests |
| Unit/ | 3 | Unit tests |
| Visual/ | 20 | Visual/UX tests |

**Total**: 66 files to remove

### 1.3 Package Dependencies to Remove

From `BitPantry.CommandLine.Tests.csproj`:
- `Verify.MSTest` - Snapshot testing framework
- `Spectre.Console.Testing` - Spectre test console

---

## 2. VirtualConsole Capability Analysis

### 2.1 Core Features (Confirmed Working)

| Feature | Implementation | Test Applicability |
|---------|---------------|-------------------|
| ANSI CSI parsing | `AnsiSequenceParser` | Cursor, colors, styles |
| Cursor tracking | `CursorRow`, `CursorColumn` | Position assertions |
| Screen buffer | `ScreenBuffer` 2D array | Content assertions |
| Cell styles | `CellStyle` with attributes | Style assertions |
| Text attributes | `CellAttributes` flags | Ghost text (Dim), selection (Reverse) |
| Color support | ConsoleColor, 256, RGB | Visual validation |
| Line wrapping | Auto-wrap at width | Long text handling |
| Clear operations | ED/EL sequences | Menu clear, screen reset |

### 2.2 ANSI Sequences Supported

**Cursor Movement:**
- `\x1b[nA` - Up
- `\x1b[nB` - Down
- `\x1b[nC` - Forward
- `\x1b[nD` - Back
- `\x1b[n;mH` - Position

**Styles (SGR):**
- `\x1b[0m` - Reset
- `\x1b[1m` - Bold
- `\x1b[2m` - Dim (ghost text)
- `\x1b[7m` - Reverse (selection)
- `\x1b[30-37m` - Foreground colors
- `\x1b[40-47m` - Background colors
- `\x1b[38;5;nm` - 256-color foreground
- `\x1b[48;5;nm` - 256-color background
- `\x1b[38;2;r;g;bm` - RGB foreground
- `\x1b[48;2;r;g;bm` - RGB background

**Erase:**
- `\x1b[J` / `\x1b[0J` - Clear below
- `\x1b[1J` - Clear above
- `\x1b[2J` - Clear all
- `\x1b[K` / `\x1b[0K` - Clear to end of line
- `\x1b[1K` - Clear to start of line
- `\x1b[2K` - Clear entire line

### 2.3 Key APIs for Testing

```csharp
// Screen content
string content = console.GetScreenContent();  // With line breaks
string text = console.GetScreenText();        // Continuous

// Individual cells
ScreenCell cell = console.GetCell(row, col);
char character = cell.Character;
CellStyle style = cell.Style;
CellAttributes attrs = style.Attributes;

// Rows
ScreenRow row = console.GetRow(rowIndex);
string rowText = row.GetText();

// Cursor
int cursorRow = console.CursorRow;
int cursorCol = console.CursorColumn;

// Dimensions
int width = console.Width;
int height = console.Height;
```

### 2.4 No Modifications Required

All test scenarios in `autocomplete-test-cases.md` can be validated with existing VirtualConsole capabilities. No core changes needed.

---

## 3. Existing Test Infrastructure to Reuse

### 3.1 MockFileSystem (System.IO.Abstractions.TestingHelpers)

**Already in use** in current tests for file path completion:

```csharp
// Pattern from RenderableSnapshotTests.cs
private static MockFileSystem CreateMockFileSystem()
{
    var mockFileSystem = new MockFileSystem();
    mockFileSystem.AddDirectory("bin");
    mockFileSystem.AddDirectory("obj");
    mockFileSystem.AddFile("README.md", new MockFileData("# Readme"));
    return mockFileSystem;
}
```

### 3.2 TestEnvironment (Client-Server Testing)

**Located in**: `BitPantry.CommandLine.Tests.Remote.SignalR/Environment/`

Provides:
- `TestServer` - In-memory ASP.NET test server
- `ConsolidatedTestConsole` - Console for CLI (needs replacement with VirtualConsole)
- `ApiKey` / `ClientId` - Per-test isolation
- Automatic SignalR connection setup

**Usage pattern:**
```csharp
using var env = new TestEnvironment();
await env.Cli.ConnectToServer(env.Server);
// ... run tests
```

**Modification needed**: Replace `ConsolidatedTestConsole` with VirtualConsole in TestEnvironment for remote autocomplete tests.

### 3.3 Mock Providers (Moq)

**Pattern established** in existing tests:

```csharp
_mockProvider = new Mock<ICompletionProvider>();
_mockProvider.Setup(p => p.Priority).Returns(100);
_mockProvider.Setup(p => p.CanHandle(It.IsAny<CompletionContext>())).Returns(true);
_mockProvider
    .Setup(p => p.GetCompletionsAsync(It.IsAny<CompletionContext>(), It.IsAny<CancellationToken>()))
    .ReturnsAsync(CompletionResult.Empty);
```

### 3.4 Test Commands

**Located in**: `BitPantry.CommandLine.Tests/Commands/`

Existing test command definitions can be reused for the new tests.

---

## 4. Test Harness Design Decisions

### 4.1 AutoCompleteTestHarness Responsibilities

1. **VirtualConsole management**: Create/configure 80x24 buffer
2. **AutoCompleteController setup**: Wire up providers, registry, cache
3. **Keyboard simulation**: `TypeText()`, `PressKey()`, `PressTab()`
4. **State exposure**: `IsMenuVisible`, `Buffer`, `SelectedItem`
5. **Console output bridge**: Route ANSI output to VirtualConsole

### 4.2 Integration Pattern

The harness needs to bridge `AutoCompleteController` (which uses `IAnsiConsole`) to `VirtualConsole`:

```
AutoCompleteController
        │
        ▼
   IAnsiConsole adapter (captures ANSI strings)
        │
        ▼
   VirtualConsole.Write(ansiString)
        │
        ▼
   Screen buffer (assertions)
```

**Key insight**: AutoCompleteController uses Spectre.Console's `IAnsiConsole`. We need an adapter that captures the raw ANSI output and feeds it to VirtualConsole.

### 4.3 Assertion Library Structure

```csharp
// VirtualConsole assertions
harness.VirtualConsole.Should()
    .ContainText("server")
    .And.HaveCellWithStyle(0, 5, CellAttributes.Dim);

// Harness state assertions
harness.Should()
    .HaveBuffer("server ")
    .And.HaveMenuVisible()
    .And.HaveSelectedItem("connect");
```

---

## 5. Test Categories & Dependencies

### 5.1 Dependency Graph

```
Phase 2A: Foundation (no external deps)
    ↓
Phase 2B: Arguments (depends on 2A)
    ↓
Phase 2C: File/Integration (depends on 2B + MockFileSystem)
    ↓
Phase 2D: Edge Cases (depends on 2A-2C)
    ↓
Phase 2E: Providers/Caching (depends on 2A + Mock providers)
    ↓
Phase 2F: Async/Environment (depends on 2A + async mocking)
    ↓
Phase 2G: Final (full integration)
```

### 5.2 Test Count by Phase

| Phase | Tests | Cumulative |
|-------|-------|------------|
| 2A | 59 | 59 |
| 2B | 35 | 94 |
| 2C | 28 | 122 |
| 2D | 35 | 157 |
| 2E | 37+ | 194+ |
| 2F | 33 | 227+ |
| 2G | 32 | 283 |

---

## 6. Clarifications Applied (from spec.md)

| Question | Answer | Impact |
|----------|--------|--------|
| Diagnostic output | Stack trace + VirtualConsole.GetScreenContent() | Failure messages include ASCII buffer |
| Mock filesystem | Use existing MockFileSystem pattern | No new dependencies |
| Async testing | Use Moq with CancellationTokenSource | Existing patterns sufficient |

---

## 7. Architectural Decisions

### 7.1 IAnsiConsole Adapter

**Decision**: Option A confirmed - Create `VirtualConsoleAnsiAdapter : IAnsiConsole` that writes to VirtualConsole.

**Rationale**:
- No production code changes required
- Clean adapter pattern maintains separation of concerns
- Testing project contains the adapter
- Adapter itself can be unit tested

**Implementation**:
```csharp
// In BitPantry.VirtualConsole.Testing/VirtualConsoleAnsiAdapter.cs
public class VirtualConsoleAnsiAdapter : IAnsiConsole
{
    private readonly VirtualConsole _console;
    
    public void Write(IRenderable renderable)
    {
        // Convert Spectre renderable to ANSI string
        var ansiOutput = RenderToAnsi(renderable);
        _console.Write(ansiOutput);
    }
    // ... other IAnsiConsole members
}
```

### 7.2 Testing Project Location

**Decision**: Option A confirmed - Create separate `BitPantry.VirtualConsole.Testing` project.

**Rationale**:
- VirtualConsole core is netstandard2.0 with zero dependencies (intentional)
- Testing extensions require FluentAssertions, Spectre.Console, and reference test framework APIs
- Separate project preserves zero-dependency core package integrity
- Follows standard .NET testing library patterns (xUnit.Abstractions, NUnit.Framework, etc.)

**Structure**:
```
BitPantry.VirtualConsole/              # Core (unchanged, zero dependencies)
├── VirtualConsole.cs
├── ScreenBuffer.cs
└── ...

BitPantry.VirtualConsole.Testing/      # NEW PROJECT
├── BitPantry.VirtualConsole.Testing.csproj
├── VirtualConsoleAnsiAdapter.cs       # IAnsiConsole → VirtualConsole bridge
├── AutoCompleteTestHarness.cs
├── VirtualConsoleAssertions.cs        # FluentAssertions extensions
├── HarnessAssertions.cs
├── KeyboardSimulator.cs
└── ...
```

**Dependencies** (Testing project only):
- ProjectReference: BitPantry.VirtualConsole
- PackageReference: FluentAssertions
- PackageReference: Spectre.Console

### 7.3 Remote Completion Tests (TC-17.x)

The existing `TestEnvironment` uses `ConsolidatedTestConsole`. For VirtualConsole-based tests:
- Option A: Modify TestEnvironment to accept IConsole abstraction
- Option B: Create separate harness for remote tests
- Option C: Test remote completion at provider level (mock SignalRServerProxy)

**Recommendation**: Option C for most tests, Option A only if needed for full E2E remote scenarios.

---

## 8. References

- [VirtualConsole README](../../BitPantry.VirtualConsole/README.md)
- [VirtualConsole ARCHITECTURE](../../BitPantry.VirtualConsole/ARCHITECTURE.md)
- [Autocomplete Test Cases](../../autocomplete-test-cases.md)
- [Spec](spec.md)
