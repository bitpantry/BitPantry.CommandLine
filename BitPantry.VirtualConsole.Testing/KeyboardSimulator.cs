using BitPantry.CommandLine;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Input;

namespace BitPantry.VirtualConsole.Testing;

/// <summary>
/// Simulates keyboard input for autocomplete testing.
/// Works with ConsoleLineMirror and AutoCompleteController to simulate user input.
/// </summary>
public class KeyboardSimulator : IKeyboardSimulator
{
    private readonly ConsoleLineMirror _inputLine;
    private readonly AutoCompleteController? _autoComplete;
    private readonly Action<ConsoleKeyInfo>? _onKeyPress;

    /// <summary>
    /// Creates a KeyboardSimulator for the given input line and optional autocomplete controller.
    /// </summary>
    /// <param name="inputLine">The input line to type into.</param>
    /// <param name="autoComplete">Optional autocomplete controller for Tab/navigation.</param>
    /// <param name="onKeyPress">Optional callback for each key press.</param>
    public KeyboardSimulator(ConsoleLineMirror inputLine, AutoCompleteController? autoComplete = null, Action<ConsoleKeyInfo>? onKeyPress = null)
    {
        _inputLine = inputLine ?? throw new ArgumentNullException(nameof(inputLine));
        _autoComplete = autoComplete;
        _onKeyPress = onKeyPress;
    }

    /// <inheritdoc/>
    public async Task TypeTextAsync(string text)
    {
        foreach (char c in text)
        {
            var keyInfo = CharToKeyInfo(c);
            await ProcessKeyAsync(keyInfo);
        }
    }

    /// <inheritdoc/>
    public void TypeText(string text)
    {
        TypeTextAsync(text).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task PressKeyAsync(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        var keyInfo = new ConsoleKeyInfo(
            KeyToChar(key, shift),
            key,
            shift,
            alt,
            control);
        await ProcessKeyAsync(keyInfo);
    }

    /// <inheritdoc/>
    public void PressKey(ConsoleKey key, bool shift = false, bool alt = false, bool control = false)
    {
        PressKeyAsync(key, shift, alt, control).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public async Task PressTabAsync() => await PressKeyAsync(ConsoleKey.Tab);

    /// <inheritdoc/>
    public void PressTab() => PressKey(ConsoleKey.Tab);

    /// <inheritdoc/>
    public async Task PressEnterAsync() => await PressKeyAsync(ConsoleKey.Enter);

    /// <inheritdoc/>
    public void PressEnter() => PressKey(ConsoleKey.Enter);

    /// <inheritdoc/>
    public async Task PressEscapeAsync() => await PressKeyAsync(ConsoleKey.Escape);

    /// <inheritdoc/>
    public void PressEscape() => PressKey(ConsoleKey.Escape);

    /// <inheritdoc/>
    public async Task PressBackspaceAsync() => await PressKeyAsync(ConsoleKey.Backspace);

    /// <inheritdoc/>
    public void PressBackspace() => PressKey(ConsoleKey.Backspace);

    /// <inheritdoc/>
    public async Task PressDownArrowAsync() => await PressKeyAsync(ConsoleKey.DownArrow);

    /// <inheritdoc/>
    public void PressDownArrow() => PressKey(ConsoleKey.DownArrow);

    /// <inheritdoc/>
    public async Task PressUpArrowAsync() => await PressKeyAsync(ConsoleKey.UpArrow);

    /// <inheritdoc/>
    public void PressUpArrow() => PressKey(ConsoleKey.UpArrow);

    /// <inheritdoc/>
    public async Task PressRightArrowAsync() => await PressKeyAsync(ConsoleKey.RightArrow);

    /// <inheritdoc/>
    public void PressRightArrow() => PressKey(ConsoleKey.RightArrow);

    /// <inheritdoc/>
    public async Task PressLeftArrowAsync() => await PressKeyAsync(ConsoleKey.LeftArrow);

    /// <inheritdoc/>
    public void PressLeftArrow() => PressKey(ConsoleKey.LeftArrow);

    private async Task ProcessKeyAsync(ConsoleKeyInfo keyInfo)
    {
        _onKeyPress?.Invoke(keyInfo);

        switch (keyInfo.Key)
        {
            case ConsoleKey.Tab:
                if (_autoComplete != null)
                {
                    if (!_autoComplete.IsEngaged)
                    {
                        // Start autocomplete with Tab
                        await _autoComplete.Begin(_inputLine);
                    }
                    else if (keyInfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                    {
                        // Shift+Tab goes to previous option
                        _autoComplete.PreviousOption(_inputLine);
                    }
                    else
                    {
                        // Tab cycles to next option
                        _autoComplete.NextOption(_inputLine);
                    }
                }
                break;

            case ConsoleKey.Enter:
                if (_autoComplete?.IsEngaged == true)
                {
                    // Accept the current selection and close menu
                    _autoComplete.Accept(_inputLine);
                }
                // Enter without autocomplete engaged would submit - handled by caller
                break;

            case ConsoleKey.Escape:
                if (_autoComplete?.IsEngaged == true)
                {
                    // Cancel the autocomplete menu
                    _autoComplete.Cancel(_inputLine);
                }
                break;

            case ConsoleKey.DownArrow:
                if (_autoComplete?.IsEngaged == true)
                {
                    // Navigate down in menu
                    _autoComplete.NextOption(_inputLine);
                }
                break;

            case ConsoleKey.UpArrow:
                if (_autoComplete?.IsEngaged == true)
                {
                    // Navigate up in menu
                    _autoComplete.PreviousOption(_inputLine);
                }
                break;

            case ConsoleKey.RightArrow:
                // Right Arrow accepts ghost text when at end of line (T039)
                if (_autoComplete?.HasGhostText == true && _inputLine.BufferPosition == _inputLine.Buffer.Length)
                {
                    _autoComplete.AcceptGhost(_inputLine);
                }
                else
                {
                    _inputLine.MovePositionRight();
                }
                break;

            case ConsoleKey.LeftArrow:
                // Clear ghost when moving cursor left (away from end of line)
                _autoComplete?.ClearGhost();
                _inputLine.MovePositionLeft();
                break;

            case ConsoleKey.Home:
                // Home key moves cursor to start of line
                _autoComplete?.ClearGhost();
                // Close menu if open - moving cursor away
                if (_autoComplete?.IsEngaged == true)
                {
                    _autoComplete.End(_inputLine);
                }
                _inputLine.MoveToPosition(0);
                break;

            case ConsoleKey.End:
                // Close menu if open - moving cursor away from completion context
                if (_autoComplete?.IsEngaged == true)
                {
                    _autoComplete.End(_inputLine);
                }
                // End key accepts ghost text when at end of line (T039)
                else if (_autoComplete?.HasGhostText == true && _inputLine.BufferPosition == _inputLine.Buffer.Length)
                {
                    _autoComplete.AcceptGhost(_inputLine);
                }
                else
                {
                    // Move to end of buffer
                    _inputLine.MoveToPosition(_inputLine.Buffer.Length);
                }
                break;

            case ConsoleKey.Delete:
                // Delete key removes character at cursor position
                if (_autoComplete?.IsEngaged == true)
                {
                    // Close menu on delete
                    _autoComplete.End(_inputLine);
                }
                _inputLine.Delete();
                break;

            case ConsoleKey.Backspace:
                // Clear ghost BEFORE backspace changes cursor position
                _autoComplete?.ClearGhost();
                
                // Always perform the backspace on the input line
                _inputLine.Backspace();
                
                if (_autoComplete?.IsEngaged == true)
                {
                    // Handle backspace while menu is open - may close menu if back to trigger point
                    await _autoComplete.HandleBackspaceWhileMenuOpenAsync(_inputLine);
                }
                else if (_autoComplete != null)
                {
                    // Update ghost text after backspace
                    await _autoComplete.UpdateGhostAsync(_inputLine.Buffer, _inputLine.BufferPosition);
                }
                break;

            case ConsoleKey.Spacebar:
                // FR-003: Space key handling is context-aware
                if (_autoComplete?.IsEngaged == true)
                {
                    // Check if cursor is inside quotes
                    bool insideQuotes = _inputLine.Buffer.IsInsideQuotes(_inputLine.BufferPosition);
                    
                    if (insideQuotes)
                    {
                        // Inside quotes: space is part of a quoted value, filter with it
                        _inputLine.Write(' ');
                        await _autoComplete.HandleCharacterWhileMenuOpenAsync(_inputLine, ' ');
                    }
                    else
                    {
                        // Outside quotes: space closes menu without accepting selection
                        _autoComplete.End(_inputLine);
                        _inputLine.Write(' ');
                    }
                }
                else
                {
                    // No menu engaged - just write the space
                    _inputLine.Write(' ');
                    if (_autoComplete != null)
                    {
                        await _autoComplete.UpdateGhostAsync(_inputLine.Buffer, _inputLine.BufferPosition);
                    }
                }
                break;

            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    _inputLine.Write(keyInfo.KeyChar);
                    
                    // Handle typing while menu is engaged
                    if (_autoComplete?.IsEngaged == true)
                    {
                        await _autoComplete.HandleCharacterWhileMenuOpenAsync(_inputLine, keyInfo.KeyChar);
                    }
                    // Update ghost text for typed characters
                    else if (_autoComplete != null)
                    {
                        await _autoComplete.UpdateGhostAsync(_inputLine.Buffer, _inputLine.BufferPosition);
                    }
                }
                break;
        }
    }

    private static ConsoleKeyInfo CharToKeyInfo(char c)
    {
        var key = CharToKey(c);
        bool shift = char.IsUpper(c) || "!@#$%^&*()_+{}|:\"<>?~".Contains(c);
        return new ConsoleKeyInfo(c, key, shift, false, false);
    }

    private static ConsoleKey CharToKey(char c)
    {
        if (char.IsLetter(c))
        {
            return (ConsoleKey)((int)ConsoleKey.A + (char.ToUpper(c) - 'A'));
        }
        if (char.IsDigit(c))
        {
            return (ConsoleKey)((int)ConsoleKey.D0 + (c - '0'));
        }
        
        return c switch
        {
            ' ' => ConsoleKey.Spacebar,
            '-' => ConsoleKey.OemMinus,
            '_' => ConsoleKey.OemMinus,
            '.' => ConsoleKey.OemPeriod,
            '/' => ConsoleKey.Oem2,
            '\\' => ConsoleKey.Oem5,
            ':' => ConsoleKey.Oem1,
            ';' => ConsoleKey.Oem1,
            '\'' => ConsoleKey.Oem7,
            '"' => ConsoleKey.Oem7,
            ',' => ConsoleKey.OemComma,
            '=' => ConsoleKey.OemPlus,
            '+' => ConsoleKey.OemPlus,
            _ => ConsoleKey.NoName
        };
    }

    private static char KeyToChar(ConsoleKey key, bool shift)
    {
        return key switch
        {
            ConsoleKey.Tab => '\t',
            ConsoleKey.Enter => '\r',
            ConsoleKey.Spacebar => ' ',
            ConsoleKey.Backspace => '\b',
            _ => '\0'
        };
    }
}
