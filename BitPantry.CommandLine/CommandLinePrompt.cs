using BitPantry.CommandLine.AutoComplete;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Text;

namespace BitPantry.CommandLine
{
    public class CommandLinePrompt
    {
        private AutoComplete.AutoCompleteOptionsBuilder _currentAutoComplete = null;
        private StringBuilder _input = new StringBuilder();
        
        private IAnsiConsole _console;
        private CommandRegistry _registry;
        private IServiceProvider _serviceProvider;
        private IConsoleService _consoleService;

        public string PromptText { get; private set; } = "$ ";

        public CommandLinePrompt(IAnsiConsole console, CommandRegistry registry, IServiceProvider serviceProvider)
        {
            _console = console;
            _registry = registry;
            _serviceProvider = serviceProvider;
            _consoleService = serviceProvider.GetRequiredService<IConsoleService>();
        }

        public string GetInput()
        {
            _console.Write(PromptText);

            _input = new StringBuilder();

            while (true)
            {
                var keyInfo = _console.Input.ReadKey(intercept: true);
                if (keyInfo.Value.Key == ConsoleKey.Enter)
                {
                    _console.WriteLine();
                    break;
                }
                else if (keyInfo.Value.Key == ConsoleKey.Backspace)
                {
                    if (_input.Length > 0)
                    {
                        _input.Length--;
                        _console.Write("\b \b");
                    }
                }
                else if(keyInfo.Value.Key == ConsoleKey.Tab)
                {
                    AutoComplete();
                }
                else
                {
                    _input.Append(keyInfo.Value.KeyChar);
                    _console.Write(keyInfo.Value.KeyChar.ToString());
                }
            }

            // Intercept the line here
            // You can add custom logic to handle the line

            return _input.ToString();
        }
        private void AutoComplete()
        {
            if(_currentAutoComplete == null)
            {
                _currentAutoComplete = new AutoComplete.AutoCompleteOptionsBuilder(_registry, _serviceProvider, _input.ToString(), _consoleService.GetCursorPosition().Left - PromptText.Length);
            }
            else
            {

            }
        }
    }
}
