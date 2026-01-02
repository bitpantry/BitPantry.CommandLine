using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.VirtualConsole
{
    public class VirtualConsoleInput : IAnsiConsoleInput
    {
        private readonly Queue<ConsoleKeyInfo> _input;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestConsoleInput"/> class.
        /// </summary>
        public VirtualConsoleInput()
        {
            _input = new Queue<ConsoleKeyInfo>();
        }

        /// <summary>
        /// Pushes the specified text to the input queue.
        /// </summary>
        /// <param name="input">The input string.</param>
        public void PushText(string input)
        {
            if (input is null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            foreach (var character in input)
            {
                PushCharacter(character);
            }
        }

        /// <summary>
        /// Pushes the specified text followed by 'Enter' to the input queue.
        /// </summary>
        /// <param name="input">The input.</param>
        public void PushTextWithEnter(string input)
        {
            PushText(input);
            PushKey(ConsoleKey.Enter);
        }

        /// <summary>
        /// Pushes the specified character to the input queue.
        /// </summary>
        /// <param name="input">The input.</param>
        public void PushCharacter(char input)
        {
            var control = char.IsUpper(input);
            _input.Enqueue(new ConsoleKeyInfo(input, (ConsoleKey)input, false, false, control));
        }

        /// <summary>
        /// Pushes the specified key to the input queue.
        /// </summary>
        /// <param name="input">The input.</param>
        public void PushKey(ConsoleKey input)
        {
            // For control keys (arrows, escape, etc.), use '\0' as KeyChar to prevent
            // the character from being written if the key is not handled
            var keyChar = IsControlKey(input) ? '\0' : (char)input;
            _input.Enqueue(new ConsoleKeyInfo(keyChar, input, false, false, false));
        }
        
        /// <summary>
        /// Determines if a console key is a control key (non-printable).
        /// </summary>
        private static bool IsControlKey(ConsoleKey key)
        {
            return key switch
            {
                ConsoleKey.UpArrow => true,
                ConsoleKey.DownArrow => true,
                ConsoleKey.LeftArrow => true,
                ConsoleKey.RightArrow => true,
                ConsoleKey.Escape => true,
                ConsoleKey.Tab => true,
                ConsoleKey.Enter => true,
                ConsoleKey.Backspace => true,
                ConsoleKey.Delete => true,
                ConsoleKey.Home => true,
                ConsoleKey.End => true,
                ConsoleKey.PageUp => true,
                ConsoleKey.PageDown => true,
                ConsoleKey.Insert => true,
                _ => false
            };
        }

        /// <summary>
        /// Pushes the specified key to the input queue.
        /// </summary>
        /// <param name="consoleKeyInfo">The input.</param>
        public void PushKey(ConsoleKeyInfo consoleKeyInfo)
        {
            _input.Enqueue(consoleKeyInfo);
        }

        /// <inheritdoc/>
        public bool IsKeyAvailable()
        {
            return _input.Count > 0;
        }

        /// <inheritdoc/>
        public ConsoleKeyInfo? ReadKey(bool intercept)
        {
            if (_input.Count == 0)
            {
                throw new InvalidOperationException("No input available.");
            }

            return _input.Dequeue();
        }

        /// <inheritdoc/>
        public Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadKey(intercept));
        }
    }
}
