using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// Renders ghost text suggestion in dim gray style.
/// Extends Spectre's Renderable for isolated testing and composition.
/// </summary>
public class GhostTextRenderable : Renderable
{
    /// <summary>
    /// The ghost text to display.
    /// </summary>
    public string GhostText { get; }

    /// <summary>
    /// Style for the ghost text.
    /// </summary>
    public Style GhostStyle { get; }

    /// <summary>
    /// Default style for ghost text - dim gray.
    /// </summary>
    public static readonly Style DefaultGhostStyle = new Style(Color.Grey, decoration: Decoration.Dim);

    /// <summary>
    /// Creates a new GhostTextRenderable with default dim gray style.
    /// </summary>
    /// <param name="ghostText">The ghost text to display.</param>
    public GhostTextRenderable(string ghostText)
        : this(ghostText, DefaultGhostStyle)
    {
    }

    /// <summary>
    /// Creates a new GhostTextRenderable with custom style.
    /// </summary>
    /// <param name="ghostText">The ghost text to display.</param>
    /// <param name="ghostStyle">Style for the ghost text.</param>
    public GhostTextRenderable(string ghostText, Style ghostStyle)
    {
        GhostText = ghostText ?? string.Empty;
        GhostStyle = ghostStyle ?? DefaultGhostStyle;
    }

    /// <inheritdoc/>
    protected override IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
    {
        // Empty or whitespace-only ghost text returns no segments
        if (string.IsNullOrWhiteSpace(GhostText))
        {
            yield break;
        }

        // Render the ghost text with the configured style
        yield return new Segment(GhostText, GhostStyle);
    }
}
