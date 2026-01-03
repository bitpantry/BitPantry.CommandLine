# Contracts: Spectre Visual Rendering Refactor

**Branch**: `009-spectre-visual-refactor` | **Date**: January 3, 2026

## Overview

This document defines the interfaces and protocols for the Spectre Visual Rendering Refactor feature. Since this is a library refactoring (not an API service), contracts are defined as C# interfaces rather than OpenAPI specs.

---

## Interface Definitions

### 1. IMenuRenderer

**Purpose**: Abstraction for menu rendering with in-place update support.

```csharp
namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Renders autocomplete menu with in-place updates using LiveRenderable pattern.
/// </summary>
public interface IMenuRenderer
{
    /// <summary>
    /// Gets whether the menu is currently visible.
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Shows the menu with the specified items and selection.
    /// </summary>
    /// <param name="items">Menu items to display.</param>
    /// <param name="selectedIndex">Index of selected item.</param>
    /// <param name="viewportStart">First visible item index for scrolling.</param>
    /// <param name="viewportSize">Maximum items to display.</param>
    void Show(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);
    
    /// <summary>
    /// Updates the menu in-place without flicker.
    /// Uses Inflate pattern to track max height and prevent phantom lines.
    /// </summary>
    void Update(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize);
    
    /// <summary>
    /// Hides and clears the menu, restoring cursor to input line.
    /// </summary>
    void Hide();
}
```

**Implementation**: `MenuLiveRenderer`

---

### 2. IGhostRenderer

**Purpose**: Abstraction for ghost text rendering.

```csharp
namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Renders ghost text suggestions.
/// </summary>
public interface IGhostRenderer
{
    /// <summary>
    /// Gets whether ghost text is currently visible.
    /// </summary>
    bool IsVisible { get; }
    
    /// <summary>
    /// Shows ghost text at current cursor position.
    /// </summary>
    /// <param name="text">Ghost text to display (portion after user input).</param>
    void Show(string text);
    
    /// <summary>
    /// Clears any visible ghost text.
    /// </summary>
    void Clear();
}
```

**Implementation**: Uses `GhostTextRenderable` internally

---

### 3. ICursorTracker

**Purpose**: Tracks cursor position through ANSI sequences for test assertions.

```csharp
namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Tracks virtual cursor position by parsing ANSI escape sequences.
/// </summary>
public interface ICursorTracker
{
    /// <summary>
    /// Current cursor column (0-based).
    /// </summary>
    int Column { get; }
    
    /// <summary>
    /// Current cursor line (0-based).
    /// </summary>
    int Line { get; }
    
    /// <summary>
    /// Current position as tuple.
    /// </summary>
    (int Column, int Line) Position { get; }
    
    /// <summary>
    /// Processes written content and updates cursor position.
    /// Parses ANSI sequences: CUU, CUD, CUF, CUB, CR, LF, etc.
    /// </summary>
    /// <param name="content">Content written to console.</param>
    void ProcessWrite(string content);
    
    /// <summary>
    /// Resets cursor to origin (0, 0).
    /// </summary>
    void Reset();
}
```

**Implementation**: `CursorTracker` (extracted from VirtualAnsiConsole)

---

## Renderable Protocols

### AutoCompleteMenuRenderable Protocol

**Input**: Menu state (items, selection, viewport)

**Output**: Spectre Segments for vertical menu display

```
Protocol: Measure() → Render()
────────────────────────────────────────────────────────────────
Input State:
  Items: ["continue", "config", "connect"]
  SelectedIndex: 1
  ViewportStart: 0
  ViewportSize: 10

Measure():
  Returns: (Width: max item length, Height: visible items + scroll indicators)

Render():
  Returns Segments:
    [
      Segment("> config", Style.Invert),    // Selected
      Segment("\n"),
      Segment("  continue", Style.Default),
      Segment("\n"),
      Segment("  connect", Style.Default),
      Segment("\n")
    ]
```

### GhostTextRenderable Protocol

**Input**: Ghost text string

**Output**: Single styled segment

```
Protocol: Measure() → Render()
────────────────────────────────────────────────────────────────
Input State:
  GhostText: "nect"
  GhostStyle: Color.Grey + Decoration.Dim

Measure():
  Returns: (Width: text.Length, Height: 1)

Render():
  Returns Segments:
    [
      Segment("nect", new Style(Color.Grey, decoration: Decoration.Dim))
    ]
```

---

## Test Console Protocol

### ConsolidatedTestConsole Usage

```csharp
// Configuration (fluent builder pattern matching Spectre)
var console = new ConsolidatedTestConsole()
    .Width(80)
    .Height(24)
    .Interactive()
    .EmitAnsiSequences();

// Input simulation
console.Input.PushKey(ConsoleKey.Tab);
console.Input.PushText("con");
console.Input.PushKey(ConsoleKey.DownArrow);

// Output verification
string output = console.Output;                    // Full output string
IReadOnlyList<string> lines = console.Lines;      // Split by newlines
(int col, int line) = console.CursorPosition;     // Tracked position

// Cleanup
console.Dispose();
```

### StepwiseTestRunner Integration

```csharp
// Existing pattern preserved with new console
var runner = new StepwiseTestRunner(registry, new ConsolidatedTestConsole());

runner.Type("con");
runner.Should().HaveGhostText("nect");

runner.PressKey(ConsoleKey.Tab);
runner.Should().HaveMenuVisible()
               .WithSelectedIndex(0);

runner.PressKey(ConsoleKey.DownArrow);
runner.Should().HaveSelectedIndex(1)
               .WithCursorAtLine(expectedLine);
```

---

## Snapshot Test Protocol

### Verify.MSTest Integration

```csharp
// Test method with snapshot verification
[TestMethod]
public async Task MenuOpen_WithThreeItems_RendersCorrectly()
{
    // Arrange
    var console = new ConsolidatedTestConsole()
        .Width(80)
        .EmitAnsiSequences();
    
    var menu = new AutoCompleteMenuRenderable(
        items: new[] { "continue", "config", "connect" },
        selectedIndex: 0,
        viewportStart: 0,
        viewportSize: 10);
    
    // Act
    console.Write(menu);
    
    // Assert - compares to MenuOpen_WithThreeItems_RendersCorrectly.verified.txt
    await Verifier.Verify(console.Output);
}
```

### Verified File Format

```
# MenuOpen_WithThreeItems_RendersCorrectly.verified.txt
# Contains raw ANSI sequences for full fidelity

[?25l                          # Hide cursor
[7m> continue[0m               # Selected item (inverted)
  config
  connect
[?25h                          # Show cursor
```

---

## Error Handling Protocol

| Scenario | Behavior |
|----------|----------|
| Null items passed to renderable | ArgumentNullException |
| Invalid viewport size (< 1) | ArgumentOutOfRangeException |
| Selected index out of range | Clamp to valid range (defensive) |
| Console disposed during render | ObjectDisposedException |
| ANSI sequence parsing error | Log warning, skip sequence (graceful degradation) |

---

## Thread Safety

| Component | Thread Safety |
|-----------|---------------|
| AutoCompleteMenuRenderable | Immutable, thread-safe |
| GhostTextRenderable | Immutable, thread-safe |
| MenuLiveRenderer | Not thread-safe (single UI thread) |
| ConsolidatedTestConsole | Not thread-safe (test context) |
| AnsiCodes | Static, immutable, thread-safe |
