using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using BitPantry.CommandLine.Input;
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
    private readonly IAnsiConsole? _console;
    private readonly IPrompt? _prompt;
    private bool _isEngaged;
    private GhostState? _currentGhostState;
    private int _menuLineLength;

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
    /// Gets the current menu items (for testing).
    /// </summary>
    public IReadOnlyList<CompletionItem>? MenuItems => _currentMenuState?.Items;

    /// <summary>
    /// Gets the currently selected menu index (for testing).
    /// </summary>
    public int MenuSelectedIndex => _currentMenuState?.SelectedIndex ?? -1;

    /// <summary>
    /// Gets the total number of menu items (for testing).
    /// </summary>
    public int MenuItemCount => _currentMenuState?.Items.Count ?? 0;

    /// <summary>
    /// Gets the currently selected menu item text, or null if no menu is open (for testing).
    /// </summary>
    public string? SelectedItemText => _currentMenuState?.Items.Count > 0 && _currentMenuState.SelectedIndex >= 0
        ? _currentMenuState.Items[_currentMenuState.SelectedIndex].InsertText
        : null;

    /// <summary>
    /// Initializes a new instance of the <see cref="AutoCompleteController"/> class.
    /// </summary>
    /// <param name="orchestrator">The completion orchestrator.</param>
    /// <param name="console">The console for ghost text rendering.</param>
    /// <param name="prompt">The prompt for cursor positioning calculations.</param>
    public AutoCompleteController(ICompletionOrchestrator? orchestrator = null, IAnsiConsole? console = null, IPrompt? prompt = null)
    {
        _orchestrator = orchestrator;
        _console = console;
        _prompt = prompt;
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

        switch (action.Type)
        {
            case CompletionActionType.InsertText:
                // Single match - auto-accept by inserting the text
                if (!string.IsNullOrEmpty(action.InsertText))
                {
                    InsertCompletion(inputLine, action.InsertText);
                }
                break;
                
            case CompletionActionType.OpenMenu:
                // Multiple matches - engage menu mode and render the menu
                _isEngaged = true;
                _currentMenuState = action.MenuState;
                RenderMenu(inputLine);
                break;
                
            case CompletionActionType.NoMatches:
                // No matches - do nothing
                break;
        }
    }
    
    private MenuState? _currentMenuState;

    /// <summary>
    /// Inserts the completion text, replacing any partial input.
    /// </summary>
    private void InsertCompletion(Input.ConsoleLineMirror inputLine, string completionText)
    {
        // Find the start of the current word (for replacement)
        var buffer = inputLine.Buffer;
        var position = inputLine.BufferPosition;
        
        // Find word start by going back to last space or start of buffer
        int wordStart = position;
        while (wordStart > 0 && buffer[wordStart - 1] != ' ')
        {
            wordStart--;
        }
        
        // Calculate the partial text being replaced
        var partialLength = position - wordStart;
        
        // Remove the partial text
        for (int i = 0; i < partialLength; i++)
        {
            inputLine.Backspace();
        }
        
        // Insert the completion text followed by a space
        inputLine.Write(completionText + " ");
    }
    
    /// <summary>
    /// Renders the autocomplete menu below the input line.
    /// Uses IAnsiConsole cursor movement instead of raw Console API.
    /// </summary>
    private void RenderMenu(Input.ConsoleLineMirror inputLine)
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0 || _console == null)
            return;

        // Build the menu line as a string first
        var menuBuilder = new StringBuilder();
        var items = _currentMenuState.Items;
        var selectedIndex = _currentMenuState.SelectedIndex;
        
        for (int i = 0; i < Math.Min(items.Count, _currentMenuState.ViewportSize); i++)
        {
            var item = items[i];
            if (i == selectedIndex)
            {
                menuBuilder.Append($"[invert]{item.InsertText}[/]");
            }
            else
            {
                menuBuilder.Append(item.InsertText);
            }
            
            if (i < items.Count - 1)
                menuBuilder.Append("  ");
        }
        
        if (items.Count > _currentMenuState.ViewportSize)
        {
            menuBuilder.Append($" (+{items.Count - _currentMenuState.ViewportSize} more)");
        }

        // Save current buffer position so we can restore cursor
        var originalBufferPos = inputLine.BufferPosition;
        var bufferLength = inputLine.Buffer.Length;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        inputLine.HideCursor();
        
        // Move cursor to end of input line first
        var stepsToEnd = bufferLength - originalBufferPos;
        if (stepsToEnd > 0)
        {
            _console.Cursor.MoveRight(stepsToEnd);
        }
        
        // Write menu on new line
        // After WriteLine, cursor is at column 0 of line 2
        // After MarkupLine, cursor is at column 0 of line 3
        _console.WriteLine();
        _console.MarkupLine(menuBuilder.ToString());
        _menuLineLength = menuBuilder.Length;
        
        // Cursor is now at column 0 of line 3
        // Move up 2 lines to get back to input line
        _console.Cursor.MoveUp(2);
        
        // After MoveUp(2), cursor is at column 0 of input line
        // The actual cursor position should be at: promptLength + originalBufferPos
        var targetColumn = promptLength + originalBufferPos;
        if (targetColumn > 0)
        {
            _console.Cursor.MoveRight(targetColumn);
        }
        
        inputLine.ShowCursor();
    }
    
    /// <summary>
    /// Clears the menu display from the screen.
    /// </summary>
    /// <param name="inputLine">The current input line, used to restore cursor position.</param>
    private void ClearMenu(Input.ConsoleLineMirror? inputLine = null)
    {
        if (!_isEngaged || _console == null || _menuLineLength == 0)
            return;
            
        // Save current position and hide cursor
        _console.Cursor.Hide();
        
        // Move down to where the menu is (2 lines down: 1 for newline, 1 for menu content)
        _console.Cursor.MoveDown(2);
        
        // Move to start of line and clear the menu content
        _console.Write("\r");  // Carriage return to column 0
        _console.Write(new string(' ', _menuLineLength + 20));  // Clear with spaces
        _console.Write("\r");  // Back to column 0
        
        // Also clear the blank line above (the WriteLine output)
        _console.Cursor.MoveUp(1);
        _console.Write(new string(' ', _menuLineLength + 20));
        _console.Write("\r");
        
        // Move back up to input line
        _console.Cursor.MoveUp(1);
        
        // Restore cursor to correct column position
        if (inputLine != null)
        {
            var promptLength = _prompt?.GetPromptLength() ?? 0;
            var targetColumn = promptLength + inputLine.BufferPosition;
            if (targetColumn > 0)
            {
                _console.Cursor.MoveRight(targetColumn);
            }
        }
        
        _currentMenuState = null;
        _menuLineLength = 0;
        
        _console.Cursor.Show();
    }

    /// <summary>
    /// Moves to the next autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void NextOption(Input.ConsoleLineMirror inputLine)
    {
        var action = _orchestrator?.HandleDownArrow();
        if (action?.MenuState != null)
        {
            ClearMenu(inputLine);  // Clear first, this resets _currentMenuState
            _currentMenuState = action.MenuState;  // Then set the new state
            RenderMenu(inputLine);
        }
    }

    /// <summary>
    /// Moves to the previous autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void PreviousOption(Input.ConsoleLineMirror inputLine)
    {
        var action = _orchestrator?.HandleUpArrow();
        if (action?.MenuState != null)
        {
            ClearMenu(inputLine);  // Clear first, this resets _currentMenuState
            _currentMenuState = action.MenuState;  // Then set the new state
            RenderMenu(inputLine);
        }
    }

    /// <summary>
    /// Cancels the current autocomplete session.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void Cancel(Input.ConsoleLineMirror inputLine)
    {
        _orchestrator?.HandleEscape();
        ClearMenu(inputLine);
        _isEngaged = false;
    }

    /// <summary>
    /// Accepts the currently selected autocomplete option.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    public void Accept(Input.ConsoleLineMirror inputLine)
    {
        var action = _orchestrator?.HandleEnter();
        ClearMenu(inputLine);
        if (action?.InsertText != null)
        {
            InsertCompletion(inputLine, action.InsertText);
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
        ClearMenu(inputLine);
        ClearGhost();
        _isEngaged = false;
    }

    /// <summary>
    /// Handles character input while the menu is open, filtering the results.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    /// <param name="character">The character that was typed.</param>
    /// <returns>A task that completes when filtering is done.</returns>
    public async Task HandleCharacterWhileMenuOpenAsync(Input.ConsoleLineMirror inputLine, char character)
    {
        if (_orchestrator == null || !_isEngaged)
            return;

        var action = await _orchestrator.HandleCharacterAsync(
            character,
            inputLine.Buffer,
            inputLine.BufferPosition);

        switch (action.Type)
        {
            case CompletionActionType.SelectionChanged:
                // Menu is filtered - update the display
                ClearMenu(inputLine);
                _currentMenuState = action.MenuState;
                if (_currentMenuState?.Items.Count > 0)
                {
                    RenderMenu(inputLine);
                }
                else
                {
                    // No items left after filtering - close menu
                    _isEngaged = false;
                }
                break;

            case CompletionActionType.CloseMenu:
                // No matches - close menu
                ClearMenu(inputLine);
                _isEngaged = false;
                break;

            default:
                // Unexpected action type - close menu to be safe
                ClearMenu(inputLine);
                _isEngaged = false;
                break;
        }
    }

    /// <summary>
    /// Updates the ghost text suggestion based on current input.
    /// </summary>
    /// <param name="input">The current input text.</param>
    /// <param name="cursorPosition">The current cursor position.</param>
    /// <returns>A task that completes when the ghost text has been updated.</returns>
    public async Task UpdateGhostAsync(string input, int cursorPosition)
    {
        // Don't show ghost when menu is open
        if (_isEngaged)
        {
            ClearGhost();
            return;
        }

        // BUG FIX: Don't show ghost text when cursor is not at the end of the input.
        // Ghost text is rendered at the cursor position, so if there's content after
        // the cursor, the ghost would visually overwrite that content.
        if (cursorPosition < (input?.Length ?? 0))
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
