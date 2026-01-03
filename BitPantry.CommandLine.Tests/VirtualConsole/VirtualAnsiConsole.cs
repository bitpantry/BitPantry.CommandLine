using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    public class VirtualAnsiConsole : IAnsiConsole
    {
        private readonly IAnsiConsole _console;
        private readonly StringWriter _writer;
        private IAnsiConsoleCursor _cursor;
        private readonly List<string> _buffer;
        private (int Column, int Line)? _savedCursorPosition;

        /// <inheritdoc/>
        public Profile Profile => _console.Profile;

        /// <inheritdoc/>
        public IExclusivityMode ExclusivityMode => _console.ExclusivityMode;

        /// <summary>
        /// Gets the console input.
        /// </summary>
        public VirtualConsoleInput Input { get; }

        /// <inheritdoc/>
        public RenderPipeline Pipeline => _console.Pipeline;

        /// <inheritdoc/>
        public IAnsiConsoleCursor Cursor => _cursor ?? _console.Cursor;

        /// <inheritdoc/>
        IAnsiConsoleInput IAnsiConsole.Input => Input;

        /// <summary>
        /// Gets the console output.
        /// </summary>
        public string Buffer => _writer.ToString();

        /// <summary>
        /// Gets the console output lines.
        /// </summary>
        public IReadOnlyList<string> Lines => Buffer.NormalizeLineEndings().TrimEnd('\n').Split(new char[] { '\n' });

        /// <summary>
        /// Gets or sets a value indicating whether or not VT/ANSI sequences
        /// should be emitted to the console.
        /// </summary>
        public bool EmitAnsiSequences { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestAnsiConsole"/> class.
        /// </summary>
        public VirtualAnsiConsole()
        {
            _writer = new StringWriter();
            _cursor = new VirtualCursor();
            _buffer = new List<string>();

            Input = new VirtualConsoleInput();
            EmitAnsiSequences = false;

            _console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.Yes,
                ColorSystem = (ColorSystemSupport)ColorSystem.TrueColor,
                Out = new AnsiConsoleOutput(_writer),
                Interactive = InteractionSupport.No,
                ExclusivityMode = new VirtualExclusivityMode(),
                Enrichment = new ProfileEnrichment
                {
                    UseDefaultEnrichers = false,
                },
            });

            _console.Profile.Width = 80;
            _console.Profile.Height = 24;
            _console.Profile.Capabilities.Ansi = true;
            _console.Profile.Capabilities.Unicode = true;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _writer.Dispose();
        }

        /// <inheritdoc/>
        public void Clear(bool home)
        {
            _console.Clear(home);
            _buffer.Clear();
            _cursor?.SetPosition(0, 0);
        }

        /// <inheritdoc/>
        public void Write(IRenderable renderable)
        {
            if (EmitAnsiSequences)
            {
                _console.Write(renderable);
            }
            else
            {
                foreach (var segment in renderable.GetSegments(this))
                {
                    if (segment.IsControlCode)
                    {
                        // Handle control codes that we need to simulate (cursor save/restore, clear)
                        // These don't get written to buffer but affect virtual console state
                        HandleControlCode(segment.Text);
                        continue;
                    }

                    WriteAtCursor(segment.Text);
                }
            }
        }

        /// <summary>
        /// Handles ANSI control codes by simulating their effects on the virtual console.
        /// </summary>
        private void HandleControlCode(string code)
        {
            // Process control codes in order as they appear
            var remaining = code;
            while (remaining.Length > 0)
            {
                // Carriage Return - \r
                if (remaining.StartsWith("\r"))
                {
                    var pos = GetCursorPosition();
                    SetCursorPosition(0, pos.Line);
                    remaining = remaining.Substring(1);
                    continue;
                }
                
                // DEC Save Cursor (DECSC) - \x1b7
                if (remaining.StartsWith("\x1b7"))
                {
                    _savedCursorPosition = GetCursorPosition();
                    remaining = remaining.Substring(2);
                    continue;
                }
                
                // DEC Restore Cursor (DECRC) - \x1b8
                if (remaining.StartsWith("\x1b8"))
                {
                    if (_savedCursorPosition.HasValue)
                    {
                        SetCursorPosition(_savedCursorPosition.Value.Column, _savedCursorPosition.Value.Line);
                    }
                    remaining = remaining.Substring(2);
                    continue;
                }
                
                // CSI sequences - \x1b[...
                if (remaining.StartsWith("\x1b["))
                {
                    // Find the end of the CSI sequence (letter character)
                    var match = System.Text.RegularExpressions.Regex.Match(remaining, @"^\x1b\[(\d*)([A-Za-z])");
                    if (match.Success)
                    {
                        var param = match.Groups[1].Value;
                        var command = match.Groups[2].Value;
                        var count = string.IsNullOrEmpty(param) ? 1 : int.Parse(param);
                        
                        switch (command)
                        {
                            case "A": // CUU - Cursor Up
                                var posUp = GetCursorPosition();
                                SetCursorPosition(posUp.Column, Math.Max(0, posUp.Line - count));
                                break;
                            case "B": // CUD - Cursor Down
                                var posDown = GetCursorPosition();
                                SetCursorPosition(posDown.Column, posDown.Line + count);
                                break;
                            case "C": // CUF - Cursor Forward (Right)
                                var posRight = GetCursorPosition();
                                SetCursorPosition(posRight.Column + count, posRight.Line);
                                break;
                            case "D": // CUB - Cursor Back (Left)
                                var posLeft = GetCursorPosition();
                                SetCursorPosition(Math.Max(0, posLeft.Column - count), posLeft.Line);
                                break;
                            case "J": // ED - Erase in Display
                                if (count == 0 || string.IsNullOrEmpty(param))
                                {
                                    ClearToEndOfScreen();
                                }
                                break;
                            case "K": // EL - Erase in Line
                                // For now, just clear to end of line
                                ClearToEndOfLine();
                                break;
                        }
                        remaining = remaining.Substring(match.Length);
                        continue;
                    }
                }
                
                // Unknown sequence or regular character, skip one character
                remaining = remaining.Substring(1);
            }
        }
        
        /// <summary>
        /// Clears from cursor to end of current line.
        /// </summary>
        private void ClearToEndOfLine()
        {
            var pos = GetCursorPosition();
            if (pos.Line < _buffer.Count)
            {
                var line = _buffer[pos.Line];
                if (pos.Column < line.Length)
                {
                    _buffer[pos.Line] = line.Substring(0, pos.Column);
                }
            }
        }

        private void WriteAtCursor(string text)
        {
            // Handle DEC Save Cursor (DECSC) - \x1b7
            if (text.Contains("\x1b7"))
            {
                var parts = text.Split(new[] { "\x1b7" }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]))
                    {
                        WriteAtCursor(parts[i]);
                    }
                    if (i < parts.Length - 1)
                    {
                        // Save current cursor position
                        _savedCursorPosition = GetCursorPosition();
                    }
                }
                return;
            }

            // Handle DEC Restore Cursor (DECRC) - \x1b8
            if (text.Contains("\x1b8"))
            {
                var parts = text.Split(new[] { "\x1b8" }, StringSplitOptions.None);
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]))
                    {
                        WriteAtCursor(parts[i]);
                    }
                    if (i < parts.Length - 1)
                    {
                        // Restore saved cursor position
                        if (_savedCursorPosition.HasValue)
                        {
                            SetCursorPosition(_savedCursorPosition.Value.Column, _savedCursorPosition.Value.Line);
                        }
                    }
                }
                return;
            }

            // Handle ANSI escape sequence for clear to end of screen
            if (text.Contains("\x1b[J") || text.Contains("\x1b[0J"))
            {
                // Split on the escape sequence and process
                var clearPattern = text.Contains("\x1b[J") ? "\x1b[J" : "\x1b[0J";
                var parts = text.Split(new[] { clearPattern }, StringSplitOptions.None);
                
                for (int i = 0; i < parts.Length; i++)
                {
                    if (!string.IsNullOrEmpty(parts[i]))
                    {
                        WriteAtCursor(parts[i]); // Recursively handle non-escape parts
                    }
                    
                    if (i < parts.Length - 1)
                    {
                        // Clear from cursor to end of screen
                        ClearToEndOfScreen();
                    }
                }
                return;
            }
            
            // Handle carriage returns, newlines properly
            var lineParts = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            
            for (int i = 0; i < lineParts.Length; i++)
            {
                var part = lineParts[i];
                if (!string.IsNullOrEmpty(part))
                {
                    WriteTextAtCursor(part);
                }
                
                // If there are more parts, it means there was a newline or carriage return
                if (i < lineParts.Length - 1)
                {
                    // Determine what character caused the split
                    // For \r alone, just move cursor to column 0 (same line)
                    // For \n or \r\n, move to next line column 0
                    var (_, currentLine) = GetCursorPosition();
                    
                    // Check the original text to see what type of line ending
                    // Since \r\n is handled first in Split, lineParts after \r\n or \n move to next line
                    // For now, we treat all splits as moving to column 0, next line for \r\n/\n
                    // and column 0 same line for \r only
                    
                    // Calculate position in original text for this split
                    var textSoFar = string.Join("", lineParts.Take(i + 1));
                    var posInOriginal = textSoFar.Length;
                    
                    // Look at what follows in original text
                    // If it's \r\n, move to next line
                    // If it's \n, move to next line  
                    // If it's \r (but not followed by \n), move to column 0 same line
                    if (posInOriginal < text.Length)
                    {
                        if (text[posInOriginal] == '\r' && posInOriginal + 1 < text.Length && text[posInOriginal + 1] == '\n')
                        {
                            // \r\n - move to next line, column 0
                            SetCursorPosition(0, currentLine + 1);
                        }
                        else if (text[posInOriginal] == '\n')
                        {
                            // \n - move to next line, column 0
                            SetCursorPosition(0, currentLine + 1);
                        }
                        else if (text[posInOriginal] == '\r')
                        {
                            // \r alone - carriage return, stay on same line, move to column 0
                            SetCursorPosition(0, currentLine);
                        }
                    }
                    else
                    {
                        // Default: move to next line
                        SetCursorPosition(0, currentLine + 1);
                    }
                }
            }
        }

        private void WriteTextAtCursor(string text)
        {
            var terminalWidth = Profile.Width;
            var (column, line) = GetCursorPosition();
            
            // Simulate line wrapping like a real terminal
            foreach (char c in text)
            {
                if (column >= terminalWidth)
                {
                    // Wrap to next line
                    column = 0;
                    line++;
                }
                
                EnsureBufferSize(line + 1);
                
                var currentLine = _buffer[line];
                if (column >= currentLine.Length)
                {
                    currentLine = currentLine.PadRight(column);
                }
                
                // Replace character at column position
                var newLine = currentLine.Remove(column, Math.Min(1, currentLine.Length - column))
                                         .Insert(column, c.ToString());
                _buffer[line] = newLine;
                column++;
            }
            
            _writer.GetStringBuilder().Clear();
            _writer.Write(string.Join(Environment.NewLine, _buffer));

            // Update cursor position
            SetCursorPosition(column, line);
        }

        private void EnsureBufferSize(int size)
        {
            while (_buffer.Count < size)
            {
                _buffer.Add(string.Empty);
            }
        }

        /// <summary>
        /// Clears from the current cursor position to the end of the screen.
        /// This simulates the ANSI escape sequence ESC[J (or ESC[0J).
        /// </summary>
        private void ClearToEndOfScreen()
        {
            var (column, line) = GetCursorPosition();
            
            // Clear from current column to end of current line
            if (line < _buffer.Count)
            {
                var currentLine = _buffer[line];
                if (column < currentLine.Length)
                {
                    _buffer[line] = currentLine.Substring(0, column);
                }
            }
            
            // Remove all lines after the current line
            while (_buffer.Count > line + 1)
            {
                _buffer.RemoveAt(_buffer.Count - 1);
            }
            
            // Update the writer buffer
            _writer.GetStringBuilder().Clear();
            _writer.Write(string.Join(Environment.NewLine, _buffer));
        }

        public (int Column, int Line) GetCursorPosition()
        {
            return (((VirtualCursor)_cursor)?.Column ?? 0, ((VirtualCursor)_cursor)?.Line ?? 0);
        }

        public void SetCursorPosition(int column, int line)
        {
            _cursor?.SetPosition(column, line);
        }

        internal void SetCursor(IAnsiConsoleCursor cursor)
        {
            _cursor = cursor;
        }

        internal sealed class VirtualCursor : IAnsiConsoleCursor
        {
            private int _column;
            private int _line;

            public int Column => _column;
            public int Line => _line;

            public void Move(CursorDirection direction, int steps)
            {
                switch (direction)
                {
                    case CursorDirection.Up:
                        _line = Math.Max(0, _line - steps);
                        break;
                    case CursorDirection.Down:
                        _line += steps;
                        break;
                    case CursorDirection.Left:
                        _column = Math.Max(0, _column - steps);
                        break;
                    case CursorDirection.Right:
                        _column += steps;
                        break;
                }
            }

            public void SetPosition(int column, int line)
            {
                _column = column;
                _line = line;
            }

            public void Show(bool show)
            {
                // No-op
            }
        }

        internal sealed class VirtualExclusivityMode : IExclusivityMode
        {
            public T Run<T>(Func<T> func)
            {
                return func();
            }

            public async Task<T> RunAsync<T>(Func<Task<T>> func)
            {
                return await func().ConfigureAwait(false);
            }
        }
    }
}
