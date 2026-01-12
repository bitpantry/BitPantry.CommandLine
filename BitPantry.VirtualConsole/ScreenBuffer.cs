using System;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// A 2D grid of screen cells representing the virtual terminal display.
    /// Manages character storage and cursor position.
    /// </summary>
    public class ScreenBuffer
    {
        private readonly ScreenCell[,] _cells;

        /// <summary>
        /// Width of the buffer in columns.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// Height of the buffer in rows.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Current cursor row (0-based, clamped to valid range).
        /// </summary>
        public int CursorRow { get; private set; }

        /// <summary>
        /// Current cursor column (0-based, clamped to valid range).
        /// </summary>
        public int CursorColumn { get; private set; }

        /// <summary>
        /// Current style applied to new characters.
        /// </summary>
        public CellStyle CurrentStyle { get; private set; }

        /// <summary>
        /// Whether the cursor is currently visible.
        /// This is a state flag that can be toggled by DECTCEM sequences (CSI ? 25 h/l).
        /// Default is true.
        /// </summary>
        public bool CursorVisible { get; set; } = true;

        /// <summary>
        /// Whether auto-wrap mode is enabled.
        /// When enabled, writing past the right margin wraps to the next line.
        /// This is controlled by DECAWM sequences (CSI ? 7 h/l).
        /// Default is true.
        /// </summary>
        public bool AutoWrapMode { get; set; } = true;

        /// <summary>
        /// Creates a new screen buffer with the specified dimensions.
        /// </summary>
        /// <param name="width">Width in columns (must be > 0).</param>
        /// <param name="height">Height in rows (must be > 0).</param>
        public ScreenBuffer(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width), "Width must be greater than 0.");
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height), "Height must be greater than 0.");

            Width = width;
            Height = height;
            _cells = new ScreenCell[height, width];
            CurrentStyle = CellStyle.Default;

            // Initialize all cells to default (space with default style)
            for (int row = 0; row < height; row++)
            {
                for (int col = 0; col < width; col++)
                {
                    _cells[row, col] = new ScreenCell();
                }
            }
        }

        /// <summary>
        /// Writes a character at the current cursor position with the current style,
        /// then advances the cursor with line wrapping.
        /// Uses "delayed wrap" (pending wrap) behavior like real terminals:
        /// - When writing at the last column, the cursor stays at that column in a "pending wrap" state
        /// - The wrap only happens when the NEXT character is written
        /// This matches ANSI terminal behavior and ensures Spectre.Console's cursor math works correctly.
        /// </summary>
        /// <param name="c">The character to write.</param>
        public void WriteChar(char c)
        {
            // If we're in "pending wrap" state (at column Width), wrap now before writing
            if (AutoWrapMode && CursorColumn >= Width)
            {
                CursorColumn = 0;
                CursorRow++;
                if (CursorRow >= Height)
                {
                    CursorRow = Height - 1;
                }
            }
            
            if (CursorRow >= 0 && CursorRow < Height && CursorColumn >= 0 && CursorColumn < Width)
            {
                _cells[CursorRow, CursorColumn] = new ScreenCell(c, CurrentStyle);
            }
            
            CursorColumn++;
            
            // Note: We do NOT immediately wrap here. The cursor can be at column Width (off-screen)
            // This is the "pending wrap" state. Wrapping happens when the next char is written.
            // However, if AutoWrapMode is off, clamp the column.
            if (!AutoWrapMode && CursorColumn >= Width)
            {
                CursorColumn = Width - 1;
            }
        }

        /// <summary>
        /// Moves the cursor to an absolute position (clamped to valid range).
        /// </summary>
        /// <param name="row">Target row (0-based).</param>
        /// <param name="column">Target column (0-based).</param>
        public void MoveCursor(int row, int column)
        {
            CursorRow = row;
            CursorColumn = column;
            ClampCursor();
        }

        /// <summary>
        /// Moves the cursor relative to its current position (clamped to valid range).
        /// </summary>
        /// <param name="deltaRow">Rows to move (positive = down, negative = up).</param>
        /// <param name="deltaColumn">Columns to move (positive = right, negative = left).</param>
        public void MoveCursorRelative(int deltaRow, int deltaColumn)
        {
            CursorRow += deltaRow;
            CursorColumn += deltaColumn;
            ClampCursor();
        }

        /// <summary>
        /// Gets the cell at the specified position.
        /// Returns a default cell if position is out of bounds.
        /// </summary>
        /// <param name="row">Row (0-based).</param>
        /// <param name="column">Column (0-based).</param>
        /// <returns>The cell at the position, or a default cell if out of bounds.</returns>
        public ScreenCell GetCell(int row, int column)
        {
            if (row < 0 || row >= Height || column < 0 || column >= Width)
            {
                return new ScreenCell();
            }
            return _cells[row, column];
        }

        /// <summary>
        /// Sets the cell at the specified position to the given character and style.
        /// Does nothing if position is out of bounds.
        /// </summary>
        /// <param name="row">Row (0-based).</param>
        /// <param name="column">Column (0-based).</param>
        /// <param name="character">The character to set.</param>
        /// <param name="style">The style to apply.</param>
        internal void SetCell(int row, int column, char character, CellStyle style)
        {
            if (row >= 0 && row < Height && column >= 0 && column < Width)
            {
                _cells[row, column] = new ScreenCell(character, style);
            }
        }

        /// <summary>
        /// Gets a row wrapper for the specified row.
        /// </summary>
        /// <param name="row">Row index (0-based).</param>
        /// <returns>A ScreenRow wrapper for the row.</returns>
        public ScreenRow GetRow(int row)
        {
            return new ScreenRow(this, row);
        }

        /// <summary>
        /// Clears the screen (or portion of it) based on the mode.
        /// </summary>
        /// <param name="mode">The clear mode.</param>
        public void ClearScreen(ClearMode mode)
        {
            switch (mode)
            {
                case ClearMode.ToEnd:
                    // Clear from cursor to end of screen
                    ClearRange(CursorRow, CursorColumn, Height - 1, Width - 1);
                    break;
                case ClearMode.ToBeginning:
                    // Clear from beginning to cursor
                    ClearRange(0, 0, CursorRow, CursorColumn);
                    break;
                case ClearMode.All:
                    // Clear entire screen
                    ClearRange(0, 0, Height - 1, Width - 1);
                    break;
            }
        }

        /// <summary>
        /// Clears the current line (or portion of it) based on the mode.
        /// </summary>
        /// <param name="mode">The clear mode.</param>
        public void ClearLine(ClearMode mode)
        {
            switch (mode)
            {
                case ClearMode.ToEnd:
                    // Clear from cursor to end of line
                    for (int col = CursorColumn; col < Width; col++)
                    {
                        _cells[CursorRow, col] = new ScreenCell(' ', CurrentStyle);
                    }
                    break;
                case ClearMode.ToBeginning:
                    // Clear from beginning of line to cursor
                    for (int col = 0; col <= CursorColumn && col < Width; col++)
                    {
                        _cells[CursorRow, col] = new ScreenCell(' ', CurrentStyle);
                    }
                    break;
                case ClearMode.All:
                    // Clear entire line
                    for (int col = 0; col < Width; col++)
                    {
                        _cells[CursorRow, col] = new ScreenCell(' ', CurrentStyle);
                    }
                    break;
            }
        }

        /// <summary>
        /// Sets the current style for subsequent character writes.
        /// </summary>
        /// <param name="style">The style to apply.</param>
        public void ApplyStyle(CellStyle style)
        {
            CurrentStyle = style;
        }

        /// <summary>
        /// Resets the current style to default.
        /// </summary>
        public void ResetStyle()
        {
            CurrentStyle = CellStyle.Default;
        }

        /// <summary>
        /// Resets cursor to home position (0, 0).
        /// </summary>
        public void ResetCursor()
        {
            CursorRow = 0;
            CursorColumn = 0;
        }

        /// <summary>
        /// Clears all cells to default (space with default style) and resets cursor.
        /// Note: Does not reset CurrentStyle.
        /// </summary>
        public void Clear()
        {
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    _cells[row, col] = new ScreenCell();
                }
            }
            ResetCursor();
        }

        private void ClearRange(int startRow, int startCol, int endRow, int endCol)
        {
            for (int row = startRow; row <= endRow && row < Height; row++)
            {
                int colStart = (row == startRow) ? startCol : 0;
                int colEnd = (row == endRow) ? endCol : Width - 1;
                for (int col = colStart; col <= colEnd && col < Width; col++)
                {
                    _cells[row, col] = new ScreenCell(' ', CurrentStyle);
                }
            }
        }

        private void ClampCursor()
        {
            if (CursorRow < 0) CursorRow = 0;
            if (CursorRow >= Height) CursorRow = Height - 1;
            if (CursorColumn < 0) CursorColumn = 0;
            if (CursorColumn >= Width) CursorColumn = Width - 1;
        }
    }
}
