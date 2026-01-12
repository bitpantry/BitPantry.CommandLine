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
        /// When true, throws an exception for any unrecognized ANSI escape sequence.
        /// Useful for testing to identify missing sequence handlers.
        /// Default is false (unrecognized sequences are silently ignored).
        /// </summary>
        public bool StrictMode { get; set; } = false;

        /// <summary>
        /// When true, logs all processed sequences for debugging.
        /// </summary>
        public bool DebugLogging { get; set; } = false;

        /// <summary>
        /// Captured debug log entries when DebugLogging is enabled.
        /// </summary>
        public System.Collections.Generic.List<string> DebugLog { get; } = new System.Collections.Generic.List<string>();

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
                    if (DebugLogging)
                    {
                        DebugLog.Add($"CTRL: {control.Code} @ Row={_buffer.CursorRow}, Col={_buffer.CursorColumn}");
                    }
                    ProcessControlCode(control.Code);
                    break;

                case SequenceResult sequence:
                    if (DebugLogging)
                    {
                        DebugLog.Add($"SEQ: {sequence.Sequence} @ Row={_buffer.CursorRow}, Col={_buffer.CursorColumn} -> processing...");
                    }
                    ProcessSequence(sequence.Sequence);
                    if (DebugLogging)
                    {
                        DebugLog.Add($"     After: Row={_buffer.CursorRow}, Col={_buffer.CursorColumn}");
                    }
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
            // Handle private mode sequences (CSI ? Ps h/l) first
            if (sequence.IsPrivate)
            {
                ProcessPrivateModeSequence(sequence);
                return;
            }

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

            // Log unrecognized sequence for debugging
            UnrecognizedSequenceReceived?.Invoke(this, sequence);
            
            // In strict mode, throw an exception to help identify missing handlers
            if (StrictMode)
            {
                throw new NotSupportedException(
                    $"Unrecognized ANSI escape sequence: {sequence}. " +
                    $"Private={sequence.IsPrivate}, FinalByte='{sequence.FinalByte}' (0x{(int)sequence.FinalByte:X2}), " +
                    $"Parameters=[{string.Join(", ", sequence.Parameters)}]. " +
                    $"Enable a handler for this sequence or disable StrictMode.");
            }
        }

        /// <summary>
        /// Processes DEC private mode sequences (CSI ? Ps h/l).
        /// These control terminal features like cursor visibility, line wrap, etc.
        /// </summary>
        private void ProcessPrivateModeSequence(CsiSequence sequence)
        {
            // Private mode SET (h) and RESET (l) sequences
            // Common sequences:
            //   CSI ? 25 h - DECTCEM: Show cursor
            //   CSI ? 25 l - DECTCEM: Hide cursor
            //   CSI ? 7 h  - DECAWM: Enable auto-wrap
            //   CSI ? 7 l  - DECAWM: Disable auto-wrap
            //   CSI ? 1049 h - Enable alternate screen buffer
            //   CSI ? 1049 l - Disable alternate screen buffer

            if (sequence.FinalByte != 'h' && sequence.FinalByte != 'l')
            {
                // Unknown private mode command
                UnrecognizedSequenceReceived?.Invoke(this, sequence);
                if (StrictMode)
                {
                    throw new NotSupportedException(
                        $"Unrecognized private mode sequence: {sequence}. " +
                        $"FinalByte='{sequence.FinalByte}' (0x{(int)sequence.FinalByte:X2}), " +
                        $"Parameters=[{string.Join(", ", sequence.Parameters)}].");
                }
                return;
            }

            int mode = sequence.GetParameter(0, 0);
            bool enable = sequence.FinalByte == 'h';

            switch (mode)
            {
                case 25:
                    // DECTCEM - Cursor visibility
                    // In a virtual console, we don't render a visible cursor, so this is a no-op
                    // but we handle it explicitly to not throw in strict mode
                    _buffer.CursorVisible = enable;
                    break;

                case 7:
                    // DECAWM - Auto-wrap mode
                    // When enabled, writing past the right margin wraps to next line
                    // For now, we'll track it but most virtual consoles already wrap
                    _buffer.AutoWrapMode = enable;
                    break;

                case 1049:
                    // Alternate screen buffer
                    // Used by applications like vim, less, etc.
                    // For testing purposes, we can ignore this or implement later
                    break;

                default:
                    // Unknown private mode - log but don't throw
                    // Many terminals emit various private modes we don't need to handle
                    UnrecognizedSequenceReceived?.Invoke(this, sequence);
                    if (StrictMode)
                    {
                        throw new NotSupportedException(
                            $"Unsupported private mode {mode}: {sequence}. " +
                            $"FinalByte='{sequence.FinalByte}' (enable={enable}).");
                    }
                    break;
            }
        }

        /// <summary>
        /// Event raised when an unrecognized ANSI sequence is received.
        /// Useful for debugging and identifying missing sequence handlers.
        /// </summary>
        public event EventHandler<CsiSequence>? UnrecognizedSequenceReceived;
    }
}
