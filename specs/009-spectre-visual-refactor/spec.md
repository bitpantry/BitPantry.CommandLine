# Feature Specification: Spectre Visual Rendering Refactor

**Feature Branch**: `009-spectre-visual-refactor`  
**Created**: January 3, 2026  
**Updated**: January 4, 2026  
**Status**: In Progress (Phase 9)  
**Input**: Refactor testing infrastructure to consolidate on Spectre.Console.Testing and refactor autocomplete menu and ghost text to use Spectre Renderable and LiveRenderable patterns for consistent testable visual rendering

---

## Clarifications

### Session 2026-01-03

- Q: What should happen to the existing `VirtualAnsiConsole.cs` file after consolidation? → A: Delete immediately after all tests pass with new infrastructure (clean break; code recoverable from repo history if needed)
- Q: Should snapshot tests capture raw ANSI escape sequences or stripped plain text? → A: Raw ANSI sequences (full fidelity, catches style bugs)
- Q: Should StepwiseTestRunner be kept, replaced, or supplemented? → A: Keep StepwiseTestRunner AND add Spectre-style completion tests; refactor StepwiseTestRunner to use consolidated console infrastructure
- Q: Should menu layout change from horizontal to vertical? → A: Switch to vertical layout (one item per line, like Spectre's SelectionPrompt) - matches Spectre pattern, simplifies LiveRenderable implementation

### Session 2026-01-04 (Testing Infrastructure Gap)

- Q: Why did "visual tests" fail to catch the menu filter highlighting bug? → A: **Critical finding**: Tests in `AutoComplete/Visual/` are mislabeled. They test **controller state** (IsMenuVisible, SelectedIndex, Buffer) but NOT rendered ANSI output. They use `StepwiseTestRunner` which wraps `ConsolidatedTestConsole` but the factory in `VisualTestBase.CreateRunner()` does NOT call `.EmitAnsiSequences()`.
- Q: Should we enable ANSI emission by default? → A: Yes. Tests that don't need ANSI can ignore the output; tests that DO need it have it available. Matches how the real console works.
- Q: Is `SpectreTestHelper.cs` needed? → A: No - confirmed zero external usages. It was created but never adopted. Delete it.
- Q: Should testing documentation be consolidated? → A: Yes. Create a comprehensive testing guide in the quickstart and reference it from CLAUDE.md. Delete the outdated `.specify/memory/testing-patterns.md`.

---

## Pre-Research & Technical Strategy

This section documents the research and planning completed prior to specification, providing context for the planning agent to validate and build upon.

### Problem Statement

The autocomplete menu rendering has persistent visual bugs (phantom/duplicate lines during Tab/arrow navigation) despite multiple fix attempts. The current approach uses manual ANSI cursor math which is fragile and difficult to test. Meanwhile, Spectre.Console has battle-tested patterns for exactly this problem.

### Previous Fix Attempts

Multiple approaches were tried before deciding on this refactor:

1. **DEC Save/Restore Cursor (ESC7/ESC8)** - Failed because these sequences don't work reliably across all terminals and don't handle terminal scrolling
2. **CSI Sequences (CUU/CUD/EL)** - Implemented but revealed deeper issues with cursor math when menu height changes
3. **Max Height Tracking (`_maxMenuLineCount`)** - Added to track maximum menu height ever rendered, mimicking Spectre's `SegmentShape.Inflate()` pattern
4. **Padding Logic** - Added to `RenderMenu()` to always render to max height, preventing leftover lines
5. **Growth Detection Fallback** - Current workaround: when menu grows beyond max height, fall back to full `ClearMenu()` + `RenderMenu()` to avoid cursor math issues

**The Core Problem**: When menu height GROWS (e.g., 2→3 lines or 2→10 lines), writing new content causes terminal scrolling. This scrolling shifts all content up, but our cursor position tracking doesn't account for this shift, resulting in cursor math errors that leave phantom lines.

### Why Horizontal Layout is Problematic

The current horizontal menu layout (`item1  [item2]  item3  (+2 more)`) adds significant complexity:

1. **Scroll indicators** - `(+N before)` and `(+N more)` require calculating visible viewport on a single line
2. **Line length tracking** - Must track `_menuLineLength` separately from content
3. **Cursor positioning** - Horizontal scrolling within a line is harder to manage than vertical line-by-line rendering
4. **Testing complexity** - Harder to assert expected content when items are space-separated on one line

Switching to **vertical layout** (one item per line) aligns with Spectre's `SelectionPrompt` pattern and simplifies all of these concerns.

### Spectre's SelectionPrompt Limitation

Spectre.Console's `SelectionPrompt` cannot be used directly because:

1. **Blocking behavior** - `SelectionPrompt.Show(console)` blocks until user selects, preventing integration with our keystroke-by-keystroke input handling
2. **No intermediate state access** - Cannot inspect menu state between keystrokes for testing
3. **Different input model** - We need to integrate autocomplete with an active input line, not replace it

**Solution**: Replicate Spectre's internal `LiveRenderable` pattern without using their blocking prompts. This gives us the proven cursor management while maintaining our interactive input model.

### Research Findings

#### 1. Current Testing Infrastructure Inventory

**Testing Frameworks & Packages Currently Used:**

| Package | Version | Purpose |
|---------|---------|---------|
| MSTest.TestFramework | 3.6.1 | Test framework |
| MSTest.TestAdapter | 3.6.1 | Test adapter |
| FluentAssertions | 6.12.0 | Fluent assertion library |
| Moq | 4.20.72 | Mocking framework |
| TestableIO.System.IO.Abstractions.TestingHelpers | 21.2.1 | File system mocking |
| Microsoft.NET.Test.Sdk | 17.11.1 | Test SDK |
| Spectre.Console.Testing | 0.54.0 | Spectre test console (added in Phase 1) |
| Verify.MSTest | 26.0.0 | Snapshot testing (added in Phase 1) |

**⚠️ CRITICAL GAP DISCOVERED (2026-01-04)**: The "visual tests" in `AutoComplete/Visual/` are actually **behavioral state tests**, NOT true visual output tests. They verify controller state (IsMenuVisible, SelectedIndex, Buffer) but do NOT verify rendered ANSI output. See "Testing Infrastructure Gap Analysis" section below.

**Current Test Structure:**
```
BitPantry.CommandLine.Tests/
├── VirtualConsole/                          # Core test infrastructure
│   ├── ConsolidatedTestConsole.cs          # Spectre TestConsole wrapper with cursor tracking
│   ├── CursorTracker.cs                    # ANSI cursor position parsing
│   ├── StepwiseTestRunner.cs               # Step-by-step keystroke test harness
│   ├── StepwiseTestRunnerAssertions.cs     # FluentAssertions extensions
│   └── SpectreTestHelper.cs                # DEAD CODE - never used (delete in Phase 9)
├── AutoComplete/
│   ├── Visual/                             # ⚠️ Mislabeled: tests state, NOT visual output
│   │   ├── VisualTestBase.cs              # Factory missing .EmitAnsiSequences() (fix in Phase 9)
│   │   ├── MenuBehaviorTests.cs           # Tab/arrow/Enter/Escape navigation
│   │   ├── GhostBehaviorTests.cs          # Ghost text display/acceptance
│   │   ├── InputEditingTests.cs           # Typing, backspace, cursor movement
│   │   ├── WorkflowTests.cs               # End-to-end completion workflows
│   │   ├── EdgeCaseTests.cs               # Complex input scenarios
│   │   ├── ViewportScrollingTests.cs      # Menu scrolling behavior
│   │   └── MenuRenderingBugTests.cs       # Bug reproduction tests
│   ├── Unit/                               # Unit tests for core entities
│   ├── Ghost/                              # Ghost text specific tests
│   └── Integration/                        # Remote/async integration tests
```

#### 2. VirtualAnsiConsole Analysis

**Current Implementation Features:**
- Virtual buffer tracking written content by line
- Virtual cursor tracking (column, line)
- ANSI escape sequence parsing (CSI A/B/C/D/J/K)
- DEC cursor save/restore (ESC7/ESC8)
- Carriage return/line break handling
- Terminal width simulation (default 80x24)
- Line wrapping simulation

**ANSI Sequences Handled:**
- `ESC[nA` - Cursor Up
- `ESC[nB` - Cursor Down
- `ESC[nC` - Cursor Forward
- `ESC[nD` - Cursor Back
- `ESC[J` / `ESC[0J` - Erase to end of screen
- `ESC[K` - Erase to end of line
- `ESC7` / `ESC8` - Save/Restore cursor (DEC)

**Strengths vs Spectre's TestConsole:**
- VirtualAnsiConsole has BETTER cursor tracking than Spectre's TestConsole
- VirtualAnsiConsole has full line buffer management
- VirtualAnsiConsole has terminal wrapping simulation

**Weaknesses:**
- No IRenderHook support
- Limited RenderPipeline usage
- ANSI parsing is reactive (after write, not during segment enumeration)
- No vertical overflow handling (unlike Spectre's LiveRenderable)

#### 3. Spectre.Console Testing Patterns

**Spectre.Console.Testing Package (v0.54.0) Provides:**

```csharp
// TestConsole - IAnsiConsole implementation for testing
public sealed class TestConsole : IAnsiConsole, IDisposable
{
    public string Output => _writer.ToString();
    public IReadOnlyList<string> Lines => Output.NormalizeLineEndings().Split('\n');
    public TestConsoleInput Input { get; }
    public bool EmitAnsiSequences { get; set; }
}

// TestConsoleInput - Input simulation
public sealed class TestConsoleInput : IAnsiConsoleInput
{
    public void PushText(string input);
    public void PushTextWithEnter(string input);
    public void PushCharacter(char input);
    public void PushKey(ConsoleKey input);
    public void PushKey(ConsoleKeyInfo consoleKeyInfo);
}

// Fluent configuration extensions
console.Colors(ColorSystem.TrueColor);
console.SupportsAnsi(true);
console.Interactive();
console.Width(80);
console.Height(24);
console.EmitAnsiSequences();
```

**Spectre Testing Patterns:**

Pattern 1 - Output Verification with ANSI Sequences:
```csharp
[Fact]
public void Should_Return_Correct_Code()
{
    var console = new TestConsole()
        .Colors(ColorSystem.TrueColor)
        .EmitAnsiSequences();

    console.Write("Hello", new Style().Foreground(new Color(128, 0, 128)));
    console.Output.ShouldBe("\u001b[38;2;128;0;128mHello\u001b[0m");
}
```

Pattern 2 - Progress/Live Testing:
```csharp
[Fact]
public void Should_Render_Task_Correctly()
{
    var console = new TestConsole()
        .Width(10)
        .Interactive()
        .EmitAnsiSequences();

    var progress = new Progress(console)
        .Columns(new[] { new ProgressBarColumn() })
        .AutoRefresh(false);

    progress.Start(ctx => ctx.AddTask("foo"));

    // Verify cursor hide/show and content clearing sequences
    console.Output.NormalizeLineEndings().ShouldBe(
        "[?25l" +           // Hide cursor
        "          \n" +    // Top padding
        "[38;5;8m━━━━━━━━━━[0m\n" +
        "          " +
        "[2K[1A[2K[1A[2K[?25h");    // Clear + show cursor
}
```

Pattern 3 - Snapshot/Verification Testing:
```csharp
[Fact]
[Expectation("Render")]
public async Task Should_Render_Correctly()
{
    var console = new TestConsole();
    var chart = Fixture.GetChart();
    console.Write(chart);
    await Verifier.Verify(console.Output);  // Compares to .verified.txt file
}
```

#### 4. Spectre's LiveRenderable Pattern (Critical for Menu Fix)

Spectre's internal `LiveRenderable` class handles in-place updates:

```csharp
internal sealed class LiveRenderable : Renderable
{
    private SegmentShape? _shape;  // Tracks max height via Inflate()
    
    public IRenderable PositionCursor(RenderOptions options)
    {
        // Returns: CR + CUU(height - 1)
        // Moves cursor to start of renderable area
    }
    
    public IRenderable RestoreCursor()
    {
        // Returns: CR + EL(2) + (CUU(1) + EL(2)).Repeat(height - 1)
        // Clears all lines of the renderable area
    }
}

// SegmentShape struct for dimension tracking
internal struct SegmentShape
{
    public int Width { get; }
    public int Height { get; }
    
    public SegmentShape Inflate(SegmentShape other)
    {
        // Returns new shape with max of both dimensions
        return new SegmentShape(
            Math.Max(Width, other.Width),
            Math.Max(Height, other.Height));
    }
    
    public void Apply(RenderOptions options, List<Segment> segments)
    {
        // Pads content to this shape's dimensions
    }
}
```

**Key Concepts:**
- `Inflate()` tracks the maximum height ever rendered (like our `_maxMenuLineCount`)
- Always renders to max height (padding if content is shorter)
- Uses cursor movement to update in-place without scrolling
- `PositionCursor()` uses `\r` + `CUU(height-1)`
- `RestoreCursor()` uses `\r` + `EL(2)` + `(CUU(1) + EL(2)).Repeat(height-1)`

#### 5. Current AutoCompleteController Menu Rendering Issues

From `AutoCompleteController.cs`:
- Manual ANSI cursor control in lines 220-260 using raw escape codes
- Custom height tracking with `_maxMenuLineCount` (mirrors Spectre's SegmentShape)
- Complex update logic in `UpdateMenuInPlace()` duplicates Spectre patterns
- Recent fix added growth detection fallback (when menu grows beyond max, falls back to clear+re-render)

**Problem**: When menu grows (e.g., 2→3 lines), terminal scrolling breaks cursor math. Current workaround detects growth and falls back to full clear+re-render.

#### 6. Testing Pattern Differences: Spectre vs BitPantry

| Aspect | Spectre's TestConsole | BitPantry's StepwiseTestRunner |
|--------|----------------------|-------------------------------|
| **Input Model** | Queue all inputs upfront, run to completion | Process one keystroke at a time, inspect state between |
| **State Inspection** | Only final output | Menu state, ghost state, cursor position after each key |
| **Use Case** | "Push Tab, Enter → verify final result" | "Press Tab → is menu visible? Press Down → is index 1?" |
| **Intermediate States** | Not accessible | Full access to controller, buffer, cursor |
| **Test Pattern** | `console.Input.PushKey(); prompt.Show(); verify output` | `runner.PressKey(); runner.Should().HaveMenuVisible()` |

**Decision**: Keep both patterns. StepwiseTestRunner is valuable for debugging complex interaction sequences and verifying intermediate visual states. Spectre-style tests are simpler for "happy path" scenarios where only the final result matters.

#### 7. Gap Analysis Summary

| Feature | BitPantry Has | Spectre Has | Gap |
|---------|---------------|-------------|-----|
| Basic IAnsiConsole mock | ✅ VirtualAnsiConsole | ✅ TestConsole | Similar |
| Input queue | ✅ VirtualConsoleInput | ✅ TestConsoleInput | Similar |
| Fluent extensions | ✅ Basic | ✅ Comprehensive | Minor gap |
| ANSI sequence parsing | ✅ Partial | ❌ NoopCursor | BitPantry better |
| Cursor position tracking | ✅ Full | ❌ Not tracked | BitPantry better |
| Line buffer management | ✅ Full | ❌ Just string output | BitPantry better |
| Terminal wrapping simulation | ✅ Basic | ❌ None | BitPantry better |
| Snapshot testing | ❌ None | ✅ Verifier integration | **Gap** |
| IRenderable testing | ⚠️ Limited | ✅ Full | Moderate gap |
| LiveRenderable pattern | ❌ Custom implementation | ✅ Internal class | **Critical gap** |
| StepwiseTestRunner | ✅ Custom | ❌ None | BitPantry unique |

---

### Testing Infrastructure Gap Analysis (Session 2026-01-04)

This section documents a critical discovery made while debugging spec-010 (menu filter highlighting). The testing infrastructure had a fundamental flaw that allowed visual bugs to slip through.

#### The Bug That Exposed the Gap

**Symptom**: Menu filter highlighting works when typing, but **disappears when pressing Up/Down arrows** to change selection.

**Root Cause**: `AutoCompleteController.UpdateMenuInPlace()` (line 314) calls `GetMenuItemStrings()` which returns `List<string>` (just DisplayText), losing the `CompletionItem` objects that contain `MatchRanges`. The renderer needs `CompletionItem` to apply highlighting.

**Why Tests Didn't Catch It**: The "visual tests" in `AutoComplete/Visual/` only assert on:
- `runner.IsMenuVisible` → boolean
- `runner.MenuSelectedIndex` → integer  
- `runner.Buffer` → string content

They **never check** whether ANSI styling codes are present in the rendered output.

#### The Mislabeling Problem

| Folder | Name Implies | Actually Tests | ANSI Output Captured |
|--------|--------------|----------------|---------------------|
| `AutoComplete/Visual/` | Visual rendering | Controller state | ❌ No |
| `Snapshots/` | File diffs | Actual rendered output | ✅ Yes |
| Renderable unit tests | Component behavior | Segment content | ✅ Yes |

The tests in `AutoComplete/Visual/` are **behavioral integration tests** that verify state transitions, NOT visual output tests.

#### Why EmitAnsiSequences() Wasn't Enabled

The factory method in `VisualTestBase.CreateRunner()` (line 246):
```csharp
var console = new ConsolidatedTestConsole().Interactive();  // ❌ Missing .EmitAnsiSequences()
```

This was intentional in Phase 3 (T017 deferred) because:
1. `VirtualAnsiConsole` was retained for backward compatibility
2. `ConsolidatedTestConsole` was made available but not mandatory
3. The factory was updated to use `ConsolidatedTestConsole` but without ANSI emission to minimize test changes

**Result**: Tests pass because they check state, but they can't detect when rendered output is missing expected styling.

#### Testing Capabilities Matrix (After Analysis)

| Capability | Tool | State Testing | ANSI Output Testing |
|------------|------|---------------|---------------------|
| Step-by-step keystroke simulation | `StepwiseTestRunner` | ✅ Full | ❌ Not enabled |
| Snapshot comparison | `Verify.MSTest` | ❌ One-shot | ✅ Full |
| Queue-and-run | `SpectreTestHelper` | ❌ No intermediate | ✅ Full |
| Renderable unit tests | Direct instantiation | N/A | ✅ Full |

**The Missing Pattern**: Step-by-step testing with ANSI output verification. This requires enabling `.EmitAnsiSequences()` in the factory.

#### Dead Code Identified

- `SpectreTestHelper.cs` - Created in Phase 3 (T018.1) but **zero external usages**. Never adopted.
- `GetMenuItemStrings()` - After fixing the highlighting bug, this method in `AutoCompleteController` becomes dead code.

#### Documentation Gaps

1. **No testing guide**: No central documentation explaining when to use which test pattern
2. **Outdated references**: `.specify/memory/testing-patterns.md` still references deleted `VirtualAnsiConsole`
3. **CLAUDE.md incomplete**: Doesn't explain testing categories or ANSI vs state testing
4. **Misleading folder name**: `AutoComplete/Visual/` suggests visual testing but doesn't do visual testing

#### Resolution (Phase 9)

See Phase 9 in tasks.md for the concrete steps to address these gaps.

---

### Technical Strategy (12-Step Plan)

The following implementation plan was developed based on the research above and refined through clarification:

1. **Add testing packages** - Add `Spectre.Console.Testing` (v0.54.0) and `Verify.MSTest` (v26.0.0) to `BitPantry.CommandLine.Tests.csproj`

2. **Create consolidated test console** - Create a new test console class that wraps Spectre's `TestConsole` while adding cursor tracking capabilities from `VirtualAnsiConsole`; preserve `StepwiseTestRunner` API compatibility with minor signature changes allowed

3. **Delete VirtualAnsiConsole** - After consolidated console passes all tests, delete `VirtualAnsiConsole.cs` (clean break; recoverable from git history)

4. **Configure snapshot testing** - Create `/BitPantry.CommandLine.Tests/Snapshots/` folder with `ModuleInitializer` to set `Verifier.DerivePathInfo()` directing all `.verified.txt` files (with raw ANSI sequences) there

5. **Create ANSI code helpers** - Add `AnsiCodes` static class with constants and builders (`HideCursor`, `ShowCursor`, `ClearLine`, `CursorUp(n)`, etc.) for readable test assertions

6. **Create `AutoCompleteMenuRenderable`** - Implement `Spectre.Console.Rendering.Renderable` with **vertical layout** (one item per line), handling viewport scrolling and selection highlighting with invert style

7. **Create `GhostTextRenderable`** - Implement `Renderable` for ghost text display, replacing manual cursor/style management in `GhostTextRenderer.cs`

8. **Create `MenuLiveRenderer` wrapper** - Implement `LiveRenderable` pattern with `SegmentShape.Inflate()` for max height tracking and proper cursor positioning for in-place updates

9. **Add isolated renderable tests** - Create unit tests for `AutoCompleteMenuRenderable` and `GhostTextRenderable` validating segment output using Spectre's `TestConsole`

10. **Add snapshot baselines** - Generate `.verified.txt` snapshots (raw ANSI) for key visual states: menu open, menu navigated, menu scrolled, ghost visible, ghost accepted

11. **Refactor controllers** - Update `AutoCompleteController.cs` (switch to vertical layout) and `GhostTextRenderer.cs` to use the new renderables, replacing manual ANSI cursor math with `LiveRenderable` pattern

12. **Run full regression suite** - Execute all ~130 visual tests plus new snapshot tests; update `StepwiseTestRunner` to use consolidated console; add Spectre-style "run to completion" test helpers as complement

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Developer Runs Visual Tests with Consistent Results (Priority: P1)

As a developer working on the autocomplete feature, I need visual tests that consistently pass or fail based on actual rendering behavior, not timing or terminal quirks, so I can confidently refactor the rendering code.

**Why this priority**: Without reliable tests, any refactoring is risky. The testing infrastructure must be solid before we can safely change the rendering implementation.

**Independent Test**: Run the existing ~130 visual tests after adding Spectre.Console.Testing. All tests should pass with the same behavior as before.

**Acceptance Scenarios**:

1. **Given** a test using the new consolidated console infrastructure, **When** the test runs, **Then** it produces identical pass/fail results to the current VirtualAnsiConsole-based tests
2. **Given** a visual test that checks menu rendering, **When** the test captures output, **Then** ANSI sequences are properly recorded for verification
3. **Given** the StepwiseTestRunner, **When** cursor position is queried, **Then** it returns accurate position reflecting all ANSI cursor movements

---

### User Story 2 - Developer Uses Snapshot Testing for Visual Regression (Priority: P1)

As a developer, I want to capture baseline snapshots of autocomplete visual states so that future changes that break rendering are caught automatically.

**Why this priority**: Snapshot testing provides the regression safety net needed to refactor with confidence. It catches visual bugs that unit tests might miss.

**Independent Test**: Create a snapshot test for "menu open with selection" state, modify the rendering code to break it, verify the test fails with a diff.

**Acceptance Scenarios**:

1. **Given** a snapshot test for menu rendering, **When** the test runs and output matches the `.verified.txt` file, **Then** the test passes
2. **Given** a snapshot test, **When** rendering output differs from baseline, **Then** the test fails with a clear diff showing what changed
3. **Given** the Snapshots folder, **When** new snapshot tests are added, **Then** `.verified.txt` files containing raw ANSI sequences are created in the designated folder

---

### User Story 3 - Menu Renders Without Phantom Lines During Navigation (Priority: P1)

As a user navigating the autocomplete menu with Tab/arrow keys, I need the menu to update cleanly in place without leaving duplicate or phantom lines on screen.

**Why this priority**: This is the core bug that motivated this entire refactor. Users experience visual glitches that make the autocomplete unusable in some scenarios.

**Independent Test**: Type a partial command, press Tab to open menu, press Down arrow 3 times, verify only one menu is visible and cursor is on correct item.

**Acceptance Scenarios**:

1. **Given** an open autocomplete menu, **When** I press Down arrow to navigate, **Then** the previous selection clears and new selection highlights without duplicating menu content
2. **Given** a menu that changes from 3 items to 5 items (filtering change), **When** the menu re-renders, **Then** the additional lines appear cleanly without cursor position errors
3. **Given** a menu that changes from 5 items to 2 items, **When** the menu re-renders, **Then** the extra lines are cleared and no phantom content remains

---

### User Story 4 - Ghost Text Renders and Clears Cleanly (Priority: P2)

As a user seeing ghost text suggestions, I need the ghost text to appear and disappear smoothly without leaving artifacts when I type or dismiss it.

**Why this priority**: Ghost text is a key UX feature but less problematic than the menu. Refactoring for consistency after the menu is fixed.

**Independent Test**: Type partial command that triggers ghost text, continue typing to dismiss it, verify no gray text artifacts remain.

**Acceptance Scenarios**:

1. **Given** a command input with ghost text visible, **When** I type a character that invalidates the ghost, **Then** the ghost text disappears completely
2. **Given** ghost text showing "nect" after "con", **When** I press Tab to accept, **Then** the ghost becomes regular text and cursor advances correctly
3. **Given** ghost text visible, **When** I press Escape, **Then** ghost text clears without affecting the input line

---

### User Story 5 - Developer Creates Isolated Renderable Tests (Priority: P2)

As a developer, I want to test menu and ghost rendering in isolation from the full autocomplete controller, so I can unit test the visual output without complex setup.

**Why this priority**: Isolated tests are faster, more focused, and easier to debug. They complement the integration tests.

**Independent Test**: Instantiate `AutoCompleteMenuRenderable` with a mock state, render to `TestConsole`, verify segment output contains expected content and styles.

**Acceptance Scenarios**:

1. **Given** an `AutoCompleteMenuRenderable` with 3 items and index 1 selected, **When** rendered to TestConsole, **Then** output contains item 2 with invert style
2. **Given** a `GhostTextRenderable` with suggestion "connect" from input "con", **When** rendered, **Then** output contains "nect" in dim gray style
3. **Given** a renderable test, **When** the test runs, **Then** it completes in under 10ms (no controller/DI overhead)

---

### Edge Cases

- What happens when menu viewport is exactly at terminal height boundary?
- How does system handle menu rendering when terminal is resized during display?
- What happens when ghost text would extend past terminal width?
- How does system handle rapid key presses that queue multiple render updates?
- What happens when menu items contain ANSI escape sequences (e.g., colored file names)?
- What happens when menu has more items than terminal height (vertical layout scrolling)?
- How does vertical menu handle very long item text that exceeds terminal width?
- What happens when menu grows from 3 to 10 items mid-session (LiveRenderable height tracking)?
- What happens when menu shrinks from 10 to 2 items (proper clearing of extra lines)?

---

## Requirements *(mandatory)*

### Functional Requirements

**Testing Infrastructure:**

- **FR-001**: System MUST add `Spectre.Console.Testing` package (v0.54.0) as a test dependency
- **FR-002**: System MUST add `Verify.MSTest` package for snapshot testing
- **FR-003**: System MUST create a consolidated test console that wraps/extends Spectre's `TestConsole` with cursor tracking capabilities; `VirtualAnsiConsole.cs` MUST be deleted after all tests pass with new infrastructure
- **FR-004**: System MUST configure snapshot storage in `/BitPantry.CommandLine.Tests/Snapshots/` folder
- **FR-005**: System MUST provide `AnsiCodes` helper class with constants for common ANSI sequences
- **FR-006**: System MUST refactor `StepwiseTestRunner` to use consolidated console infrastructure (allow minor API changes if behavior preserved)
- **FR-006a**: System MUST add Spectre-style "run to completion" test helpers as complement to StepwiseTestRunner for simpler scenarios

**Renderable Components:**

- **FR-007**: System MUST implement `AutoCompleteMenuRenderable` extending Spectre's `Renderable` base class
- **FR-008**: System MUST implement `GhostTextRenderable` extending Spectre's `Renderable` base class
- **FR-009**: Renderables MUST return proper `Segment` collections with appropriate styles
- **FR-010**: Menu renderable MUST handle viewport scrolling (show subset of items with scroll indicators)
- **FR-011**: Menu renderable MUST highlight selected item with invert style AND prefix indicator (`>` for selected, space for unselected) for accessibility
- **FR-011a**: Menu renderable MUST use vertical layout (one item per line) matching Spectre's SelectionPrompt pattern

**LiveRenderable Pattern:**

- **FR-012**: System MUST implement `MenuLiveRenderer` that tracks maximum rendered height
- **FR-013**: LiveRenderer MUST pad content to max height to prevent leftover lines
- **FR-014**: LiveRenderer MUST use Spectre's cursor positioning approach (`CR` + `CUU`)
- **FR-015**: LiveRenderer MUST clear previous content before re-rendering when size increases

**Controller Refactoring:**

- **FR-016**: `AutoCompleteController` MUST use `MenuLiveRenderer` instead of manual ANSI cursor control
- **FR-017**: `GhostTextRenderer` MUST use `GhostTextRenderable` instead of manual styling
- **FR-018**: Refactored controllers MUST pass all existing visual tests

### Key Entities

- **AutoCompleteMenuRenderable**: Renders menu items as Spectre Segments in vertical layout (one item per line); contains menu state (items, selected index, viewport offset); uses invert style for selection
- **GhostTextRenderable**: Renders ghost suggestion text in dim gray style; contains ghost state (suggestion text, visible portion after current input)
- **MenuLiveRenderer**: Wrapper that manages in-place updates using Spectre's `LiveRenderable` pattern; tracks max height via `Inflate()`, handles cursor positioning with `PositionCursor()` and `RestoreCursor()`
- **ConsolidatedTestConsole**: Test infrastructure class wrapping Spectre's TestConsole with cursor tracking capabilities extracted from VirtualAnsiConsole; supports both step-by-step and run-to-completion testing patterns

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All existing ~130 visual tests pass after refactoring with no behavior changes
- **SC-002**: Snapshot tests capture at least 5 distinct visual states (menu open, menu navigated, menu scrolled, ghost visible, ghost accepted)
- **SC-003**: Manual testing of Tab/arrow navigation shows no phantom lines or visual glitches across 50 consecutive navigation actions
- **SC-004**: Isolated renderable tests execute in under 50ms total (compared to full integration tests)
- **SC-005**: Code reduction of at least 30% in `AutoCompleteController.RenderMenu()`, `ClearMenu()`, and `UpdateMenuInPlace()` methods combined
- **SC-006**: Zero `[ESC` raw escape sequence strings in controller code (all cursor control delegated to LiveRenderer)

---

## Assumptions

- Spectre.Console.Testing 0.54.0 is compatible with the current Spectre.Console version used in the project
- The `LiveRenderable` pattern can be replicated or accessed without modifying Spectre.Console internals
- Existing test infrastructure (FluentAssertions, Moq) remains compatible with new packages
- The StepwiseTestRunner pattern is worth preserving and adapting rather than replacing entirely
- Switching from horizontal to vertical menu layout is acceptable UX change (one item per line vs inline)
- Existing ~130 visual tests can be updated to work with vertical layout without major rewrite
- Raw ANSI sequences in `.verified.txt` files are acceptable for diff review (tools like VS Code render ANSI)
- Cursor tracking extensions for Spectre's TestConsole can be implemented without patching Spectre internals
- The growth detection workaround (fallback to clear+re-render) will be eliminated by the LiveRenderable pattern
