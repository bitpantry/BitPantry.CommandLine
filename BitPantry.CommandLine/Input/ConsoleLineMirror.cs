using Spectre.Console;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.Input
{
    public class ConsoleLineMirror
    {
        private IAnsiConsole _console;
        private StringBuilder _mirrorBuffer;

        public bool Overwrite { get; set; } = false;

        public string Buffer => _mirrorBuffer.ToString();

        public int BufferPosition { get; private set; }

        public ConsoleLineMirror(IAnsiConsole console) : this(console, string.Empty, 0) { }

        public ConsoleLineMirror(IAnsiConsole console, string initialInput, int initialPosition)
        {
            _console = console;
            _mirrorBuffer = new StringBuilder(initialInput);
            BufferPosition = initialPosition;
        }

        public void HideCursor()
        {
            _console.Cursor.Hide();
        }

        public void ShowCursor()
        {
            _console.Cursor.Show();
        }

        public void Write(char ch)
        {
            Write(ch.ToString());
        }

        public void Backspace()
        {
            if (BufferPosition <= 0)
                return;

            _mirrorBuffer.Remove(BufferPosition - 1, 1);
            BufferPosition--;

            var newStr = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";

            _console.Cursor.Hide();
            try
            {
                _console.Cursor.MoveLeft();
                _console.Write(newStr);
                _console.Cursor.MoveLeft(newStr.Length);
            }
            finally
            {
                _console.Cursor.Show();
            }
        }

        public void MovePositionLeft(int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (BufferPosition <= 0)
                    return;

                BufferPosition--;
                _console.Cursor.MoveLeft();
            }
        }

        public void MovePositionRight(int steps = 1)
        {
            for (int i = 0; i < steps; i++)
            {
                if (BufferPosition >= _mirrorBuffer.Length)
                    return;

                BufferPosition++;
                _console.Cursor.MoveRight();
            }
        }

        public void MoveToPosition(int position)
        {
            if (BufferPosition < position)
                MovePositionRight(position - BufferPosition);

            if (position < BufferPosition)
                MovePositionLeft(BufferPosition - position);
        }

        public void Clear(int startPosition = 0)
        {
            var padCount = _mirrorBuffer.Length - startPosition;

            if (padCount > 0)
            {
                _console.Cursor.Hide();
                try
                {
                    ClearCore(startPosition, padCount);
                }
                finally
                {
                    _console.Cursor.Show();
                }
            }
        }

        /// <summary>
        /// Core implementation of Clear that doesn't hide/show cursor.
        /// Used by RenderWithStyles which manages cursor visibility itself.
        /// </summary>
        private void ClearCore(int startPosition, int padCount)
        {
            MoveToPosition(startPosition);

            for (int i = 0; i < padCount; i++)
            {
                _mirrorBuffer.Remove(startPosition, 1);
                _console.Write(" ");
            }

            _console.Cursor.MoveLeft(padCount);
        }

        public void Write(string str)
        {
            if (Overwrite)
            {
                foreach (var ch in str)
                {
                    if (BufferPosition == _mirrorBuffer.Length)
                        _mirrorBuffer.Append(ch);
                    else
                        _mirrorBuffer[BufferPosition] = ch;

                    BufferPosition++;
                }

                _console.Write(str);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, str);
                BufferPosition += str.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                if (after.Length > 0)
                {
                    // Insert mode with content after cursor - hide cursor during rewrite
                    _console.Cursor.Hide();
                    try
                    {
                        _console.Write(str);
                        _console.Write(after);
                        _console.Cursor.MoveLeft(after.Length);
                    }
                    finally
                    {
                        _console.Cursor.Show();
                    }
                }
                else
                {
                    // Appending at end - no cursor move needed
                    _console.Write(str);
                }
            }
        }

        public void Markup(string str)
        {
            var unstr = str.Unmarkup();

            if (Overwrite)
            {
                foreach (var ch in unstr)
                {
                    if (BufferPosition == _mirrorBuffer.Length)
                        _mirrorBuffer.Append(ch);
                    else
                        _mirrorBuffer[BufferPosition] = ch;

                    BufferPosition++;
                }

                _console.Markup(str);
            }
            else
            {
                _mirrorBuffer.Insert(BufferPosition, unstr);
                BufferPosition += unstr.Length;

                var after = _mirrorBuffer.ToString().Substring(BufferPosition);

                if (after.Length > 0)
                {
                    // Insert mode with content after cursor - hide cursor during rewrite
                    _console.Cursor.Hide();
                    try
                    {
                        _console.Markup(str);
                        _console.Write(after);
                        _console.Cursor.MoveLeft(after.Length);
                    }
                    finally
                    {
                        _console.Cursor.Show();
                    }
                }
                else
                {
                    // Appending at end - no cursor move needed
                    _console.Markup(str);
                }
            }
        }

        internal void Delete()
        {
            if (BufferPosition >= _mirrorBuffer.Length)
                return;

            _mirrorBuffer.Remove(BufferPosition, 1);

            var str = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";

            _console.Cursor.Hide();
            try
            {
                _console.Write(str);
                _console.Cursor.MoveLeft(str.Length);
            }
            finally
            {
                _console.Cursor.Show();
            }
        }

        /// <summary>
        /// Renders styled segments to the console, replacing current line content.
        /// </summary>
        /// <param name="segments">The styled segments to render.</param>
        /// <param name="cursorPosition">The desired cursor position after rendering.</param>
        public void RenderWithStyles(IReadOnlyList<StyledSegment> segments, int cursorPosition)
        {
            _console.Cursor.Hide();
            try
            {
                // Clear existing content (use internal method to avoid nested hide/show)
                var padCount = _mirrorBuffer.Length;
                if (padCount > 0)
                {
                    ClearCore(0, padCount);
                }

                // Build new buffer content
                _mirrorBuffer.Clear();
                foreach (var segment in segments)
                {
                    _mirrorBuffer.Append(segment.Text);
                }

                // Render each segment with its style
                foreach (var segment in segments)
                {
                    _console.Write(new Text(segment.Text, segment.Style));
                }

                // Update buffer position and move cursor
                BufferPosition = _mirrorBuffer.Length;
                if (cursorPosition < BufferPosition)
                {
                    var moveLeft = BufferPosition - cursorPosition;
                    _console.Cursor.MoveLeft(moveLeft);
                    BufferPosition = cursorPosition;
                }
            }
            finally
            {
                _console.Cursor.Show();
            }
        }
    }
}
