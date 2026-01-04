using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Renders the autocomplete menu as a vertical list of items with selection highlighting.
/// Extends Spectre's Renderable for isolated testing and composition.
/// </summary>
public class AutoCompleteMenuRenderable : Renderable
{
    /// <summary>
    /// Menu items to display.
    /// </summary>
    public IReadOnlyList<string> Items { get; }

    /// <summary>
    /// Currently selected item index. -1 for no selection.
    /// </summary>
    public int SelectedIndex { get; }

    /// <summary>
    /// First visible item index (for viewport scrolling).
    /// </summary>
    public int ViewportStart { get; }

    /// <summary>
    /// Maximum visible items in the viewport.
    /// </summary>
    public int ViewportSize { get; }

    /// <summary>
    /// Last visible item index (exclusive).
    /// </summary>
    public int ViewportEnd => Math.Min(ViewportStart + ViewportSize, Items.Count);

    /// <summary>
    /// Whether there are items above the viewport.
    /// </summary>
    public bool HasScrollUp => ViewportStart > 0;

    /// <summary>
    /// Whether there are items below the viewport.
    /// </summary>
    public bool HasScrollDown => ViewportEnd < Items.Count;

    /// <summary>
    /// Style for selected (highlighted) items.
    /// </summary>
    private static readonly Style HighlightStyle = Style.Parse("invert");

    /// <summary>
    /// Style for scroll indicators.
    /// </summary>
    private static readonly Style DimStyle = new Style(Color.Grey, decoration: Decoration.Dim);

    /// <summary>
    /// Creates a new AutoCompleteMenuRenderable.
    /// </summary>
    /// <param name="items">Menu items to display.</param>
    /// <param name="selectedIndex">Currently selected item index (-1 for no selection).</param>
    /// <param name="viewportStart">First visible item index.</param>
    /// <param name="viewportSize">Maximum visible items.</param>
    public AutoCompleteMenuRenderable(
        IReadOnlyList<string> items,
        int selectedIndex,
        int viewportStart,
        int viewportSize)
    {
        Items = items ?? Array.Empty<string>();
        ViewportStart = Math.Max(0, viewportStart);
        ViewportSize = Math.Max(1, viewportSize);
        
        // Clamp selectedIndex to valid range
        if (Items.Count == 0)
        {
            SelectedIndex = -1;
        }
        else
        {
            SelectedIndex = Math.Clamp(selectedIndex, -1, Items.Count - 1);
        }
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Empty items - return no segments
        if (Items.Count == 0)
        {
            yield break;
        }

        // Scroll up indicator
        if (HasScrollUp)
        {
            var aboveCount = ViewportStart;
            yield return new Segment($"  (↑ {aboveCount} more)", DimStyle);
            yield return Segment.LineBreak;
        }

        // Render visible items in viewport
        var end = ViewportEnd;
        for (int i = ViewportStart; i < end; i++)
        {
            var item = Items[i];
            var isSelected = i == SelectedIndex;
            var style = isSelected ? HighlightStyle : Style.Plain;

            // Indent and render item
            yield return new Segment("  ", Style.Plain);
            yield return new Segment(item, style);
            yield return Segment.LineBreak;
        }

        // Scroll down indicator
        if (HasScrollDown)
        {
            var belowCount = Items.Count - ViewportEnd;
            yield return new Segment($"  (↓ {belowCount} more)", DimStyle);
            yield return Segment.LineBreak;
        }
    }
}
