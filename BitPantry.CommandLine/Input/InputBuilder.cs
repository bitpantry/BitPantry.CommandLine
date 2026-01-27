using BitPantry.CommandLine.AutoComplete;
using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Input
{
    public class InputBuilder : IDisposable
    {
        private IAnsiConsole _console;
        private IPrompt _prompt;
        private AutoCompleteController _acCtrl;
        private InputLog _inputLog = new InputLog();

        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);
            _acCtrl.Reset();

            try
            {

                var input = await new ConsoleInputInterceptor(_console)
                    // Tab - accept single option, do nothing for multiple (future: menu)
                    .AddHandler(ConsoleKey.Tab, async ctx =>
                    {
                        if (!_acCtrl.IsActive)
                            return await Task.FromResult(false); // Pass through - nothing to do

                        var optionCount = _acCtrl.AvailableOptionCount;
                        if (optionCount == 1)
                        {
                            _acCtrl.Accept(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        else if (optionCount > 1)
                        {
                            // TODO: Open menu for multiple options
                            // For now, do nothing - leave ghost text showing
                            return await Task.FromResult(true); // Handled (consumed Tab, but did nothing)
                        }

                        return await Task.FromResult(false);
                    })
                    // Right Arrow - accept ghost text, else move cursor
                    .AddHandler(ConsoleKey.RightArrow, async ctx =>
                    {
                        if (_acCtrl.IsActive)
                        {
                            _acCtrl.Accept(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        return await Task.FromResult(false); // Let default handler move cursor right
                    })
                    // Escape - suppress ghost text for current element
                    .AddHandler(ConsoleKey.Escape, async ctx =>
                    {
                        if (!_acCtrl.IsActive)
                            return await Task.FromResult(false);

                        _acCtrl.Suppress(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    // Backspace - clear ghost text first, then perform backspace
                    .AddHandler(ConsoleKey.Backspace, async ctx =>
                    {
                        _acCtrl.Dismiss(ctx.InputLine);
                        ctx.InputLine.Backspace();
                        return await Task.FromResult(true);
                    })
                    // Up Arrow - dismiss ghost text, then navigate history
                    .AddHandler(ConsoleKey.UpArrow, async ctx =>
                    {
                        if (_acCtrl.IsActive)
                            _acCtrl.Dismiss(ctx.InputLine);

                        // History navigation
                        if (_inputLog.Previous())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    // Down Arrow - dismiss ghost text, then navigate history
                    .AddHandler(ConsoleKey.DownArrow, async ctx =>
                    {
                        if (_acCtrl.IsActive)
                            _acCtrl.Dismiss(ctx.InputLine);

                        // History navigation
                        if (_inputLog.Next())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    // Enter - dismiss ghost text (don't accept), then let default handler submit
                    .AddHandler(ConsoleKey.Enter, async ctx =>
                    {
                        if (_acCtrl.IsActive)
                            _acCtrl.Dismiss(ctx.InputLine);

                        // Return false to let default Enter handling submit the line
                        return await Task.FromResult(false);
                    })
                    // After every keypress, update ghost text
                    .OnKeyPressed(async ctx =>
                    {
                        _acCtrl.Update(ctx.InputLine);
                        await Task.CompletedTask;
                    })
                    .ReadLine(token);

                _inputLog.Add(input);

                return input;
            }
            catch
            {
                _console.WriteLine();
                throw;
            }
        }

        public void Dispose()
        {
            // AutoCompleteController no longer requires disposal
        }
    }
}
