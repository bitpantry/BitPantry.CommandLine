# Quickstart: Spectre Visual Rendering Refactor

**Branch**: `009-spectre-visual-refactor` | **Date**: January 3, 2026

## Overview

This guide helps developers understand and work with the new Spectre-based visual rendering infrastructure for autocomplete menus and ghost text.

---

## Prerequisites

- .NET 8.0 SDK
- Visual Studio 2022 / VS Code with C# extension
- Clone the repository and checkout the feature branch:
  ```bash
  git checkout 009-spectre-visual-refactor
  ```

---

## Quick Start

### 1. Build the Solution

```bash
dotnet build BitPantry.CommandLine.sln
```

### 2. Run Tests

```bash
# Run all tests
dotnet test

# Run visual tests only
dotnet test --filter "FullyQualifiedName~Visual"

# Run with verbose output
dotnet test --logger "console;verbosity=detailed"
```

### 3. Verify Snapshot Tests

```bash
# Run snapshot tests
dotnet test --filter "FullyQualifiedName~Snapshot"

# If snapshots differ, review .received.txt vs .verified.txt files
# Accept new snapshots by renaming .received.txt to .verified.txt
```

---

## Key Concepts

### Spectre Renderable Pattern

Instead of manually constructing ANSI escape sequences, we use Spectre.Console's `Renderable` base class:

```csharp
// OLD: Manual ANSI (fragile)
console.Write("\u001b[7m> continue\u001b[0m\n  config\n");

// NEW: Spectre Renderable (robust)
var menu = new AutoCompleteMenuRenderable(items, selectedIndex, viewport);
console.Write(menu);
```

**Benefits**:
- Spectre handles style application and segment rendering
- Testable: render to TestConsole, verify segments
- Composable: combine renderables for complex layouts

### LiveRenderable Pattern (Copied from Spectre)

The `MenuLiveRenderable` is a near-verbatim copy of Spectre's internal `LiveRenderable` class. See [reference-code.md](reference-code.md) for the complete source.

```csharp
var live = new MenuLiveRenderable(console);

// Set content and render
live.SetRenderable(menuRenderable);
console.Write(live);

// Update in place - uses Inflate pattern
live.SetRenderable(newMenuRenderable);
console.Write(live.PositionCursor(options));  // Move cursor up
console.Write(live);                           // Re-render (padded to max height)

// Clean up
console.Write(live.RestoreCursor());  // Clear all lines
```

**Key Pattern**: `SegmentShape.Inflate()` ensures dimensions only grow. When content shrinks, blank padding lines fill the gap, preventing phantom lines.

### Vertical Menu Layout

The menu now uses vertical layout (one item per line) instead of horizontal:

```
# Horizontal (old)
con| nect  [continue]  config  (+2 more)

# Vertical (new)
con|
> continue    ← selected (inverted style)
  config
  connect
```

---

## Working with Renderables

### Creating a Menu Renderable

```csharp
using BitPantry.CommandLine.AutoComplete.Rendering;

var menu = new AutoCompleteMenuRenderable(
    items: new[] { "continue", "config", "connect" },
    selectedIndex: 1,           // config is selected
    viewportStart: 0,           // start from first item
    viewportSize: 10);          // show up to 10 items

// Render to console
console.Write(menu);
```

### Creating a Ghost Text Renderable

```csharp
var ghost = new GhostTextRenderable("nect");  // Completes "con" → "connect"

// Render to console (dim gray style)
console.Write(ghost);
```

### Using the Live Renderer

```csharp
var liveRenderer = new MenuLiveRenderer(console);

// Show menu
liveRenderer.Show(items, selectedIndex: 0, viewportStart: 0, viewportSize: 5);

// User presses Down arrow - update selection
liveRenderer.Update(items, selectedIndex: 1, viewportStart: 0, viewportSize: 5);

// User presses Escape - hide menu
liveRenderer.Hide();
```

---

## Writing Tests

### ⚠️ Critical: State Tests vs Visual Output Tests

There are **two distinct types** of testing for autocomplete features:

| Type | What It Tests | When to Use | ANSI Required |
|------|---------------|-------------|---------------|
| **State Tests** | Controller state (IsMenuVisible, SelectedIndex, Buffer) | Behavior/logic verification | No |
| **Visual Output Tests** | Actual rendered ANSI sequences | Styling, highlighting, colors | **Yes** |

**Common Mistake**: Writing a "visual test" that only checks state. If your feature involves **styling** (colors, highlighting, selection indicators), you MUST verify the ANSI output.

### Choosing the Right Test Pattern

```
Is your feature about...

├─ Logic/behavior (what items appear, when menu opens)?
│   └─ Use StepwiseTestRunner with state assertions
│      runner.Should().HaveMenuVisible().WithSelectedIndex(0);
│
├─ Visual styling (colors, highlighting, formatting)?
│   └─ Use ConsolidatedTestConsole with ANSI assertions
│      console.Output.Should().Contain("\u001b[34m");  // blue
│
├─ Regression prevention for specific visual state?
│   └─ Use Snapshot testing
│      await Verifier.Verify(console.Output);
│
└─ Component rendering in isolation?
    └─ Use direct renderable instantiation
       new AutoCompleteMenuRenderable(...).Render(console);
```

### Using ConsolidatedTestConsole

```csharp
using BitPantry.CommandLine.Tests.VirtualConsole;

[TestMethod]
public void Menu_SelectionChange_UpdatesCorrectly()
{
    // Arrange - ALWAYS call EmitAnsiSequences() for visual tests
    var console = new ConsolidatedTestConsole()
        .Width(80)
        .Height(24)
        .EmitAnsiSequences();  // ⚠️ Required for ANSI output capture

    var menu = new AutoCompleteMenuRenderable(
        items: new[] { "alpha", "beta", "gamma" },
        selectedIndex: 1,
        viewportStart: 0,
        viewportSize: 10);

    // Act
    console.Write(menu);

    // Assert on ANSI output
    console.Output.Should().Contain("[7mbeta[0m");  // Inverted style
    console.CursorPosition.Should().Be((0, 3));     // After 3 lines
}
```

### Using StepwiseTestRunner

**For state/behavior testing** (menu opens, selection changes, buffer updates):

```csharp
[TestMethod]
public void Tab_OpensMenu_WithFirstItemSelected()
{
    // Arrange - CreateRunner() now enables EmitAnsiSequences by default
    var runner = CreateRunner();

    // Act
    runner.Type("con");
    runner.PressKey(ConsoleKey.Tab);

    // Assert - state assertions (no ANSI verification)
    runner.Should()
        .HaveMenuVisible()
        .WithSelectedIndex(0)
        .WithItems("continue", "config", "connect");
}
```

**For visual output testing** (highlighting, colors):

```csharp
[TestMethod]
public void FilterHighlighting_PersistsOnNavigation()
{
    // Arrange
    var runner = CreateRunner();

    // Act - type filter and navigate
    await runner.TypeText("conn");
    await runner.PressKey(ConsoleKey.Tab);
    await runner.PressKey(ConsoleKey.Down);  // Change selection

    // Assert - verify ANSI codes in output
    runner.Console.Output.Should().Contain("\u001b[34m");  // Blue highlight
    // OR use the helper assertion:
    runner.Should().HaveHighlightedText("conn");
}
```

### Snapshot Testing

```csharp
[TestMethod]
public async Task MenuWithScroll_RendersCorrectly()
{
    // Arrange
    var console = new ConsolidatedTestConsole()
        .Width(40)
        .EmitAnsiSequences();

    var menu = new AutoCompleteMenuRenderable(
        items: Enumerable.Range(1, 20).Select(i => $"item{i}").ToList(),
        selectedIndex: 5,
        viewportStart: 3,
        viewportSize: 5);

    // Act
    console.Write(menu);

    // Assert - compares to .verified.txt file with raw ANSI
    await Verifier.Verify(console.Output);
}
```

---

## Common Tasks

### Updating an Existing Visual Test

1. Find the test in `BitPantry.CommandLine.Tests/AutoComplete/Visual/`
2. Update to use `ConsolidatedTestConsole` instead of `VirtualAnsiConsole`:

```csharp
// Before
var console = new VirtualAnsiConsole();

// After
var console = new ConsolidatedTestConsole()
    .Width(80)
    .Height(24);
```

3. Run the test to verify it passes with the new console

### Adding a New Snapshot Test

1. Create test method in appropriate test class
2. Arrange console and renderable state
3. Render to console
4. Call `await Verifier.Verify(console.Output)`
5. Run test - it will fail with `.received.txt` file
6. Review the received output
7. Rename to `.verified.txt` to accept as baseline
8. Commit the `.verified.txt` file

### Debugging Cursor Position Issues

```csharp
// Use ConsolidatedTestConsole's cursor tracking
var console = new ConsolidatedTestConsole().EmitAnsiSequences();

console.Write(menu);

// Check cursor position after render
var (col, line) = console.CursorPosition;
Console.WriteLine($"Cursor at: ({col}, {line})");

// Check raw output for ANSI sequences
Console.WriteLine($"Raw output: {console.Output.Replace("\u001b", "\\e")}");
```

---

## ANSI Sequence Reference

Use the `AnsiCodes` helper for readable code:

```csharp
using static BitPantry.CommandLine.AutoComplete.Rendering.AnsiCodes;

// Instead of magic strings
console.Write("\u001b[?25l");  // ❌ Hard to read

// Use constants
console.Write(HideCursor);     // ✅ Self-documenting
console.Write(CursorUp(3));    // ✅ Parameterized

// Common sequences
HideCursor          // \u001b[?25l
ShowCursor          // \u001b[?25h
ClearLine           // \u001b[2K
CursorUp(n)         // \u001b[nA
CursorDown(n)       // \u001b[nB
CarriageReturn      // \r
```

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Snapshot test fails unexpectedly | Check for whitespace differences; use diff tool on `.received.txt` vs `.verified.txt` |
| Cursor position wrong in tests | Verify `EmitAnsiSequences()` is called on console |
| Menu leaves phantom lines | Ensure `MenuLiveRenderer.Hide()` is called; check `_maxHeight` tracking |
| Style not applied in tests | Use `Colors(ColorSystem.TrueColor)` on test console |
| Tests pass locally, fail in CI | Check terminal width assumptions; use explicit `.Width()` configuration |
| **Tests pass but feature visually broken** | You're testing state, not output. Add ANSI output assertions. |
| Highlighting disappears on navigation | Verify `UpdateMenuInPlace()` passes `CompletionItem` not strings |

---

## Testing Checklist for New Features

Before marking a feature complete, verify:

### For Behavior Features (logic, state changes)
- [ ] State tests verify IsMenuVisible, SelectedIndex, Buffer as expected
- [ ] Edge cases covered (empty input, no matches, single match)
- [ ] Keyboard navigation works (Tab, Enter, Escape, arrows)

### For Visual Features (styling, colors, formatting)
- [ ] **ANSI output assertions** verify expected escape sequences
- [ ] Highlighting/colors verified with `console.Output.Should().Contain(...)`
- [ ] Snapshot test captures the visual state
- [ ] Manual testing confirms visual appearance

### Common ANSI Codes to Assert

| Visual Effect | ANSI Code | Example Assertion |
|--------------|-----------|-------------------|
| Blue foreground | `\u001b[34m` | `.Contain("\u001b[34m")` |
| Inverted (selection) | `\u001b[7m` | `.Contain("\u001b[7m")` |
| Dim/gray | `\u001b[90m` | `.Contain("\u001b[90m")` |
| Reset | `\u001b[0m` | `.Contain("\u001b[0m")` |

---

## File Locations

| Component | Location |
|-----------|----------|
| Renderables | `BitPantry.CommandLine/AutoComplete/Rendering/` |
| Live renderer | `BitPantry.CommandLine/AutoComplete/Rendering/MenuLiveRenderer.cs` |
| ANSI helpers | `BitPantry.CommandLine/AutoComplete/Rendering/AnsiCodes.cs` |
| Test console | `BitPantry.CommandLine.Tests/VirtualConsole/ConsolidatedTestConsole.cs` |
| Cursor tracker | `BitPantry.CommandLine.Tests/VirtualConsole/CursorTracker.cs` |
| Test base class | `BitPantry.CommandLine.Tests/AutoComplete/Visual/VisualTestBase.cs` |
| Snapshots | `BitPantry.CommandLine.Tests/Snapshots/` |
| Verifier config | `BitPantry.CommandLine.Tests/Snapshots/ModuleInitializer.cs` |

---

## Next Steps

After completing Phase 9 (Testing Consolidation):

1. ✅ All visual tests pass with ANSI emission enabled
2. ✅ ANSI assertion helpers available for visual verification
3. ✅ Dead code removed (SpectreTestHelper, GetMenuItemStrings)
4. ✅ Documentation consolidated (this quickstart is the source of truth)
5. ✅ Menu filter highlighting bug fixed via TDD
