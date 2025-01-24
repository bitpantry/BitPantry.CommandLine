using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Prompt
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

            _console.Cursor.MoveLeft();
            _console.Write(newStr);
            _console.Cursor.MoveLeft(newStr.Length);
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
                MoveToPosition(startPosition);

                for (int i = 0; i < padCount; i++)
                {
                    _mirrorBuffer.Remove(startPosition, 1);
                    _console.Write(" ");
                }

                _console.Cursor.MoveLeft(padCount);
            }

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

                _console.Write(str);
                _console.Write(after);
                _console.Cursor.MoveLeft(after.Length);
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

                _console.Markup(str);
                _console.Write(after);
                _console.Cursor.MoveLeft(after.Length);
            }
        }

        internal void Delete()
        {
            if (BufferPosition >= _mirrorBuffer.Length)
                return;

            _mirrorBuffer.Remove(BufferPosition, 1);

            var str = $"{_mirrorBuffer.ToString().Substring(BufferPosition)} ";

            _console.Write(str);
            _console.Cursor.MoveLeft(str.Length);
        }
    }
}
