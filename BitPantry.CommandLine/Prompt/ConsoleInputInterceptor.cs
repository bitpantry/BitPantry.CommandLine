using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Prompt
{
    class ConsoleInputInterceptor
    {
        private Dictionary<ConsoleKey, Func<ReadKeyHandlerContext, Task<bool>>> _handlerDict = new Dictionary<ConsoleKey, Func<ReadKeyHandlerContext, Task<bool>>>();
        private Func<ReadKeyHandlerContext, Task<bool>> _defaultHandler;

        private IAnsiConsole _console;
        private ConsoleLineMirror _inputLine;

        public ConsoleInputInterceptor(IAnsiConsole console) : this(console, string.Empty, 0) { }

        public ConsoleInputInterceptor(IAnsiConsole console, string initialInput, int initialPosition)
        {
            _console = console;
            _inputLine = new ConsoleLineMirror(console, initialInput, initialPosition);
        }

        public async Task<string> ReadLine(CancellationToken token = default)
        {
            var submitLine = false;

            do
            {
                var keyInfo = await _console.Input.ReadKeyAsync(true, token);
                if (keyInfo.HasValue)
                    submitLine = await HandleKeyPress(keyInfo.Value);
                else if (token.IsCancellationRequested) // keyInfo is null and cancellation requested - expected behavior, break loop
                    return null;
                else // KeyInfo is null unexpectedly - throw exception for upstream handling
                    throw new Exception("The process has not been canceled, but the KeyInfo read from the console is unexpectedly null. Either the input stream was interrupted or an attempt to read from a headless environment was made.");

            } while (!submitLine);

            _console.WriteLine();
            return _inputLine.Buffer;
        }

        private async Task<bool> HandleKeyPress(ConsoleKeyInfo keyInfo)
        {
            var isHandled = false;

            // added handlers

            if (_handlerDict.ContainsKey(keyInfo.Key))
                isHandled = await _handlerDict[keyInfo.Key].Invoke(new ReadKeyHandlerContext(_inputLine, keyInfo));

            if (!isHandled && _defaultHandler != null)
                isHandled = await _defaultHandler.Invoke(new ReadKeyHandlerContext(_inputLine, keyInfo));

            // default handling

            if (!isHandled)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        return true;
                    case ConsoleKey.LeftArrow:
                        _inputLine.MovePositionLeft();
                        break;
                    case ConsoleKey.RightArrow:
                        _inputLine.MovePositionRight();
                        break;
                    case ConsoleKey.Backspace:
                        _inputLine.Backspace();
                        break;
                    case ConsoleKey.Delete:
                        _inputLine.Delete();
                        break;
                    default:
                        if (!char.IsControl(keyInfo.KeyChar))
                            _inputLine.Write(keyInfo.KeyChar);
                        break;
                }
            }

            return false;
        }

        public ConsoleInputInterceptor AddHandler(ConsoleKey key, Func<ReadKeyHandlerContext, Task<bool>> handler)
        {
            _handlerDict.Add(key, handler);
            return this;
        }

        public ConsoleInputInterceptor AddDefaultHandler(Func<ReadKeyHandlerContext, Task<bool>> handler)
        {
            _defaultHandler = handler;
            return this;
        }

    }
}
