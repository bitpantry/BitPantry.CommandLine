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
    internal class InputBuilder : IDisposable
    {
        private readonly IAnsiConsole _console;
        private readonly IPrompt _prompt;
        private readonly AutoCompleteController _acCtrl;
        private readonly KeyProcessedNotifier _notifier;
        private readonly InputLog _inputLog = new InputLog();

        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl, KeyProcessedNotifier notifier = null)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
            _notifier = notifier;
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);
            _acCtrl.Reset(_prompt.GetPromptLength());

            try
            {
                var input = await new ConsoleInputInterceptor(_console, _notifier)
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
                    // Space - accept menu selection or insert space (in quoted context, adds to filter)
                    .AddHandler(ConsoleKey.Spacebar, async ctx =>
                    {
                        var handled = _acCtrl.HandleKey(ConsoleKey.Spacebar, ctx.InputLine);
                        if (!handled)
                        {
                            // If autocomplete didn't handle it (e.g., in quoted menu context),
                            // add the space character and update the menu filter (UX-026b)
                            ctx.InputLine.Write(" ");
                            if (_acCtrl.Mode == AutoCompleteMode.Menu)
                            {
                                await _acCtrl.UpdateMenuFilterAsync(ctx.InputLine);
                            }
                            else
                            {
                                await _acCtrl.UpdateAsync(ctx.InputLine);
                            }
                            return true;
                        }
                        return handled;
                    })
                    // Backspace - special handling: need to modify line first in menu mode
                    .AddHandler(ConsoleKey.Backspace, async ctx =>
                    {
                        if (_acCtrl.Mode == AutoCompleteMode.Menu)
                        {
                            // Perform backspace first, then refilter menu
                            ctx.InputLine.Backspace();
                            await _acCtrl.UpdateMenuFilterAsync(ctx.InputLine);
                            return true;
                        }
                        else
                        {
                            // Dismiss ghost text and perform backspace
                            _acCtrl.HandleKey(ConsoleKey.Backspace, ctx.InputLine);
                        }
                        ctx.InputLine.Backspace();
                        return true;
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
                    // Enter - accept menu selection OR submit line
                    .AddHandler(ConsoleKey.Enter, async ctx =>
                    {
                        if (_acCtrl.Mode == AutoCompleteMode.Menu)
                        {
                            // Accept selection but don't submit
                            _acCtrl.HandleKey(ConsoleKey.Enter, ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        
                        // Dismiss ghost text before submitting the line
                        if (_acCtrl.Mode == AutoCompleteMode.GhostText)
                        {
                            _acCtrl.HandleKey(ConsoleKey.Enter, ctx.InputLine);
                        }
                        
                        // Fall through to submit the line
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
                            return;
                        }

                        if (_acCtrl.Mode == AutoCompleteMode.Menu)
                        {
                            // Type-to-filter: update menu based on new input
                            await _acCtrl.UpdateMenuFilterAsync(ctx.InputLine);
                        }
                        else
                        {
                            // Update ghost text
                            await _acCtrl.UpdateAsync(ctx.InputLine);
                        }
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
