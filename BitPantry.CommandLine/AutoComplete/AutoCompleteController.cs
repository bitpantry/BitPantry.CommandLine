using System;
using System.Threading.Tasks;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Coordinates autocomplete operations for the input line.
/// This is a transitional controller that bridges to the new completion system.
/// </summary>
/// <remarks>
/// Full implementation will be completed in Phase 7 (User Story 6 - Interactive Menu).
/// Currently provides stub implementations to allow the project to compile.
/// </remarks>
public class AutoCompleteController : IDisposable
{
    private readonly ICompletionOrchestrator? _orchestrator;
    private readonly GhostTextRenderer? _ghostRenderer;
    private bool _isEngaged;
    private GhostState? _currentGhostState;

    /// <summary>
    /// Gets whether autocomplete is currently engaged/active.
    /// </summary>
    public bool IsEngaged => _isEngaged;

    /// <summary>
    /// Gets the current ghost text, if any.
    /// </summary>
    public string? CurrentGhostText => _currentGhostState?.GhostText;

    /// <summary>
    /// Gets whether ghost text is currently visible.
    /// </summary>
    public bool HasGhostText => _currentGhostState?.IsVisible == true;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoCompleteController"/> class.
    /// </summary>
    /// <param name="orchestrator">The completion orchestrator.</param>
    /// <param name="console">The console for ghost text rendering.</param>
    public AutoCompleteController(ICompletionOrchestrator? orchestrator = null, IAnsiConsole? console = null)
    {
        _orchestrator = orchestrator;
        if (console != null)
        {
            _ghostRenderer = new GhostTextRenderer(console);
        }
    }

    /// <summary>
    /// Begins autocomplete for the given input line.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public async Task Begin(Input.ConsoleLineMirror inputLine)
    {
        if (_orchestrator == null)
            return;

        var action = await _orchestrator.HandleTabAsync(
            inputLine.Buffer,
            inputLine.BufferPosition);

        if (action.Type == CompletionActionType.OpenMenu)
        {
            _isEngaged = true;
            // TODO: Render menu in Phase 7
        }
    }

    /// <summary>
    /// Moves to the next autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void NextOption(Input.ConsoleLineMirror inputLine)
    {
        _orchestrator?.HandleDownArrow();
        // TODO: Render updated menu in Phase 7
    }

    /// <summary>
    /// Moves to the previous autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void PreviousOption(Input.ConsoleLineMirror inputLine)
    {
        _orchestrator?.HandleUpArrow();
        // TODO: Render updated menu in Phase 7
    }

    /// <summary>
    /// Cancels the current autocomplete session.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void Cancel(Input.ConsoleLineMirror inputLine)
    {
        _orchestrator?.HandleEscape();
        _isEngaged = false;
        // TODO: Clear menu display in Phase 7
    }

    /// <summary>
    /// Accepts the currently selected autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void Accept(Input.ConsoleLineMirror inputLine)
    {
        var action = _orchestrator?.HandleEnter();
        if (action?.InsertText != null)
        {
            // TODO: Apply the insert text to inputLine in Phase 7
        }
        _isEngaged = false;
    }

    /// <summary>
    /// Ends the current autocomplete session without accepting.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void End(Input.ConsoleLineMirror inputLine)
    {
        _orchestrator?.HandleEscape();
        _isEngaged = false;
    }

    /// <summary>
    /// Updates the ghost text suggestion based on current input.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPosition">The current cursor position (not currently used).</param>
    /// <returns>A task that completes when the ghost text has been updated.</returns>
    public async Task UpdateGhostAsync(string input, int cursorPosition)
    {
        // Don't show ghost when menu is open
        if (_isEngaged)
        {
            ClearGhost();
            return;
        }

        if (_orchestrator == null)
            return;

        var ghostText = await _orchestrator.UpdateGhostTextAsync(input);
        
        var previousGhost = _currentGhostState;
        
        if (!string.IsNullOrEmpty(ghostText))
        {
            _currentGhostState = GhostState.FromSuggestion(input, input + ghostText, GhostSuggestionSource.History);
            _ghostRenderer?.Update(previousGhost, _currentGhostState);
        }
        else
        {
            ClearGhost();
        }
    }

    /// <summary>
    /// Clears the current ghost text display.
    /// </summary>
    public void ClearGhost()
    {
        if (_currentGhostState != null)
        {
            _ghostRenderer?.Clear(_currentGhostState);
            _currentGhostState = null;
        }
    }

    /// <summary>
    /// Accepts the current ghost text suggestion.
    /// </summary>
    /// <param name="inputLine">The input line to apply the ghost text to.</param>
    /// <returns>True if ghost text was accepted, false otherwise.</returns>
    public bool AcceptGhost(Input.ConsoleLineMirror inputLine)
    {
        if (_currentGhostState == null || !_currentGhostState.IsVisible)
            return false;

        var ghostText = _currentGhostState.GhostText;
        if (string.IsNullOrEmpty(ghostText))
            return false;

        // Clear the ghost text display first
        ClearGhost();

        // Insert the ghost text at cursor position
        foreach (var c in ghostText)
        {
            inputLine.Write(c);
        }

        return true;
    }

    /// <summary>
    /// Disposes resources used by the controller.
    /// </summary>
    public void Dispose()
    {
        // Nothing to dispose currently
    }
}
