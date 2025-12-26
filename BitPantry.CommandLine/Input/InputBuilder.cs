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
        private InputLog _inputLog;

        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl, InputLog inputLog)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
            _inputLog = inputLog ?? new InputLog();
        }

        // Constructor for backwards compatibility
        public InputBuilder(IAnsiConsole console, IPrompt prompt, AutoCompleteController acCtrl)
            : this(console, prompt, acCtrl, new InputLog())
        {
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);

            try
            {

                var input = await new ConsoleInputInterceptor(_console)
                    .AddHandler(ConsoleKey.Tab, async ctx =>
                    {
                        // Clear ghost when opening menu
                        _acCtrl.ClearGhost();
                        
                        if (_acCtrl.IsEngaged)
                        {
                            if (ctx.KeyInfo.Modifiers == ConsoleModifiers.Shift)
                                _acCtrl.PreviousOption(ctx.InputLine);
                            else
                                _acCtrl.NextOption(ctx.InputLine);
                        }
                        else
                        {
                            await _acCtrl.Begin(ctx.InputLine);
                        }

                        return true;
                    })
                    .AddHandler(ConsoleKey.Escape, async ctx =>
                    {
                        if (!_acCtrl.IsEngaged)
                            return await Task.FromResult(false);

                        _acCtrl.Cancel(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    .AddHandler(ConsoleKey.Enter, async ctx =>
                    {
                        // Clear ghost on Enter
                        _acCtrl.ClearGhost();
                        
                        if (!_acCtrl.IsEngaged)
                            return await Task.FromResult(false);

                        _acCtrl.Accept(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    .AddHandler(ConsoleKey.RightArrow, async ctx =>
                    {
                        // Right Arrow accepts ghost text (T039)
                        if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                        {
                            _acCtrl.AcceptGhost(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.End, async ctx =>
                    {
                        // End key also accepts ghost text when at end of line (T039)
                        if (_acCtrl.HasGhostText && ctx.InputLine.BufferPosition == ctx.InputLine.Buffer.Length)
                        {
                            _acCtrl.AcceptGhost(ctx.InputLine);
                            return await Task.FromResult(true);
                        }
                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.UpArrow, async ctx =>
                    {
                        // Clear ghost when navigating history
                        _acCtrl.ClearGhost();
                        if (_inputLog.Previous())
                        {
                            ctx.InputLine.HideCursor();

                            if (_acCtrl.IsEngaged)
                                _acCtrl.End(ctx.InputLine);

                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);

                            ctx.InputLine.ShowCursor();

                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    .AddHandler(ConsoleKey.DownArrow, async ctx =>
                    {
                        // Clear ghost when navigating history
                        _acCtrl.ClearGhost();
                        
                        if (_inputLog.Next())
                        {
                            ctx.InputLine.HideCursor();

                            if (_acCtrl.IsEngaged)
                                _acCtrl.End(ctx.InputLine);

                            ctx.InputLine.ShowCursor();

                            _inputLog.WriteLineAtCurrentIndex(ctx.InputLine);

                            return await Task.FromResult(true);
                        }

                        return await Task.FromResult(false);
                    })
                    .AddDefaultHandler(async ctx =>
                    {
                        if (_acCtrl.IsEngaged)
                            _acCtrl.End(ctx.InputLine);
                        
                        // Update ghost text after each keystroke (T038)
                        // Let the character be processed first, then update ghost
                        _ = Task.Run(async () =>
                        {
                            // Small delay to let the character be added to the buffer
                            await Task.Delay(10);
                            await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
                        });
                        
                        return await Task.FromResult(false);
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
            _acCtrl.Dispose();
        }
    }
}
