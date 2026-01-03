# Reference Code: Spectre Internal Classes

**Purpose**: Complete source code from Spectre.Console that we need to copy (with namespace changes) for our implementation. These are `internal` classes we cannot import directly.

**Source**: https://github.com/spectreconsole/spectre.console

---

## 1. LiveRenderable.cs

**Source**: `src/Spectre.Console/Live/LiveRenderable.cs`

```csharp
using static Spectre.Console.AnsiSequences;

namespace Spectre.Console.Rendering;

internal sealed class LiveRenderable : Renderable
{
    private readonly object _lock = new object();
    private readonly IAnsiConsole _console;
    private IRenderable? _renderable;
    private SegmentShape? _shape;

    public IRenderable? Target => _renderable;
    public bool DidOverflow { get; private set; }

    [MemberNotNullWhen(true, nameof(Target))]
    public bool HasRenderable => _renderable != null;
    public VerticalOverflow Overflow { get; set; }
    public VerticalOverflowCropping OverflowCropping { get; set; }

    public LiveRenderable(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        Overflow = VerticalOverflow.Ellipsis;
        OverflowCropping = VerticalOverflowCropping.Top;
    }

    public LiveRenderable(IAnsiConsole console, IRenderable renderable)
        : this(console)
    {
        _renderable = renderable ?? throw new ArgumentNullException(nameof(renderable));
    }

    public void SetRenderable(IRenderable? renderable)
    {
        lock (_lock)
        {
            _renderable = renderable;
        }
    }

    public IRenderable PositionCursor(RenderOptions options)
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return new ControlCode(string.Empty);
            }

            // Check if the size have been reduced
            if (_shape.Value.Height > options.ConsoleSize.Height || 
                _shape.Value.Width > options.ConsoleSize.Width)
            {
                // Important reset shape, so the size can shrink
                _shape = null;
                return new ControlCode(ED(2) + ED(3) + CUP(1, 1));
            }

            var linesToMoveUp = _shape.Value.Height - 1;
            return new ControlCode("\r" + CUU(linesToMoveUp));
        }
    }

    public IRenderable RestoreCursor()
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return new ControlCode(string.Empty);
            }

            var linesToClear = _shape.Value.Height - 1;
            return new ControlCode("\r" + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear));
        }
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        lock (_lock)
        {
            DidOverflow = false;

            if (_renderable != null)
            {
                var segments = _renderable.Render(options, maxWidth);
                var lines = Segment.SplitLines(segments);

                var shape = SegmentShape.Calculate(options, lines);
                if (shape.Height > _console.Profile.Height)
                {
                    if (Overflow == VerticalOverflow.Crop)
                    {
                        if (OverflowCropping == VerticalOverflowCropping.Bottom)
                        {
                            // Remove bottom lines
                            var index = Math.Min(_console.Profile.Height, lines.Count);
                            var count = lines.Count - index;
                            lines.RemoveRange(index, count);
                        }
                        else
                        {
                            // Remove top lines
                            var start = lines.Count - _console.Profile.Height;
                            lines.RemoveRange(0, start);
                        }

                        shape = SegmentShape.Calculate(options, lines);
                    }
                    else if (Overflow == VerticalOverflow.Ellipsis)
                    {
                        var ellipsisText = _console.Profile.Capabilities.Unicode ? "…" : "...";
                        var ellipsis = new SegmentLine(((IRenderable)new Markup($"[yellow]{ellipsisText}[/]")).Render(options, maxWidth));

                        if (OverflowCropping == VerticalOverflowCropping.Bottom)
                        {
                            // Remove bottom lines
                            var index = Math.Min(_console.Profile.Height - 1, lines.Count);
                            var count = lines.Count - index;
                            lines.RemoveRange(index, count);
                            lines.Add(ellipsis);
                        }
                        else
                        {
                            // Remove top lines
                            var start = lines.Count - _console.Profile.Height;
                            lines.RemoveRange(0, start + 1);
                            lines.Insert(0, ellipsis);
                        }

                        shape = SegmentShape.Calculate(options, lines);
                    }

                    DidOverflow = true;
                }

                // KEY PATTERN: Inflate - shape only grows, never shrinks
                _shape = _shape == null ? shape : _shape.Value.Inflate(shape);
                
                // KEY PATTERN: Apply padding to match max dimensions
                _shape.Value.Apply(options, ref lines);

                foreach (var (_, _, last, line) in lines.Enumerate())
                {
                    foreach (var item in line)
                    {
                        yield return item;
                    }

                    if (!last)
                    {
                        yield return Segment.LineBreak;
                    }
                }

                yield break;
            }

            _shape = null;
        }
    }
}
```

---

## 2. SegmentShape.cs

**Source**: `src/Spectre.Console/Rendering/SegmentShape.cs`

```csharp
namespace Spectre.Console.Rendering;

internal readonly struct SegmentShape
{
    public int Width { get; }
    public int Height { get; }

    public SegmentShape(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public static SegmentShape Calculate(RenderOptions options, List<SegmentLine> lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        var height = lines.Count;
        var width = lines.Max(l => Segment.CellCount(l));

        return new SegmentShape(width, height);
    }

    /// <summary>
    /// Returns a new shape with max dimensions of this and other.
    /// This is the "inflate" pattern - dimensions only grow, never shrink.
    /// </summary>
    public SegmentShape Inflate(SegmentShape other)
    {
        return new SegmentShape(
            Math.Max(Width, other.Width),
            Math.Max(Height, other.Height));
    }

    /// <summary>
    /// Pads segment lines to match this shape's dimensions.
    /// Adds trailing spaces to Width, blank lines to Height.
    /// </summary>
    public void Apply(RenderOptions options, ref List<SegmentLine> lines)
    {
        // Pad each line horizontally to Width
        foreach (var line in lines)
        {
            var length = Segment.CellCount(line);
            var missing = Width - length;
            if (missing > 0)
            {
                line.Add(Segment.Padding(missing));
            }
        }

        // Add blank lines vertically to reach Height
        if (lines.Count < Height && Width > 0)
        {
            var missing = Height - lines.Count;
            for (var i = 0; i < missing; i++)
            {
                lines.Add(new SegmentLine
                {
                    Segment.Padding(Width),
                });
            }
        }
    }
}
```

---

## 3. AnsiSequences.cs (Relevant Methods)

**Source**: `src/Spectre.Console/AnsiSequences.cs`

```csharp
namespace Spectre.Console;

internal static class AnsiSequences
{
    // Cursor Up - moves cursor up N lines
    public static string CUU(int count) => $"\u001b[{count}A";
    
    // Cursor Down - moves cursor down N lines  
    public static string CUD(int count) => $"\u001b[{count}B";
    
    // Cursor Forward - moves cursor right N columns
    public static string CUF(int count) => $"\u001b[{count}C";
    
    // Cursor Back - moves cursor left N columns
    public static string CUB(int count) => $"\u001b[{count}D";
    
    // Cursor Position - moves cursor to row, column
    public static string CUP(int row, int column) => $"\u001b[{row};{column}H";
    
    // Erase in Display
    // 0 = cursor to end, 1 = start to cursor, 2 = entire screen, 3 = entire screen + scrollback
    public static string ED(int mode) => $"\u001b[{mode}J";
    
    // Erase in Line
    // 0 = cursor to end, 1 = start to cursor, 2 = entire line
    public static string EL(int mode) => $"\u001b[{mode}K";
}
```

---

## 4. ControlCode.cs

**Source**: `src/Spectre.Console/Widgets/ControlCode.cs`

```csharp
namespace Spectre.Console;

/// <summary>
/// A control code - emits raw ANSI without measuring.
/// </summary>
public sealed class ControlCode : Renderable
{
    private readonly Segment _segment;

    public ControlCode(string control)
    {
        _segment = Segment.Control(control);
    }

    protected override Measurement Measure(RenderOptions options, int maxWidth)
    {
        return new Measurement(0, 0);  // Control codes have no width
    }

    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        if (options.Ansi)
        {
            yield return _segment;
        }
    }
}
```

---

## 5. StringExtensions.Repeat (Helper)

Used by `RestoreCursor()`:

```csharp
internal static string Repeat(this string text, int count)
{
    if (count <= 0) return string.Empty;
    return string.Concat(Enumerable.Repeat(text, count));
}
```

---

## Usage Notes

### Cursor Positioning Pattern

```
Initial state:        After rendering 3 lines:    After update to 2 lines:
                      Line 1                       Line 1 (new)
                      Line 2                       Line 2 (new)
                      Line 3                       [blank - padded]
cursor here →         cursor here →                cursor here →
```

1. **PositionCursor()**: Move cursor to start of renderable area
   - `\r` (carriage return to column 0)
   - `CUU(height-1)` (move up to first line)

2. **Render with Apply()**: Content padded to max dimensions
   - Lines padded with spaces to max width
   - Blank lines added to reach max height

3. **RestoreCursor()**: Clear and restore to clean state
   - `\r` + `EL(2)` (go to column 0, clear line)
   - Repeat `CUU(1)` + `EL(2)` for remaining lines

### Why This Prevents Phantom Lines

The `Inflate()` pattern ensures the shape only grows. If content shrinks from 5 lines to 3 lines:
- `_shape.Height` stays at 5
- `Apply()` adds 2 blank padding lines
- Total output is always 5 lines, overwriting any previous content

This eliminates the "leftover lines" bug that occurs when manually managing cursor position without dimension tracking.
