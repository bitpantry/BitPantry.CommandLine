using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    public class VirtualAnsiConsole : IAnsiConsole
    {
        private readonly IAnsiConsole _console;
        private readonly StringWriter _writer;
        private IAnsiConsoleCursor _cursor;
        private readonly List<string> _buffer;

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
                        continue;
                    }

                    WriteAtCursor(segment.Text);
                }
            }
        }

        private void WriteAtCursor(string text)
        {
            var (column, line) = GetCursorPosition();
            EnsureBufferSize(line + 1);

            var currentLine = _buffer[line];
            if (column >= currentLine.Length)
            {
                currentLine = currentLine.PadRight(column);
            }

            var newLine = currentLine.Remove(column, Math.Min(text.Length, currentLine.Length - column))
                                     .Insert(column, text);

            _buffer[line] = newLine;
            _writer.GetStringBuilder().Clear();
            _writer.Write(string.Join(Environment.NewLine, _buffer));

            // Update cursor position
            SetCursorPosition(column + text.Length, line);
        }

        private void EnsureBufferSize(int size)
        {
            while (_buffer.Count < size)
            {
                _buffer.Add(string.Empty);
            }
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
