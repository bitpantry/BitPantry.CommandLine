using Spectre.Console;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// High-level menu renderer that wraps MenuLiveRenderable for in-place updates.
/// Implements the IMenuRenderer interface for controller integration.
/// Uses the Inflate pattern to prevent phantom lines when menu content shrinks.
/// </summary>
public class MenuLiveRenderer : IMenuRenderer
{
    private readonly IAnsiConsole _console;
    private readonly MenuLiveRenderable _liveRenderable;
    private bool _isVisible;

    /// <summary>
    /// Gets whether the menu is currently visible.
    /// </summary>
    public bool IsVisible => _isVisible;

    /// <summary>
    /// Gets the current shape tracking (for testing).
    /// </summary>
    public SegmentShape CurrentShape => _liveRenderable.CurrentShape;

    /// <summary>
    /// Creates a new MenuLiveRenderer.
    /// </summary>
    /// <param name="console">The console to render to.</param>
    public MenuLiveRenderer(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _liveRenderable = new MenuLiveRenderable(console);
    }

    /// <inheritdoc/>
    public void Show(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize)
    {
        _isVisible = true;

        // Create the menu renderable
        var menu = new AutoCompleteMenuRenderable(items, selectedIndex, viewportStart, viewportSize);
        _liveRenderable.SetRenderable(menu);

        // Render to console
        _console.Write(_liveRenderable);
    }

    /// <inheritdoc/>
    public void Update(IReadOnlyList<string> items, int selectedIndex, int viewportStart, int viewportSize)
    {
        if (!_isVisible)
        {
            // If not visible, just show
            Show(items, selectedIndex, viewportStart, viewportSize);
            return;
        }

        // First, position cursor at start of previous render
        _console.Write(_liveRenderable.PositionCursor());

        // Create updated menu renderable
        var menu = new AutoCompleteMenuRenderable(items, selectedIndex, viewportStart, viewportSize);
        _liveRenderable.SetRenderable(menu);

        // Re-render (will use Inflate pattern to maintain max dimensions)
        _console.Write(_liveRenderable);
    }

    /// <inheritdoc/>
    public void Hide()
    {
        if (!_isVisible)
        {
            return;
        }

        _isVisible = false;

        // Clear all content using RestoreCursor (moves up and clears each line)
        _console.Write(_liveRenderable.RestoreCursor());

        // Clear the renderable and reset shape tracking
        _liveRenderable.SetRenderable(null);
        _liveRenderable.ResetShape();
    }
}
