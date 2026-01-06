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
    /// Menu items to display (as strings for backwards compatibility).
    /// </summary>
    public IReadOnlyList<string> Items { get; }

    /// <summary>
    /// Completion items with match data (for highlighting).
    /// </summary>
    private readonly IReadOnlyList<CompletionItem>? _completionItems;

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
    /// Style for match highlighting (blue for filter matches).
    /// </summary>
    private static readonly Style MatchHighlightStyle = Style.Parse("blue");

    /// <summary>
    /// Style for scroll indicators.
    /// </summary>
    private static readonly Style DimStyle = new Style(Color.Grey, decoration: Decoration.Dim);

    /// <summary>
    /// Creates a new AutoCompleteMenuRenderable with string items.
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
        _completionItems = null;
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

    /// <summary>
    /// Creates a new AutoCompleteMenuRenderable with CompletionItem items (supports match highlighting).
    /// </summary>
    /// <param name="items">Completion items to display.</param>
    /// <param name="selectedIndex">Currently selected item index (-1 for no selection).</param>
    /// <param name="viewportStart">First visible item index.</param>
    /// <param name="viewportSize">Maximum visible items.</param>
    public AutoCompleteMenuRenderable(
        IReadOnlyList<CompletionItem> items,
        int selectedIndex,
        int viewportStart,
        int viewportSize)
    {
        _completionItems = items ?? Array.Empty<CompletionItem>();
        Items = _completionItems.Select(i => i.DisplayText).ToList();
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
        // Empty items - display "(no matches)" message
        if (Items.Count == 0)
        {
            yield return new Segment("  (no matches)", DimStyle);
            yield return Segment.LineBreak;
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
            var displayText = Items[i];
            var isSelected = i == SelectedIndex;
            var baseStyle = isSelected ? HighlightStyle : Style.Plain;

            // Indent
            yield return new Segment("  ", Style.Plain);

            // Get MatchRanges if we have CompletionItems
            var matchRanges = _completionItems?[i].MatchRanges ?? Array.Empty<Range>();

            if (matchRanges.Count > 0)
            {
                // Render with match highlighting (works for both selected and unselected items)
                foreach (var segment in RenderWithMatchHighlight(displayText, matchRanges, baseStyle))
                {
                    yield return segment;
                }
            }
            else
            {
                // Render plain or with selection style
                yield return new Segment(displayText, baseStyle);
            }

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

    /// <summary>
    /// Renders a string with highlighted match ranges.
    /// </summary>
    /// <param name="text">The text to render.</param>
    /// <param name="matchRanges">The ranges of text to highlight.</param>
    /// <param name="baseStyle">The base style for non-matched text (e.g., selection style).</param>
    private IEnumerable<Segment> RenderWithMatchHighlight(string text, IReadOnlyList<Range> matchRanges, Style baseStyle)
    {
        if (matchRanges.Count == 0)
        {
            yield return new Segment(text, baseStyle);
            yield break;
        }

        // Sort ranges by start position
        var sortedRanges = matchRanges
            .Select(r => (Start: r.Start.Value, End: r.End.Value))
            .OrderBy(r => r.Start)
            .ToList();

        // Combine base style with match highlight for matched portions
        var matchStyle = baseStyle.Combine(MatchHighlightStyle);

        int currentPos = 0;
        foreach (var (start, end) in sortedRanges)
        {
            // Clamp to valid text bounds
            var clampedStart = Math.Max(0, Math.Min(start, text.Length));
            var clampedEnd = Math.Max(0, Math.Min(end, text.Length));

            // Render text before match
            if (currentPos < clampedStart)
            {
                yield return new Segment(text[currentPos..clampedStart], baseStyle);
            }

            // Render matched portion
            if (clampedStart < clampedEnd)
            {
                yield return new Segment(text[clampedStart..clampedEnd], matchStyle);
            }

            currentPos = clampedEnd;
        }

        // Render remaining text after last match
        if (currentPos < text.Length)
        {
            yield return new Segment(text[currentPos..], baseStyle);
        }
    }
}
