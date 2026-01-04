using Spectre.Console;
using System;

namespace BitPantry.CommandLine.AutoComplete.Rendering;

/// <summary>
/// High-level ghost text renderer that wraps GhostTextRenderable for in-place updates.
/// Implements the IGhostRenderer interface for controller integration.
/// Uses the Inflate pattern to prevent ghost text artifacts when text shrinks.
/// 
/// Ghost text is rendered inline (horizontal) after the user's cursor position.
/// </summary>
public class GhostLiveRenderer : IGhostRenderer
{
    private readonly IAnsiConsole _console;
    private bool _isVisible;
    private SegmentShape _shape;
    private string _currentText = string.Empty;

    /// <summary>
    /// Gets whether the ghost text is currently visible.
    /// </summary>
    public bool IsVisible => _isVisible;

    /// <summary>
    /// Gets the current shape tracking (for testing).
    /// </summary>
    public SegmentShape CurrentShape => _shape;

    /// <summary>
    /// Creates a new GhostLiveRenderer.
    /// </summary>
    /// <param name="console">The console to render to.</param>
    public GhostLiveRenderer(IAnsiConsole console)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _shape = new SegmentShape(0, 0);
    }

    /// <inheritdoc/>
    public void Show(string text)
    {
        _isVisible = true;
        _currentText = text ?? string.Empty;

        // Update shape tracking (Inflate pattern)
        var newShape = new SegmentShape(_currentText.Length, 1);
        _shape = _shape.Inflate(newShape);

        // Create and render the ghost text
        RenderGhostText(_currentText);
    }

    /// <inheritdoc/>
    public void Update(string text)
    {
        if (!_isVisible)
        {
            Show(text);
            return;
        }

        // First, move back to start of ghost text
        if (_shape.Width > 0)
        {
            _console.Write(PositionCursor());
        }

        // Update the text
        _currentText = text ?? string.Empty;

        // Update shape (Inflate - only grows, never shrinks)
        var newShape = new SegmentShape(_currentText.Length, 1);
        _shape = _shape.Inflate(newShape);

        // Render the new ghost text, padding to max width to clear old chars
        RenderGhostTextWithPadding(_currentText, _shape.Width);
    }

    /// <summary>
    /// Clears any visible ghost text. Alias for Hide().
    /// </summary>
    public void Clear()
    {
        Hide();
    }

    /// <summary>
    /// Hides the ghost text and resets state.
    /// </summary>
    public void Hide()
    {
        if (!_isVisible)
        {
            return;
        }

        // Clear the ghost text area
        if (_shape.Width > 0)
        {
            // Move cursor to start of ghost
            _console.Write(PositionCursor());
            
            // Clear with spaces
            _console.Write(new string(' ', _shape.Width));
            
            // Move back to original position
            _console.Cursor.MoveLeft(_shape.Width);
        }

        _isVisible = false;
        _currentText = string.Empty;
        _shape = new SegmentShape(0, 0);
    }

    /// <summary>
    /// Gets the ANSI sequence to position cursor at start of ghost text.
    /// For horizontal ghost text, this moves cursor left by current width.
    /// </summary>
    /// <returns>ANSI escape sequence string.</returns>
    public string PositionCursor()
    {
        if (_shape.Width <= 0)
        {
            return string.Empty;
        }

        // Move cursor left to start of ghost text
        return AnsiCodes.CursorLeft(_shape.Width);
    }

    /// <summary>
    /// Gets the ANSI sequence to restore cursor after ghost text operations.
    /// For horizontal ghost text, this moves cursor right past the text area.
    /// </summary>
    /// <returns>ANSI escape sequence string.</returns>
    public string RestoreCursor()
    {
        if (_shape.Width <= 0)
        {
            return string.Empty;
        }

        // Move cursor right past ghost text
        return AnsiCodes.CursorRight(_shape.Width);
    }

    /// <summary>
    /// Renders ghost text with dim style.
    /// </summary>
    private void RenderGhostText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // Create and render the ghost renderable
        var renderable = new GhostTextRenderable(text);
        _console.Write(renderable);

        // Move cursor back to before ghost text
        _console.Cursor.MoveLeft(text.Length);
    }

    /// <summary>
    /// Renders ghost text padded to specific width (for clearing old chars).
    /// </summary>
    private void RenderGhostTextWithPadding(string text, int width)
    {
        // Render the ghost text
        if (!string.IsNullOrEmpty(text))
        {
            var renderable = new GhostTextRenderable(text);
            _console.Write(renderable);
        }

        // Pad with spaces to clear any remaining old text
        var paddingNeeded = width - (text?.Length ?? 0);
        if (paddingNeeded > 0)
        {
            _console.Write(new string(' ', paddingNeeded));
        }

        // Move cursor back to before ghost text
        if (width > 0)
        {
            _console.Cursor.MoveLeft(width);
        }
    }
}
