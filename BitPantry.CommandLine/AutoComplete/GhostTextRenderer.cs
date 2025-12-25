using System;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Renders ghost text suggestions in muted ANSI style.
/// </summary>
/// <remarks>
/// Ghost text appears after the cursor position in a muted color (dark gray)
/// and is visually distinct from actual input. The ghost text represents
/// a completion suggestion that can be accepted with Right Arrow or End key.
/// </remarks>
public class GhostTextRenderer
{
    private readonly IAnsiConsole _console;
    private readonly Style _ghostStyle;
    private int _lastGhostLength;

    /// <summary>
    /// Default ghost text color (dark gray).
    /// </summary>
    public static readonly Color DefaultGhostColor = Color.Grey;

    /// <summary>
    /// Initializes a new instance of the <see cref="GhostTextRenderer"/> class.
    /// </summary>
    /// <param name="console">The console to render to.</param>
    /// <param name="ghostColor">Optional custom ghost text color.</param>
    public GhostTextRenderer(IAnsiConsole console, Color? ghostColor = null)
    {
        _console = console ?? throw new ArgumentNullException(nameof(console));
        _ghostStyle = new Style(foreground: ghostColor ?? DefaultGhostColor);
    }

    /// <summary>
    /// Renders ghost text at the current cursor position.
    /// </summary>
    /// <param name="ghostState">The ghost state to render.</param>
    /// <remarks>
    /// The ghost text is rendered in muted color after the cursor.
    /// The cursor position is restored after rendering.
    /// </remarks>
    public void Render(GhostState ghostState)
    {
        if (ghostState == null || !ghostState.IsVisible)
            return;

        RenderGhostText(ghostState.GhostText);
    }

    /// <summary>
    /// Renders ghost text string at the current position.
    /// </summary>
    /// <param name="ghostText">The ghost text to render.</param>
    public void RenderGhostText(string ghostText)
    {
        if (string.IsNullOrEmpty(ghostText))
            return;

        // Render ghost text in muted style
        _console.Write(ghostText, _ghostStyle);
        _lastGhostLength = ghostText.Length;

        // Move cursor back to original position (before ghost text)
        _console.Cursor.MoveLeft(ghostText.Length);
    }

    /// <summary>
    /// Clears the ghost text from the display.
    /// </summary>
    /// <param name="ghostLength">The length of ghost text to clear.</param>
    /// <remarks>
    /// This clears the ghost text by overwriting with spaces.
    /// </remarks>
    public void Clear(int ghostLength)
    {
        if (ghostLength <= 0)
            return;

        // Clear ghost text area with spaces
        _console.Write(new string(' ', ghostLength));
        
        // Move cursor back
        _console.Cursor.MoveLeft(ghostLength);
        
        _lastGhostLength = 0;
    }

    /// <summary>
    /// Clears the ghost text based on ghost state.
    /// </summary>
    /// <param name="ghostState">The ghost state to clear.</param>
    public void Clear(GhostState ghostState)
    {
        if (ghostState?.GhostText != null)
        {
            Clear(ghostState.GhostText.Length);
        }
    }

    /// <summary>
    /// Clears the last rendered ghost text.
    /// </summary>
    public void ClearLast()
    {
        if (_lastGhostLength > 0)
        {
            Clear(_lastGhostLength);
        }
    }

    /// <summary>
    /// Updates the ghost text display, clearing old and rendering new.
    /// </summary>
    /// <param name="previousGhost">The previous ghost state to clear.</param>
    /// <param name="newGhost">The new ghost state to render.</param>
    public void Update(GhostState previousGhost, GhostState newGhost)
    {
        // Clear previous ghost if longer than new
        var previousLength = previousGhost?.GhostText?.Length ?? 0;
        var newLength = newGhost?.GhostText?.Length ?? 0;

        if (previousLength > newLength)
        {
            Clear(previousLength);
        }

        // Render new ghost
        Render(newGhost);
    }
}
