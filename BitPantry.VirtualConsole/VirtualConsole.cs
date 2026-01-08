using System;
using System.Text;
using BitPantry.VirtualConsole.AnsiParser;

namespace BitPantry.VirtualConsole
{
    /// <summary>
    /// A virtual terminal emulator that processes ANSI escape sequences
    /// and maintains a 2D screen buffer for testing CLI applications.
    /// </summary>
    public class VirtualConsole
    {
        private readonly ScreenBuffer _buffer;
        private readonly AnsiSequenceParser _parser;
        private readonly CursorProcessor _cursorProcessor;
        private readonly SgrProcessor _sgrProcessor;
        private readonly EraseProcessor _eraseProcessor;

        /// <summary>
        /// Width of the virtual console in columns.
        /// </summary>
        public int Width => _buffer.Width;

        /// <summary>
        /// Height of the virtual console in rows.
        /// </summary>
        public int Height => _buffer.Height;

        /// <summary>
        /// Current cursor row (0-based).
        /// </summary>
        public int CursorRow => _buffer.CursorRow;

        /// <summary>
        /// Current cursor column (0-based).
        /// </summary>
        public int CursorColumn => _buffer.CursorColumn;

        /// <summary>
        /// Creates a new virtual console with the specified dimensions.
        /// </summary>
        /// <param name="width">Width in columns (must be > 0).</param>
        /// <param name="height">Height in rows (must be > 0).</param>
        public VirtualConsole(int width, int height)
        {
            _buffer = new ScreenBuffer(width, height);
            _parser = new AnsiSequenceParser();
            _cursorProcessor = new CursorProcessor();
            _sgrProcessor = new SgrProcessor();
            _eraseProcessor = new EraseProcessor();
        }

        /// <summary>
        /// Writes text to the virtual console, processing any embedded ANSI sequences.
        /// </summary>
        /// <param name="text">The text to write (may contain ANSI escape sequences).</param>
        public void Write(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            foreach (char c in text)
            {
                var result = _parser.Process(c);
                ProcessResult(result);
            }
        }

        /// <summary>
        /// Gets the cell at the specified position.
        /// </summary>
        /// <param name="row">Row (0-based).</param>
        /// <param name="column">Column (0-based).</param>
        /// <returns>The cell at the position.</returns>
        public ScreenCell GetCell(int row, int column)
        {
            return _buffer.GetCell(row, column);
        }

        /// <summary>
        /// Gets a row wrapper for the specified row.
        /// </summary>
        /// <param name="row">Row index (0-based).</param>
        /// <returns>A ScreenRow wrapper.</returns>
        public ScreenRow GetRow(int row)
        {
            return _buffer.GetRow(row);
        }

        /// <summary>
        /// Gets all screen text as a single string without ANSI codes.
        /// Characters are concatenated row by row.
        /// </summary>
        /// <returns>The screen text.</returns>
        public string GetScreenText()
        {
            var sb = new StringBuilder();
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    sb.Append(_buffer.GetCell(row, col).Character);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Gets screen content with line breaks between rows.
        /// </summary>
        /// <returns>The screen content with line breaks.</returns>
        public string GetScreenContent()
        {
            var sb = new StringBuilder();
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    sb.Append(_buffer.GetCell(row, col).Character);
                }
                if (row < Height - 1)
                {
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Clears the screen and resets cursor to home position.
        /// Current style is preserved.
        /// </summary>
        public void Clear()
        {
            _buffer.Clear();
        }

        /// <summary>
        /// Gets direct access to the underlying screen buffer.
        /// Use this for advanced operations like direct cursor manipulation.
        /// </summary>
        /// <returns>The screen buffer.</returns>
        public ScreenBuffer GetScreenBuffer()
        {
            return _buffer;
        }

        private void ProcessResult(ParserResult result)
        {
            switch (result)
            {
                case PrintResult print:
                    _buffer.WriteChar(print.Character);
                    break;

                case ControlResult control:
                    ProcessControlCode(control.Code);
                    break;

                case SequenceResult sequence:
                    ProcessSequence(sequence.Sequence);
                    break;

                case NoActionResult:
                    // Sequence still building, nothing to do
                    break;
            }
        }

        private void ProcessControlCode(ControlCode code)
        {
            switch (code)
            {
                case ControlCode.CarriageReturn:
                    _buffer.MoveCursor(_buffer.CursorRow, 0);
                    break;

                case ControlCode.LineFeed:
                    _buffer.MoveCursorRelative(1, 0);
                    break;

                case ControlCode.Tab:
                    // Move to next tab stop (every 8 columns)
                    int nextTab = ((_buffer.CursorColumn / 8) + 1) * 8;
                    _buffer.MoveCursor(_buffer.CursorRow, nextTab);
                    break;

                case ControlCode.Backspace:
                    _buffer.MoveCursorRelative(0, -1);
                    break;

                case ControlCode.Bell:
                    // Ignore bell in virtual console
                    break;
            }
        }

        private void ProcessSequence(CsiSequence sequence)
        {
            // Try cursor processor first
            if (_cursorProcessor.CanProcess(sequence.FinalByte))
            {
                _cursorProcessor.Process(sequence, _buffer);
                return;
            }

            // Try SGR processor for colors/styles
            if (_sgrProcessor.CanProcess(sequence.FinalByte))
            {
                _sgrProcessor.Process(sequence, _buffer);
                return;
            }

            // Try erase processor for ED (J) / EL (K)
            if (_eraseProcessor.CanProcess(sequence))
            {
                _eraseProcessor.Process(sequence, _buffer);
                return;
            }

            // Unrecognized sequences are silently ignored for now
            // Future: Could throw or log based on configuration
        }
    }
}
