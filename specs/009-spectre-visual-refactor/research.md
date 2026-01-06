# Research: Spectre Visual Rendering Refactor

**Branch**: `009-spectre-visual-refactor` | **Date**: January 3, 2026 | **Updated**: January 4, 2026

## Overview

This document consolidates research findings for the Spectre Visual Rendering Refactor feature. All technical unknowns from the specification have been resolved.

---

## Research Findings

### 1. Spectre.Console.Testing Compatibility

**Decision**: Use Spectre.Console.Testing v0.54.0 with existing Spectre.Console v0.49.1

**Rationale**: 
- Spectre.Console.Testing is a separate package that provides `TestConsole` and `TestConsoleInput` for testing scenarios
- The testing package is designed to be compatible with the main Spectre.Console package
- Both packages are from the same library author (Patrik Svensson) and follow semantic versioning
- Version 0.54.0 is the latest stable release with comprehensive testing utilities

**Alternatives Considered**:
- Keep VirtualAnsiConsole only → Rejected: Missing IRenderable/IRenderHook support, no snapshot testing integration
- Build custom test infrastructure from scratch → Rejected: Duplicates proven Spectre patterns, higher maintenance burden

---

### 1a. EmitAnsiSequences Gap Discovery (2026-01-04)

**Finding**: The testing infrastructure had a critical gap - "visual tests" weren't actually testing visual output.

**Root Cause Analysis**:
- `VisualTestBase.CreateRunner()` creates `ConsolidatedTestConsole` but does NOT call `.EmitAnsiSequences()`
- Without this call, ANSI escape codes are stripped from `console.Output`
- Tests only verify controller state (IsMenuVisible, SelectedIndex) not rendered output
- Visual bugs (like missing highlighting) pass tests because styling isn't verified

**Evidence**: Menu filter highlighting bug in spec-010 was caught manually but all tests passed. The bug was in `AutoCompleteController.UpdateMenuInPlace()` which passed strings instead of `CompletionItem` objects, losing `MatchRanges` data.

**Resolution**: 
- Enable `.EmitAnsiSequences()` by default in `VisualTestBase.CreateRunner()`
- Add ANSI assertion helpers to `StepwiseTestRunnerAssertions`
- Document the distinction between state tests and visual output tests

---

### 2. LiveRenderable Pattern Implementation

**Decision**: ~~Replicate Spectre's internal `LiveRenderable` pattern externally~~ **UPDATED**: Copy Spectre's `LiveRenderable` and `SegmentShape` source code directly (with namespace change)

**Rationale**:
- Spectre's `LiveRenderable` and `SegmentShape` are `internal` but **fully visible** in the open-source repo
- Total implementation is only ~220 lines - manageable to copy verbatim
- Copying rather than "replicating from documentation" eliminates interpretation errors
- We maintain exact behavioral parity with Spectre's proven implementation

**Implementation Approach**:
```csharp
// Copy from: https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Live/LiveRenderable.cs
// Copy from: https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Rendering/SegmentShape.cs

namespace BitPantry.CommandLine.AutoComplete;

// Near-verbatim copy of Spectre's internal LiveRenderable
internal sealed class MenuLiveRenderable : Renderable { ... }

// Near-verbatim copy of Spectre's internal SegmentShape  
internal readonly struct SegmentShape { ... }
```

**Key Methods to Copy Exactly**:
- `PositionCursor()`: `"\r" + CUU(linesToMoveUp)`
- `RestoreCursor()`: `"\r" + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear)`
- `SegmentShape.Inflate()`: `new SegmentShape(Math.Max(Width, other.Width), Math.Max(Height, other.Height))`
- `SegmentShape.Apply()`: Pad lines to Width, add blank lines to Height

**Alternatives Considered**:
- Fork Spectre.Console to expose LiveRenderable → Rejected: Maintenance burden, version drift risk
- Use Spectre's `Live` display directly → Rejected: Blocking API incompatible with keystroke-by-keystroke input model

---

### 3. Vertical Menu Layout

**Decision**: Switch from horizontal to vertical menu layout (one item per line)

**Rationale**:
- Aligns with Spectre's `SelectionPrompt` visual pattern
- Simplifies LiveRenderable height tracking (each item is one line)
- Eliminates horizontal scroll indicator complexity (`+N before`, `+N more`)
- Easier to test: each item on its own line means simpler assertions
- More accessible: screen readers handle vertical lists better than inline lists

**Visual Change**:
```
# Before (horizontal)
con| nect  [continue]  config  (+2 more)

# After (vertical)
con|
> continue    ← selected (invert style)
  config
  connect
  (+2 more below)
```

**Alternatives Considered**:
- Keep horizontal layout → Rejected: Root cause of cursor math complexity, harder to test
- Make layout configurable → Rejected: Added complexity without clear user benefit

---

### 4. Snapshot Testing Strategy

**Decision**: Use Verify.MSTest with raw ANSI sequences in `.verified.txt` files

**Rationale**:
- Raw ANSI sequences provide full fidelity: catches style bugs, cursor positioning errors
- VS Code and modern terminals render ANSI in diffs, making review practical
- Verify.MSTest integrates cleanly with existing MSTest framework
- Snapshot files stored in `/BitPantry.CommandLine.Tests/Snapshots/` for organization

**Configuration**:
```csharp
// ModuleInitializer.cs
[ModuleInitializer]
public static void Initialize()
{
    Verifier.DerivePathInfo((sourceFile, projectDirectory, type, method) =>
        new PathInfo(
            directory: Path.Combine(projectDirectory, "Snapshots"),
            typeName: type.Name,
            methodName: method.Name));
}
```

**Alternatives Considered**:
- Strip ANSI for plain text snapshots → Rejected: Loses style and cursor positioning information
- Use separate snapshot tool (Snapshooter) → Rejected: Verify.MSTest has better MSTest integration

---

### 5. Consolidated Test Console Design

**Decision**: Create `ConsolidatedTestConsole` that wraps Spectre's `TestConsole` and adds cursor tracking

**Rationale**:
- Spectre's `TestConsole` has excellent IAnsiConsole implementation and segment rendering
- BitPantry's `VirtualAnsiConsole` has better cursor position tracking and line buffer management
- Combining both gives: IRenderable support + cursor tracking + line buffer + ANSI parsing

**Design Approach**:
```csharp
public class ConsolidatedTestConsole : IAnsiConsole
{
    private readonly TestConsole _spectreConsole;
    private readonly CursorTracker _cursorTracker;  // Extracted from VirtualAnsiConsole
    
    // Delegate IAnsiConsole to Spectre
    // Add cursor tracking on Write operations
    // Expose cursor position for StepwiseTestRunner assertions
}
```

**Alternatives Considered**:
- Extend Spectre's TestConsole directly → Rejected: TestConsole is sealed
- Keep VirtualAnsiConsole and add Spectre alongside → Rejected: Dual infrastructure, inconsistent behaviors

---

### 6. StepwiseTestRunner Preservation

**Decision**: Keep StepwiseTestRunner, refactor to use ConsolidatedTestConsole

**Rationale**:
- StepwiseTestRunner provides unique value: step-by-step keystroke testing with state inspection
- Spectre's testing pattern (queue inputs → run → verify output) doesn't allow intermediate state checks
- Both patterns are complementary: StepwiseTestRunner for debugging, Spectre-style for happy paths

**API Compatibility**:
- Minor signature changes allowed (e.g., console type parameter)
- Core pattern preserved: `PressKey()` → `Should().HaveMenuVisible()`
- Add `SpectreTestHelper` for "queue and run" pattern as complement

**Alternatives Considered**:
- Replace StepwiseTestRunner entirely → Rejected: Loses step-by-step debugging capability
- Keep completely unchanged → Rejected: Must update console dependency

---

## Resolved Clarifications

All NEEDS CLARIFICATION items from the specification have been resolved:

| Item | Resolution |
|------|------------|
| Spectre.Console.Testing version | v0.54.0 (compatible with 0.49.1) |
| LiveRenderable accessibility | Replicate pattern externally |
| Menu layout change impact | Vertical layout approved in spec clarifications |
| Snapshot format | Raw ANSI sequences |
| VirtualAnsiConsole fate | Delete after consolidation complete |
| StepwiseTestRunner preservation | Keep and refactor |

---

## Technology Decisions Summary

| Component | Choice | Version |
|-----------|--------|---------|
| Testing framework | MSTest + FluentAssertions | 3.6.1 / 6.12.0 |
| Console testing | Spectre.Console.Testing | 0.54.0 |
| Snapshot testing | Verify.MSTest | 26.0.0 |
| Menu layout | Vertical (one item per line) | — |
| LiveRenderable | External replication | — |
| Cursor tracking | ConsolidatedTestConsole wrapper | — |

---

## Spectre Source Code Validation (Deep Research)

Research conducted against `spectreconsole/spectre.console` GitHub repository to validate our approach aligns with Spectre's internal patterns and best practices.

### Key Findings

#### 1. LiveRenderable Pattern - CONFIRMED

Our proposed pattern matches Spectre's actual implementation exactly:

```csharp
// From Spectre.Console/Live/LiveRenderable.cs (internal sealed)
internal sealed class LiveRenderable : Renderable
{
    private SegmentShape? _shape;
    
    public IRenderable PositionCursor(RenderOptions options)
    {
        // Uses: "\r" + CUU(linesToMoveUp) to position cursor at start
        return new ControlCode("\r" + CUU(_shape.Value.Height - 1));
    }
    
    public IRenderable RestoreCursor()
    {
        // Uses: "\r" + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear)
        var linesToClear = _shape.Value.Height - 1;
        return new ControlCode("\r" + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear));
    }
    
    protected override IEnumerable<Segment> Render(...)
    {
        // Key: _shape = _shape.Value.Inflate(shape) - tracks max dimensions
        // Key: _shape.Value.Apply() - pads content to max dimensions
    }
}
```

**Validation**: Our `MenuLiveRenderer` design in data-model.md correctly captures this pattern. The `Inflate()` approach (always take max of current and previous dimensions) is exactly how Spectre prevents phantom lines.

#### 2. SegmentShape - CONFIRMED

```csharp
// From Spectre.Console/Rendering/SegmentShape.cs
internal readonly struct SegmentShape
{
    public int Width { get; }
    public int Height { get; }
    
    public SegmentShape Inflate(SegmentShape other)
    {
        return new SegmentShape(
            Math.Max(Width, other.Width),
            Math.Max(Height, other.Height));
    }
    
    public void Apply(RenderOptions options, ref List<SegmentLine> lines)
    {
        // Pads lines to Width with Segment.Padding
        // Adds blank lines to reach Height
    }
}
```

**Validation**: We should replicate this `SegmentShape` structure. It's the core abstraction for the "inflate and pad" pattern.

#### 3. IRenderHook Integration - CRITICAL NEW FINDING

Spectre uses `IRenderHook` to integrate LiveRenderable into the render pipeline:

```csharp
// From Spectre.Console/Prompts/List/ListPromptRenderHook.cs
internal sealed class ListPromptRenderHook<T> : IRenderHook
{
    private readonly LiveRenderable _live;
    
    public IEnumerable<IRenderable> Process(RenderOptions options, IEnumerable<IRenderable> renderables)
    {
        lock (_lock)
        {
            if (!_live.HasRenderable || _dirty)
            {
                _live.SetRenderable(_builder());
                _dirty = false;
            }

            yield return _live.PositionCursor(options);  // Move cursor up

            foreach (var renderable in renderables)
            {
                yield return renderable;  // Any other content
            }

            yield return _live;  // Render the live content
        }
    }
}
```

**Action**: Consider adding `IRenderHook` pattern to our design. This enables composition with other renderables and is how Spectre's SelectionPrompt/MultiSelectionPrompt work.

#### 4. RenderHookScope - CONFIRMED

```csharp
// From Spectre.Console/Rendering/RenderHookScope.cs
public sealed class RenderHookScope : IDisposable
{
    public RenderHookScope(IAnsiConsole console, IRenderHook hook)
    {
        _console.Pipeline.Attach(_hook);
    }
    
    public void Dispose() => _console.Pipeline.Detach(_hook);
}
```

**Validation**: Using `RenderHookScope` pattern ensures proper cleanup. Our `MenuLiveRenderer` should support this pattern.

#### 5. SelectionPrompt Architecture - CONFIRMED

Spectre's selection prompts follow this architecture:
- `ListPrompt<T>` - Core input loop with `RenderHookScope`
- `ListPromptRenderHook<T>` - IRenderHook that wraps LiveRenderable
- `IListPromptStrategy<T>` - Interface for rendering and input handling
- `ListPromptState<T>` - Tracks current index, items, page info

**Validation**: Our vertical menu matches this pattern conceptually. We don't need to use `IListPromptStrategy` (too coupled to multi-selection), but the state management approach is sound.

#### 6. TestConsole Patterns - CONFIRMED

```csharp
// From Spectre.Console.Testing/TestConsole.cs
public sealed class TestConsole : IAnsiConsole
{
    public bool EmitAnsiSequences { get; set; }  // When true, output includes raw ANSI
    
    public void Write(IRenderable renderable)
    {
        if (EmitAnsiSequences)
        {
            _cursor = null;  // Uses NoopCursor when emitting ANSI
            // Outputs raw ANSI sequences
        }
        else
        {
            // Strips ANSI, outputs plain text
        }
    }
}
```

```csharp
// From Spectre.Console.Testing/TestConsoleInput.cs
public sealed class TestConsoleInput : IAnsiConsoleInput
{
    public void PushText(string text);
    public void PushTextWithEnter(string text);
    public void PushCharacter(char character);
    public void PushKey(ConsoleKey key);
    public void PushKey(ConsoleKeyInfo consoleKeyInfo);
    
    public ConsoleKeyInfo? ReadKey(bool intercept);  // Returns null if empty queue
}
```

**Validation**: Our ConsolidatedTestConsole design is correct:
- Use `EmitAnsiSequences = true` for snapshot tests
- Use `TestConsoleInput.PushKey()` for simulating keystrokes
- Add cursor tracking wrapper (Spectre's cursor becomes NoopCursor with ANSI mode)

#### 7. Cursor Positioning ANSI Sequences - CONFIRMED

From `Spectre.Console/AnsiSequences.cs`:
- `CUU(n)` = Cursor Up n lines = `\x1b[{n}A`
- `EL(n)` = Erase in Line = `\x1b[{n}K` (n=2 means clear entire line)
- `CR` = Carriage Return = `\r`

**Validation**: These are the exact sequences our AnsiCodes entity should define.

### Best Practice Alignment Assessment

| Our Design | Spectre Pattern | Status |
|------------|-----------------|--------|
| LiveRenderable replication | Internal class, must replicate | ✅ ALIGNED |
| SegmentShape.Inflate | Exact pattern used | ✅ ALIGNED |
| Cursor positioning sequences | CR + CUU + EL | ✅ ALIGNED |
| TestConsole + EmitAnsiSequences | Core testing pattern | ✅ ALIGNED |
| TestConsoleInput queue | PushKey/PushText | ✅ ALIGNED |
| IRenderHook integration | Used for all live rendering | ⚠️ ENHANCEMENT POSSIBLE |

### Recommendations Based on Research

1. **Add IRenderHook support** (Optional Enhancement)
   - Our design works without it, but adding `IRenderHook` support would enable future composition
   - Consider adding `AutoCompleteRenderHook` that wraps our `MenuLiveRenderer`

2. **Replicate SegmentShape exactly**
   - Create `SegmentShape` readonly struct with `Inflate()` and `Apply()` methods
   - This is a small, well-defined abstraction worth copying

3. **Use ControlCode for cursor sequences**
   - Spectre uses `new ControlCode(string)` to emit raw ANSI without measuring/rendering
   - Our design should do the same rather than building strings manually

4. **Confirm TestConsole default size**
   - Spectre's TestConsole defaults to 80x24
   - Ensure our tests account for this when testing scroll behavior

### Items We Are NOT Reinventing

The following are correctly leveraged from Spectre (not duplicated):

- ✅ `IRenderable` / `Renderable` base class
- ✅ `Segment` / `SegmentLine` output representation
- ✅ `TestConsole` for test infrastructure
- ✅ `TestConsoleInput` for keystroke simulation
- ✅ `Style` / `Color` for formatting
- ✅ `Markup` / `Text` for text rendering

### Items We Must Copy (Spectre Internal - Full Source Available)

The following are `internal` to Spectre but we have **full source code visibility** via the open-source repository. These should be copied near-verbatim with namespace changes:

| Spectre Source | Our Copy | Lines | Source URL |
|----------------|----------|-------|------------|
| `LiveRenderable.cs` | `MenuLiveRenderable.cs` | ~100 | [GitHub](https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Live/LiveRenderable.cs) |
| `SegmentShape.cs` | `SegmentShape.cs` | ~58 | [GitHub](https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Rendering/SegmentShape.cs) |

**Additional Implementation** (not copied, but inspired by Spectre patterns):
- ⚙️ `ConsolidatedTestConsole` - Wraps `TestConsole` + adds cursor tracking
- ⚙️ `AutoCompleteMenuRenderable` - Our menu rendering using Spectre's `Renderable` base

---

## Next Steps

1. ✅ Phase 1: Generate data-model.md with entity definitions for renderables
2. ✅ Phase 1: Generate contracts/ with interface definitions
3. ✅ Phase 1: Generate quickstart.md for developer onboarding
4. Phase 2: Break into implementation tasks (via /speckit.tasks)
