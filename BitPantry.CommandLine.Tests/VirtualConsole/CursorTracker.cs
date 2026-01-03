using System;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Tests.VirtualConsole;

/// <summary>
/// Tracks cursor position by parsing ANSI escape sequences.
/// Extracted from VirtualAnsiConsole for reuse with ConsolidatedTestConsole.
/// 
/// Supports:
/// - CUU (Cursor Up): ESC[nA
/// - CUD (Cursor Down): ESC[nB
/// - CUF (Cursor Forward): ESC[nC
/// - CUB (Cursor Back): ESC[nD
/// - CR (Carriage Return): \r
/// - LF (Line Feed): \n
/// - Various clear commands (don't affect position)
/// </summary>
public class CursorTracker
{
    private int _column;
    private int _line;

    // Regex to match ANSI escape sequences: ESC[ followed by optional number and command letter
    private static readonly Regex AnsiPattern = new(@"\u001b\[(\d*)([ABCDHJKfm])", RegexOptions.Compiled);

    /// <summary>
    /// Current column (0-indexed)
    /// </summary>
    public int Column => _column;

    /// <summary>
    /// Current line (0-indexed)
    /// </summary>
    public int Line => _line;

    /// <summary>
    /// Current position as (Column, Line) tuple
    /// </summary>
    public (int Column, int Line) Position => (_column, _line);

    /// <summary>
    /// Processes text that may contain ANSI escape sequences and updates cursor position
    /// </summary>
    /// <param name="text">Text to process, may contain ANSI sequences</param>
    public void ProcessText(string text)
    {
        int i = 0;
        while (i < text.Length)
        {
            // Check for escape sequence
            if (text[i] == '\u001b' && i + 1 < text.Length && text[i + 1] == '[')
            {
                // Find end of sequence
                var match = AnsiPattern.Match(text, i);
                if (match.Success && match.Index == i)
                {
                    ProcessEscapeSequence(match);
                    i += match.Length;
                    continue;
                }
            }

            // Handle special characters
            switch (text[i])
            {
                case '\r':
                    _column = 0;
                    break;
                case '\n':
                    _line++;
                    break;
                default:
                    // Regular character
                    _column++;
                    break;
            }
            i++;
        }
    }

    private void ProcessEscapeSequence(Match match)
    {
        var numberPart = match.Groups[1].Value;
        var command = match.Groups[2].Value;

        // Default to 1 if no number specified
        int n = string.IsNullOrEmpty(numberPart) ? 1 : int.Parse(numberPart);

        switch (command)
        {
            case "A": // CUU - Cursor Up
                _line = Math.Max(0, _line - n);
                break;
            case "B": // CUD - Cursor Down
                _line += n;
                break;
            case "C": // CUF - Cursor Forward
                _column += n;
                break;
            case "D": // CUB - Cursor Back
                _column = Math.Max(0, _column - n);
                break;
            case "J": // ED - Erase Display (doesn't move cursor)
            case "K": // EL - Erase Line (doesn't move cursor)
            case "m": // SGR - Select Graphic Rendition (style, doesn't move cursor)
                // These don't affect cursor position
                break;
            case "H": // CUP - Cursor Position (row;col)
            case "f": // HVP - Horizontal Vertical Position (same as H)
                // For simplicity, we'll handle the basic case
                // Format: ESC[row;colH or ESC[rowH (column defaults to 1)
                // This is less commonly used in our context
                break;
        }
    }

    /// <summary>
    /// Resets cursor position to origin (0, 0)
    /// </summary>
    public void Reset()
    {
        _column = 0;
        _line = 0;
    }

    /// <summary>
    /// Sets cursor position directly (for testing or special cases)
    /// </summary>
    public void SetPosition(int column, int line)
    {
        _column = Math.Max(0, column);
        _line = Math.Max(0, line);
    }
}
