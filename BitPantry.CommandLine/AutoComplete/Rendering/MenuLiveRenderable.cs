using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// A LiveRenderable implementation for menu rendering.
/// Tracks maximum rendered dimensions using the "inflate" pattern to prevent phantom lines.
/// Based on Spectre.Console's internal LiveRenderable class.
/// 
/// <para><b>Spectre Pattern Derivation</b></para>
/// <para>
/// This class is adapted from Spectre.Console's internal LiveRenderable pattern
/// (see https://github.com/spectreconsole/spectre.console/blob/main/src/Spectre.Console/Live/LiveRenderable.cs).
/// </para>
/// 
/// <para><b>Key Patterns Used:</b></para>
/// <list type="bullet">
///   <item><description>
///     <b>Inflate Pattern:</b> Shape dimensions only grow, never shrink. When content shrinks
///     (e.g., fewer menu items), the shape maintains max dimensions seen. This ensures
///     that clearing operations erase ALL previously rendered content, preventing phantom lines.
///   </description></item>
///   <item><description>
///     <b>SegmentShape Tracking:</b> Borrowed from Spectre's SegmentShapeExtensions. We track
///     width and height of rendered content to know exactly how much space to clear.
///   </description></item>
///   <item><description>
///     <b>ControlCode Rendering:</b> Uses Spectre's ControlCode class to emit raw ANSI sequences
///     for cursor positioning without interpretation by the rendering pipeline.
///   </description></item>
///   <item><description>
///     <b>Line Padding:</b> When current content is smaller than max shape, we pad with spaces
///     to explicitly overwrite previous content rather than relying on terminal behavior.
///   </description></item>
/// </list>
/// 
/// <para><b>Thread Safety:</b> All operations are protected by a lock for safe concurrent access.</para>
/// </summary>
public class MenuLiveRenderable : Renderable
{
    private readonly object _lock = new object();
    private readonly IAnsiConsole _console;
    private IRenderable? _renderable;
    private SegmentShape? _shape;

    /// <summary>
    /// Gets the current target renderable being wrapped.
    /// </summary>
    public IRenderable? Target => _renderable;

    /// <summary>
    /// Gets whether a renderable is currently set.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Target))]
    public bool HasRenderable => _renderable != null;

    /// <summary>
    /// Gets the current tracked shape (maximum dimensions seen).
    /// Returns a zero shape if no rendering has occurred.
    /// </summary>
    public SegmentShape CurrentShape => _shape ?? new SegmentShape(0, 0);

    /// <summary>
    /// Creates a new MenuLiveRenderable.
    /// </summary>
    /// <param name="console">The console to render to.</param>
    public MenuLiveRenderable(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
    }

    /// <summary>
    /// Sets the renderable to be displayed.
    /// </summary>
    /// <param name="renderable">The renderable to display, or null to clear.</param>
    public void SetRenderable(IRenderable? renderable)
    {
        lock (_lock)
        {
            _renderable = renderable;
        }
    }

    /// <summary>
    /// Returns a control code to position the cursor at the start of the renderable area.
    /// Used before re-rendering to overwrite previous content.
    /// </summary>
    /// <returns>A ControlCode that positions the cursor.</returns>
    public IRenderable PositionCursor()
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return new ControlCode(string.Empty);
            }

            var linesToMoveUp = _shape.Value.Height - 1;
            if (linesToMoveUp <= 0)
            {
                // Only 1 line or less, just return to start of line
                return new ControlCode("\r");
            }

            // CR = return to column 0, CUU = cursor up N lines
            return new ControlCode("\r" + AnsiCodes.CursorUp(linesToMoveUp));
        }
    }

    /// <summary>
    /// Returns a control code to restore the cursor and clear all rendered content.
    /// Used when hiding the renderable.
    /// </summary>
    /// <returns>A ControlCode that clears and restores cursor position.</returns>
    public IRenderable RestoreCursor()
    {
        lock (_lock)
        {
            if (_shape == null)
            {
                return new ControlCode(string.Empty);
            }

            var linesToClear = _shape.Value.Height - 1;

            // Start with CR (column 0) + EL(2) (clear entire line)
            var sequence = "\r" + AnsiCodes.EraseLine(2);

            // Then for each additional line, move up and clear
            for (int i = 0; i < linesToClear; i++)
            {
                sequence += AnsiCodes.CursorUp(1) + AnsiCodes.EraseLine(2);
            }

            return new ControlCode(sequence);
        }
    }

    /// <summary>
    /// Resets the shape tracking. Called when hiding the renderable.
    /// </summary>
    public void ResetShape()
    {
        lock (_lock)
        {
            _shape = null;
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        lock (_lock)
        {
            if (_renderable != null)
            {
                // Get segments from the wrapped renderable
                var segments = _renderable.Render(options, maxWidth);
                var segmentList = segments.ToList();

                // Split into lines for dimension tracking
                var lines = SplitIntoLines(segmentList);

                // Calculate the current shape
                var currentShape = CalculateShape(lines);

                // KEY PATTERN: Inflate - shape only grows, never shrinks
                _shape = _shape == null ? currentShape : _shape.Value.Inflate(currentShape);

                // Apply padding to match max dimensions
                ApplyPadding(ref lines, _shape.Value);

                // Yield segments with line breaks
                for (int i = 0; i < lines.Count; i++)
                {
                    foreach (var segment in lines[i])
                    {
                        yield return segment;
                    }

                    if (i < lines.Count - 1)
                    {
                        yield return Segment.LineBreak;
                    }
                }

                yield break;
            }

            // No renderable - reset shape
            _shape = null;
        }
    }

    /// <summary>
    /// Splits segments into lines based on LineBreak segments.
    /// </summary>
    private List<List<Segment>> SplitIntoLines(List<Segment> segments)
    {
        var lines = new List<List<Segment>>();
        var currentLine = new List<Segment>();

        foreach (var segment in segments)
        {
            if (segment.IsLineBreak)
            {
                lines.Add(currentLine);
                currentLine = new List<Segment>();
            }
            else
            {
                currentLine.Add(segment);
            }
        }

        // Add the last line if it has content
        if (currentLine.Count > 0)
        {
            lines.Add(currentLine);
        }

        return lines;
    }

    /// <summary>
    /// Calculates the shape (width/height) of the rendered content.
    /// </summary>
    private SegmentShape CalculateShape(List<List<Segment>> lines)
    {
        if (lines.Count == 0)
        {
            return new SegmentShape(0, 0);
        }

        var maxWidth = 0;
        foreach (var line in lines)
        {
            var lineWidth = 0;
            foreach (var segment in line)
            {
                if (!segment.IsControlCode)
                {
                    // Use Text.Length for width calculation (adequate for ASCII command names)
                    lineWidth += segment.Text.Length;
                }
            }
            maxWidth = Math.Max(maxWidth, lineWidth);
        }

        return new SegmentShape(maxWidth, lines.Count);
    }

    /// <summary>
    /// Applies padding to match the target shape dimensions.
    /// Pads lines with spaces to Width, adds blank lines to reach Height.
    /// </summary>
    private void ApplyPadding(ref List<List<Segment>> lines, SegmentShape targetShape)
    {
        // Pad each line horizontally to target width
        foreach (var line in lines)
        {
            var lineWidth = 0;
            foreach (var segment in line)
            {
                if (!segment.IsControlCode)
                {
                    // Use Text.Length for width calculation (adequate for ASCII command names)
                    lineWidth += segment.Text.Length;
                }
            }

            var padding = targetShape.Width - lineWidth;
            if (padding > 0)
            {
                line.Add(Segment.Padding(padding));
            }
        }

        // Add blank lines to reach target height
        while (lines.Count < targetShape.Height)
        {
            var blankLine = new List<Segment>();
            if (targetShape.Width > 0)
            {
                blankLine.Add(Segment.Padding(targetShape.Width));
            }
            lines.Add(blankLine);
        }
    }
}
