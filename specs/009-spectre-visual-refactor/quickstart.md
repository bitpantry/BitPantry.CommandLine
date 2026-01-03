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

### Using ConsolidatedTestConsole

```csharp
using BitPantry.CommandLine.Tests.VirtualConsole;

[TestMethod]
public void Menu_SelectionChange_UpdatesCorrectly()
{
    // Arrange
    var console = new ConsolidatedTestConsole()
        .Width(80)
        .Height(24)
        .EmitAnsiSequences();

    var menu = new AutoCompleteMenuRenderable(
        items: new[] { "alpha", "beta", "gamma" },
        selectedIndex: 1,
        viewportStart: 0,
        viewportSize: 10);

    // Act
    console.Write(menu);

    // Assert
    console.Output.Should().Contain("[7mbeta[0m");  // Inverted style
    console.CursorPosition.Should().Be((0, 3));     // After 3 lines
}
```

### Using StepwiseTestRunner

```csharp
[TestMethod]
public void Tab_OpensMenu_WithFirstItemSelected()
{
    // Arrange
    var runner = new StepwiseTestRunner(registry);

    // Act
    runner.Type("con");
    runner.PressKey(ConsoleKey.Tab);

    // Assert
    runner.Should()
        .HaveMenuVisible()
        .WithSelectedIndex(0)
        .WithItems("continue", "config", "connect");
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

---

## File Locations

| Component | Location |
|-----------|----------|
| Renderables | `BitPantry.CommandLine/AutoComplete/Rendering/` |
| Live renderer | `BitPantry.CommandLine/AutoComplete/Rendering/MenuLiveRenderer.cs` |
| ANSI helpers | `BitPantry.CommandLine/AutoComplete/Rendering/AnsiCodes.cs` |
| Test console | `BitPantry.CommandLine.Tests/VirtualConsole/ConsolidatedTestConsole.cs` |
| Cursor tracker | `BitPantry.CommandLine.Tests/VirtualConsole/CursorTracker.cs` |
| Snapshots | `BitPantry.CommandLine.Tests/Snapshots/` |
| Verifier config | `BitPantry.CommandLine.Tests/Snapshots/ModuleInitializer.cs` |

---

## Next Steps

After completing this refactor:

1. ✅ All ~130 visual tests pass with new infrastructure
2. ✅ Snapshot baselines established for key visual states
3. ✅ VirtualAnsiConsole deleted (git history preserved)
4. ✅ No raw ANSI escape sequences in controller code

Future enhancements:
- Add keyboard navigation animations
- Support custom menu item styling
- Add menu search/filter capability
