using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete.Rendering;
using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Coordinates autocomplete operations for the input line.
/// Uses MenuLiveRenderer for vertical menu display with Inflate pattern to prevent phantom lines.
/// </summary>
public class AutoCompleteController : IDisposable
{
    private readonly ICompletionOrchestrator? _orchestrator;
    private readonly GhostTextRenderer? _ghostRenderer;
    private readonly IAnsiConsole? _console;
    private readonly IPrompt? _prompt;
    private readonly IMenuRenderer? _menuRenderer;
    private bool _isEngaged;
    private GhostState? _currentGhostState;
    private int _lastInputCursorColumn;  // Saved cursor position for menu operations

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
            _menuRenderer = new MenuLiveRenderer(console);
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
    /// <summary>
    /// Gets the menu item strings for rendering.
    /// Uses DisplayText for the menu (user-friendly display) while InsertText is used when selecting.
    /// </summary>
    private IReadOnlyList<string> GetMenuItemStrings()
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0)
            return Array.Empty<string>();

        return _currentMenuState.Items.Select(i => i.DisplayText).ToList();
    }

    /// <summary>
    /// Positions cursor at the start of the menu area (below input line) and saves input position.
    /// </summary>
    private void MoveToMenuArea(Input.ConsoleLineMirror inputLine)
    {
        if (_console == null) return;

        var originalBufferPos = inputLine.BufferPosition;
        var bufferLength = inputLine.Buffer.Length;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        var terminalWidth = _console.Profile.Width;

        // Save cursor column for restoration
        _lastInputCursorColumn = (promptLength + originalBufferPos) % terminalWidth;

        inputLine.HideCursor();

        // Move cursor to end of input line
        var stepsToEnd = bufferLength - originalBufferPos;
        if (stepsToEnd > 0)
        {
            _console.Cursor.MoveRight(stepsToEnd);
        }

        // Move to next line (menu area starts below input)
        _console.WriteLine();
    }

    /// <summary>
    /// Restores cursor to the saved input line position.
    /// </summary>
    private void RestoreInputCursor(Input.ConsoleLineMirror inputLine)
    {
        if (_console == null) return;

        // Move to saved column position
        _console.Write(new ControlCode($"\r{AnsiCodes.CursorForward(_lastInputCursorColumn)}"));
        
        inputLine.ShowCursor();
    }

    /// <summary>
    /// Renders the autocomplete menu below the input line using MenuLiveRenderer.
    /// </summary>
    private void RenderMenu(Input.ConsoleLineMirror inputLine)
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0 || _menuRenderer == null)
            return;

        MoveToMenuArea(inputLine);

        var items = GetMenuItemStrings();
        _menuRenderer.Show(
            items,
            _currentMenuState.SelectedIndex,
            _currentMenuState.ViewportStart,
            _currentMenuState.ViewportSize);

        // Move back up past the menu (height tracked by MenuLiveRenderer)
        var menuShape = (_menuRenderer as MenuLiveRenderer)?.CurrentShape;
        var linesToMoveUp = menuShape?.Height ?? items.Count;
        if (linesToMoveUp > 0)
        {
            _console!.Write(new ControlCode(AnsiCodes.CursorUp(linesToMoveUp)));
        }

        RestoreInputCursor(inputLine);
    }

    /// <summary>
    /// Clears the menu display from the screen using MenuLiveRenderer.
    /// </summary>
    private void ClearMenu(Input.ConsoleLineMirror? inputLine = null)
    {
        if (_menuRenderer == null || !_menuRenderer.IsVisible || _console == null)
            return;

        // Get current menu height (RestoreCursor expects cursor at bottom of menu)
        var menuShape = (_menuRenderer as MenuLiveRenderer)?.CurrentShape;
        var currentHeight = menuShape?.Height ?? 0;

        // Move down to the BOTTOM of the menu area (where RestoreCursor expects the cursor to be)
        if (currentHeight > 0)
        {
            _console.Write(new ControlCode(AnsiCodes.CursorDown(currentHeight)));
        }
        else
        {
            // Fallback - just move down 1
            _console.Cursor.MoveDown(1);
        }

        // Hide clears and handles its own cursor positioning (moves up while clearing)
        _menuRenderer.Hide();

        // After RestoreCursor, we're at the top of where the menu was (first menu line)
        // Move up one more to get back to the input line
        _console.Cursor.MoveUp(1);

        // Restore cursor column if we have input line context
        if (inputLine != null)
        {
            var promptLength = _prompt?.GetPromptLength() ?? 0;
            var terminalWidth = _console.Profile.Width;
            var cursorColumn = (promptLength + inputLine.BufferPosition) % terminalWidth;
            _console.Write(new ControlCode($"\r{AnsiCodes.CursorForward(cursorColumn)}"));
        }

        _currentMenuState = null;
    }

    /// <summary>
    /// Updates the menu content in place using MenuLiveRenderer.
    /// </summary>
    private void UpdateMenuInPlace(Input.ConsoleLineMirror inputLine)
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0 || _menuRenderer == null || _console == null)
            return;

        var originalBufferPos = inputLine.BufferPosition;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        var terminalWidth = _console.Profile.Width;

        inputLine.HideCursor();

        // Get current menu height BEFORE update (this is what PositionCursor expects)
        var menuShape = (_menuRenderer as MenuLiveRenderer)?.CurrentShape;
        var currentHeight = menuShape?.Height ?? 0;

        // Move down to the BOTTOM of the menu area (where PositionCursor expects the cursor to be)
        // We need to move: 1 (to get below input) + (height - 1) (to get to last line of menu)
        // This equals moving down by the menu height
        if (currentHeight > 0)
        {
            _console.Write(new ControlCode(AnsiCodes.CursorDown(currentHeight)));
        }
        else
        {
            // Fallback for first update - just move down 1
            _console.Cursor.MoveDown(1);
        }

        // Update uses the Inflate pattern internally - PositionCursor moves to top, then re-renders
        var items = GetMenuItemStrings();
        _menuRenderer.Update(
            items,
            _currentMenuState.SelectedIndex,
            _currentMenuState.ViewportStart,
            _currentMenuState.ViewportSize);

        // After render, cursor is at bottom of menu
        // Move back up past the menu
        var newShape = (_menuRenderer as MenuLiveRenderer)?.CurrentShape;
        var linesToMoveUp = newShape?.Height ?? items.Count;
        if (linesToMoveUp > 0)
        {
            _console.Write(new ControlCode(AnsiCodes.CursorUp(linesToMoveUp)));
        }

        // Restore cursor column position (we're now at input line)
        var cursorColumn = (promptLength + originalBufferPos) % terminalWidth;
        _console.Write(new ControlCode($"\r{AnsiCodes.CursorForward(cursorColumn)}"));

        inputLine.ShowCursor();
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
            _currentMenuState = action.MenuState;
            UpdateMenuInPlace(inputLine);
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
            _currentMenuState = action.MenuState;
            UpdateMenuInPlace(inputLine);
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
