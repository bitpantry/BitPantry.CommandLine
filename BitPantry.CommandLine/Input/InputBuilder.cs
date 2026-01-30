using BitPantry.CommandLine.AutoComplete;
using Spectre.Console;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Input
{
    /// <summary>
    /// Builds user input with autocomplete support and command history.
    /// Uses AutoCompleteController.HandleKey for most autocomplete operations.
    /// </summary>
    public class InputBuilder : IDisposable
    {
        private readonly IAnsiConsole _console;
        private readonly IPrompt _prompt;
        private readonly AutoCompleteController _acCtrl;
        private readonly InputLog _inputLog = new InputLog();

        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);
            _acCtrl.Reset(_prompt.GetPromptLength());

            try
            {
                var input = await new ConsoleInputInterceptor(_console)
                    // Tab - delegate to autocomplete controller
                    .AddHandler(ConsoleKey.Tab, async ctx =>
                    {
                        return await Task.FromResult(_acCtrl.HandleKey(ConsoleKey.Tab, ctx.InputLine));
                    })
                    // Right Arrow - accept ghost text or move cursor
                    .AddHandler(ConsoleKey.RightArrow, async ctx =>
                    {
                        return await Task.FromResult(_acCtrl.HandleKey(ConsoleKey.RightArrow, ctx.InputLine));
                    })
                    // Left Arrow - close menu if open, then let cursor move
                    .AddHandler(ConsoleKey.LeftArrow, async ctx =>
                    {
                        _acCtrl.HandleKey(ConsoleKey.LeftArrow, ctx.InputLine);
                        // Always let cursor move
                        return await Task.FromResult(false);
                    })
                    // Escape - dismiss autocomplete
                    .AddHandler(ConsoleKey.Escape, async ctx =>
                    {
                        return await Task.FromResult(_acCtrl.HandleKey(ConsoleKey.Escape, ctx.InputLine));
                    })
                    // Space - accept menu selection or insert space
                    .AddHandler(ConsoleKey.Spacebar, async ctx =>
                    {
                        return await Task.FromResult(_acCtrl.HandleKey(ConsoleKey.Spacebar, ctx.InputLine));
                    })
                    // Backspace - special handling: need to modify line first in menu mode
                    .AddHandler(ConsoleKey.Backspace, async ctx =>
                    {
                        if (_acCtrl.Mode == AutoCompleteMode.Menu)
                        {
                            // Perform backspace first, then refilter menu
                            ctx.InputLine.Backspace();
                            _acCtrl.UpdateMenuFilter(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        else
                        {
                            // Dismiss ghost text and perform backspace
                            _acCtrl.HandleKey(ConsoleKey.Backspace, ctx.InputLine);
                        }
                        ctx.InputLine.Backspace();
                        return await Task.FromResult(true);
                    })
                    // Up Arrow - navigate menu or history
                    .AddHandler(ConsoleKey.UpArrow, async ctx =>
                    {
                        if (_acCtrl.HandleKey(ConsoleKey.UpArrow, ctx.InputLine))
                            return await Task.FromResult(true);

                        // History navigation (autocomplete was not in menu mode)
                        if (_inputLog.Previous())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    // Down Arrow - navigate menu or history
                    .AddHandler(ConsoleKey.DownArrow, async ctx =>
                    {
                        if (_acCtrl.HandleKey(ConsoleKey.DownArrow, ctx.InputLine))
                            return await Task.FromResult(true);

                        // History navigation (autocomplete was not in menu mode)
                        if (_inputLog.Next())
                        {
                            ctx.InputLine.HideCursor();
                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);
                            ctx.InputLine.ShowCursor();
                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    // Enter - accept menu selection then submit
                    .AddHandler(ConsoleKey.Enter, async ctx =>
                    {
                        _acCtrl.HandleKey(ConsoleKey.Enter, ctx.InputLine);
                        // Always fall through to submit the line
                        return await Task.FromResult(false);
                    })
                    // After every keypress, update autocomplete
                    .OnKeyPressed(async ctx =>
                    {
                        // Skip menu filter updates for navigation keys that don't change input
                        if (ctx.KeyInfo.Key == ConsoleKey.UpArrow ||
                            ctx.KeyInfo.Key == ConsoleKey.DownArrow ||
                            ctx.KeyInfo.Key == ConsoleKey.Escape)
                        {
                            await Task.CompletedTask;
                            return;
                        }

                        if (_acCtrl.Mode == AutoCompleteMode.Menu)
                        {
                            // Type-to-filter: update menu based on new input
                            _acCtrl.UpdateMenuFilter(ctx.InputLine);
                        }
                        else
                        {
                            // Update ghost text
                            _acCtrl.Update(ctx.InputLine);
                        }
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
