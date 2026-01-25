namespace BitPantry.CommandLine.AutoComplete.Rendering
{
    /// <summary>
    /// Static helper class with ANSI escape sequence constants and builders.
    /// These are the standard ANSI/VT100 control sequences used for terminal manipulation.
    /// </summary>
    public static class AnsiCodes
    {
        // === Cursor Visibility ===

        /// <summary>
        /// ANSI sequence to hide the cursor: ESC[?25l
        /// </summary>
        public const string HideCursor = "\u001b[?25l";

        /// <summary>
        /// ANSI sequence to show the cursor: ESC[?25h
        /// </summary>
        public const string ShowCursor = "\u001b[?25h";

        // === Line Clearing ===

        /// <summary>
        /// ANSI sequence to clear the entire line: ESC[2K
        /// </summary>
        public const string ClearLine = "\u001b[2K";

        /// <summary>
        /// ANSI sequence to clear from cursor to end of line: ESC[K (same as ESC[0K)
        /// </summary>
        public const string ClearToEndOfLine = "\u001b[K";

        // === Special Characters ===

        /// <summary>
        /// Carriage return - moves cursor to column 0
        /// </summary>
        public const string CarriageReturn = "\r";

        // === Cursor Movement Builders ===

        /// <summary>
        /// Moves cursor up N lines: ESC[nA
        /// </summary>
        public static string CursorUp(int n) => $"\u001b[{n}A";

        /// <summary>
        /// Moves cursor down N lines: ESC[nB
        /// </summary>
        public static string CursorDown(int n) => $"\u001b[{n}B";

        /// <summary>
        /// Moves cursor forward (right) N columns: ESC[nC
        /// </summary>
        public static string CursorForward(int n) => $"\u001b[{n}C";

        /// <summary>
        /// Moves cursor right N columns. Alias for CursorForward.
        /// </summary>
        public static string CursorRight(int n) => CursorForward(n);

        /// <summary>
        /// Moves cursor back (left) N columns: ESC[nD
        /// </summary>
        public static string CursorBack(int n) => $"\u001b[{n}D";

        /// <summary>
        /// Moves cursor left N columns. Alias for CursorBack.
        /// </summary>
        public static string CursorLeft(int n) => CursorBack(n);

        /// <summary>
        /// Erase in Line with specified mode: ESC[nK
        /// Mode 0: Clear from cursor to end of line
        /// Mode 1: Clear from start of line to cursor
        /// Mode 2: Clear entire line
        /// </summary>
        public static string EraseLine(int mode) => $"\u001b[{mode}K";

        /// <summary>
        /// Moves cursor to specific row and column: ESC[row;colH
        /// Note: 1-indexed (top-left is 1,1)
        /// </summary>
        public static string CursorPosition(int row, int column) => $"\u001b[{row};{column}H";

        /// <summary>
        /// Erase in Display with specified mode: ESC[nJ
        /// Mode 0: Clear from cursor to end of screen
        /// Mode 1: Clear from start of screen to cursor
        /// Mode 2: Clear entire screen
        /// Mode 3: Clear entire screen + scrollback buffer
        /// </summary>
        public static string EraseDisplay(int mode) => $"\u001b[{mode}J";
    }
}
