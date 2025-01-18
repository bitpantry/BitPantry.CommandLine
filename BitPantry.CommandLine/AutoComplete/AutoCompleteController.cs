using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    public class AutoCompleteController
    {
        private IAnsiConsole _console;
        private CommandRegistry _registry;
        private IConsoleService _consoleService;
        private IServiceProvider _serviceProvider;

        private AutoCompleteOptionsBuilder _activeBuilder;
        private int _activeBuilderLeftMin;
        private int _activeBuilderLineNumber = 0;

        public bool IsActive => _activeBuilder != null;

        public AutoCompleteController(IAnsiConsole console, IConsoleService consoleService, CommandRegistry registry, IServiceProvider serviceProvider)
        {
            _console = console;
            _registry = registry;
            _consoleService = consoleService;
            _serviceProvider = serviceProvider;
        }

        public void BeginAutoComplete(string input, int promptStringLength)
        {
            _activeBuilderLeftMin = promptStringLength;
            _activeBuilderLineNumber = _consoleService.GetCursorPosition().Top;

            _activeBuilder = new AutoCompleteOptionsBuilder(_registry, _serviceProvider, input, _consoleService.GetCursorPosition().Left - promptStringLength);
            if (_activeBuilder.CurrentResult.Option != null)
                PreviewCurrentOption();
        }

        public bool HandleKey(ConsoleKeyInfo key)
        {
            if (key.Key == ConsoleKey.Tab && key.Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                _activeBuilder.PreviousOption();
                PreviewCurrentOption();
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                _activeBuilder.NextOption();
                PreviewCurrentOption();
            }
            else if (key.Key == ConsoleKey.Enter)
            {
                _activeBuilder = null;
            }
            else if(key.Key == ConsoleKey.Escape)
            {
                var originalInput = _activeBuilder.ParsedInput.ToString();

                _console.Cursor.SetPosition(_activeBuilderLineNumber, _activeBuilderLeftMin);
                _console.Write(originalInput);

                var endPadding = originalInput.Length - (_activeBuilder.CurrentResult.AutoCompleteStartPosition + _activeBuilder.CurrentResult.AutoCompletedInputString.Length + _activeBuilderLeftMin);

                if (endPadding > 0)
                    _console.Write(string.Empty.PadLeft(endPadding, ' '));

                _activeBuilder = null;
            }
            else
            {
                _activeBuilder = null;
            }

            return _activeBuilder != null;
        }

        private void PreviewCurrentOption()
        {
            _console.Cursor.SetPosition(_activeBuilder.CurrentResult.AutoCompleteStartPosition + _activeBuilderLeftMin - 1, _activeBuilderLineNumber); // move to start of auto complete
            _console.Write(_activeBuilder.CurrentResult.AutoCompletedInputString); // write the line
            _console.Write(string.Empty.PadLeft(_activeBuilder.CurrentResult.NumPositionsToClearAfterInput, ' ')); // clear out older input line
            _console.Cursor.SetPosition(_activeBuilder.CurrentResult.AutoCompletedCursorPosition + _activeBuilderLeftMin - 1, _activeBuilderLineNumber); // move cursor to end of auto complete position
        }
    }
}
