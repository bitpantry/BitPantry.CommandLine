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
    private int _menuLineCount;  // Current line count
    private int _maxMenuLineCount;  // Maximum height ever rendered (like Spectre's _shape.Inflate)

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
    /// Builds the menu content string based on the current menu state, respecting viewport scrolling.
    /// </summary>
    private string BuildMenuContent()
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0)
            return string.Empty;

        var menuBuilder = new StringBuilder();
        var items = _currentMenuState.Items;
        var selectedIndex = _currentMenuState.SelectedIndex;
        var viewportStart = _currentMenuState.ViewportStart;
        var viewportEnd = Math.Min(items.Count, viewportStart + _currentMenuState.ViewportSize);
        
        // Show indicator if there are items before the viewport
        if (viewportStart > 0)
        {
            menuBuilder.Append($"[dim](+{viewportStart} before)[/]  ");
        }
        
        // Render items in the viewport
        for (int i = viewportStart; i < viewportEnd; i++)
        {
            var item = items[i];
            if (i == selectedIndex)
            {
                menuBuilder.Append($"[invert]{Markup.Escape(item.InsertText)}[/]");
            }
            else
            {
                menuBuilder.Append(Markup.Escape(item.InsertText));
            }
            
            if (i < viewportEnd - 1)
                menuBuilder.Append("  ");
        }
        
        // Show indicator if there are items after the viewport
        var itemsAfter = items.Count - viewportEnd;
        if (itemsAfter > 0)
        {
            menuBuilder.Append($"  [dim](+{itemsAfter} more)[/]");
        }

        return menuBuilder.ToString();
    }

    /// <summary>
    /// Renders the autocomplete menu below the input line.
    /// Uses Spectre Console's pattern: track max height, always render to that height.
    /// </summary>
    private void RenderMenu(Input.ConsoleLineMirror inputLine)
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0 || _console == null)
            return;

        // Build the menu content
        var menuText = BuildMenuContent();
        var plainText = Markup.Remove(menuText);

        // Save current buffer position so we can restore cursor
        var originalBufferPos = inputLine.BufferPosition;
        var bufferLength = inputLine.Buffer.Length;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        var terminalWidth = _console.Profile.Width;
        
        inputLine.HideCursor();
        
        // Move to menu start position (input_line + 1, column 0)
        var stepsToEnd = bufferLength - originalBufferPos;
        if (stepsToEnd > 0)
        {
            _console.Cursor.MoveRight(stepsToEnd);
        }
        
        // Calculate actual line count using proper cell width calculation
        _menuLineLength = plainText.Length;
        var contentLineCount = Math.Max(1, (int)Math.Ceiling((double)plainText.GetCellWidth() / terminalWidth));
        _menuLineCount = contentLineCount;
        
        // Track maximum height (like Spectre's _shape.Inflate)
        _maxMenuLineCount = Math.Max(_maxMenuLineCount, contentLineCount);
        
        // Write newline to move to menu line, then menu content
        _console.WriteLine();
        _console.Markup(menuText);
        
        // If content is shorter than max, pad with empty lines
        // (Spectre Console's SegmentShape.Apply pattern)
        if (contentLineCount < _maxMenuLineCount)
        {
            var padding = _maxMenuLineCount - contentLineCount;
            for (int i = 0; i < padding; i++)
            {
                _console.WriteLine();
            }
        }
        
        // Move cursor back up to input position using max height
        // (like Spectre's PositionCursor: linesToMoveUp = _shape.Value.Height - 1, but we add 1 for newline)
        var linesToMoveUp = _maxMenuLineCount;
        _console.Write(new ControlCode($"\x1b[{linesToMoveUp}A"));  // CUU - Cursor Up
        
        // Move to original cursor column position
        var cursorColumn = (promptLength + originalBufferPos) % terminalWidth;
        _console.Write(new ControlCode($"\r\x1b[{cursorColumn}C"));  // CR + CUF - Move to column
        
        inputLine.ShowCursor();
    }
    
    /// <summary>
    /// Clears the menu display from the screen.
    /// Uses Spectre Console's RestoreCursor pattern: CR + EL(2) + (CUU(1) + EL(2)) repeated.
    /// </summary>
    /// <param name="inputLine">The current input line, used to restore cursor position.</param>
    private void ClearMenu(Input.ConsoleLineMirror? inputLine = null)
    {
        if (!_isEngaged || _console == null || _maxMenuLineCount == 0)
            return;
            
        // Hide cursor during clearing
        _console.Cursor.Hide();
        
        // Save current cursor column position
        var terminalWidth = _console.Profile.Width;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        var cursorColumn = inputLine != null 
            ? (promptLength + inputLine.BufferPosition) % terminalWidth 
            : 0;
        
        // Move down to menu area
        _console.Cursor.MoveDown(1);
        
        // Clear using Spectre's RestoreCursor pattern:
        // \r + EL(2) + (CUU(1) + EL(2)).Repeat(linesToClear)
        // This clears each line individually from bottom to top
        var linesToClear = _maxMenuLineCount - 1;
        var clearSequence = "\r\x1b[2K";  // CR + EL(2) - erase entire line
        for (int i = 0; i < linesToClear; i++)
        {
            clearSequence += "\x1b[1A\x1b[2K";  // CUU(1) + EL(2)
        }
        _console.Write(new ControlCode(clearSequence));
        
        // Move back up to input line (we're now at top of menu area)
        _console.Cursor.MoveUp(1);
        
        // Restore cursor column position
        _console.Write(new ControlCode($"\r\x1b[{cursorColumn}C"));  // CR + CUF - Move to column
        
        _currentMenuState = null;
        _menuLineLength = 0;
        _menuLineCount = 0;
        _maxMenuLineCount = 0;  // Reset max height when menu closes
        
        _console.Cursor.Show();
    }

    /// <summary>
    /// Updates the menu content in place without clearing and re-rendering.
    /// Uses Spectre Console's pattern: track max height, always render to that height.
    /// </summary>
    /// <param name="inputLine">The current input line.</param>
    private void UpdateMenuInPlace(Input.ConsoleLineMirror inputLine)
    {
        if (_currentMenuState == null || _currentMenuState.Items.Count == 0 || _console == null)
            return;

        // Build the new menu content
        var newMenuContent = BuildMenuContent();
        var newPlainText = Markup.Remove(newMenuContent);
        var terminalWidth = _console.Profile.Width;
        
        // Calculate new content line count
        var newContentLineCount = Math.Max(1, (int)Math.Ceiling((double)newPlainText.GetCellWidth() / terminalWidth));
        
        // If content GROWS beyond current max, we can't safely update in place
        // because writing extra lines causes terminal scroll which breaks cursor math.
        // Fall back to clear + re-render which handles positioning correctly.
        if (newContentLineCount > _maxMenuLineCount)
        {
            // Save menu state, clear with old max, then render fresh with new content
            var savedState = _currentMenuState;
            var wasEngaged = _isEngaged;
            _isEngaged = true;  // Ensure ClearMenu works
            ClearMenu(inputLine);
            _isEngaged = wasEngaged;
            _currentMenuState = savedState;
            RenderMenu(inputLine);
            return;
        }
        
        var originalBufferPos = inputLine.BufferPosition;
        var promptLength = _prompt?.GetPromptLength() ?? 0;
        
        inputLine.HideCursor();
        
        // Max height stays the same or content is smaller (shrinking case)
        // _maxMenuLineCount already >= newContentLineCount, no update needed
        
        // Move down to menu line
        _console.Cursor.MoveDown(1);
        
        // Clear all lines using Spectre's pattern: CR + EL(2) for each line
        // Start from current position and clear down to max height
        var clearSequence = "\r\x1b[2K";  // CR + EL(2) - erase first line
        for (int i = 1; i < _maxMenuLineCount; i++)
        {
            clearSequence += "\x1b[1B\x1b[2K";  // CUD(1) + EL(2) - move down and erase
        }
        _console.Write(new ControlCode(clearSequence));
        
        // Move back to first menu line
        if (_maxMenuLineCount > 1)
        {
            _console.Write(new ControlCode($"\x1b[{_maxMenuLineCount - 1}A"));  // CUU - Cursor Up
        }
        _console.Write(new ControlCode("\r"));  // CR - back to column 0
        
        // Write the new menu
        _console.Markup(newMenuContent);
        
        // Update tracked metrics
        _menuLineLength = newPlainText.Length;
        _menuLineCount = newContentLineCount;
        
        // If content is shorter than max, pad with empty lines
        // (Spectre Console's SegmentShape.Apply pattern - ensures cursor is always at consistent position)
        if (newContentLineCount < _maxMenuLineCount)
        {
            var padding = _maxMenuLineCount - newContentLineCount;
            for (int i = 0; i < padding; i++)
            {
                _console.WriteLine();
            }
        }
        
        // Move cursor back up to input position using max height
        // After padding, cursor is always at line (_maxMenuLineCount - 1) of menu area
        var linesToMoveUp = _maxMenuLineCount;
        _console.Write(new ControlCode($"\x1b[{linesToMoveUp}A"));  // CUU - Cursor Up
        
        // Restore cursor column position
        var cursorColumn = (promptLength + originalBufferPos) % terminalWidth;
        _console.Write(new ControlCode($"\r\x1b[{cursorColumn}C"));  // CR + CUF - Move to column
        
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
