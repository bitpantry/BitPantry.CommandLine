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
        private Prompt _prompt;
        private AutoCompleteController _acCtrl;
        private InputLog _inputLog = new InputLog();

        public InputBuilder(IAnsiConsole console, Prompt prompt, AutoCompleteController acCtrl)
        {
            _console = console;
            _prompt = prompt;
            _acCtrl = acCtrl;
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _prompt.Write(_console);

            try
            {

                var input = await new ConsoleInputInterceptor(_console)
                    .AddHandler(ConsoleKey.Tab, async ctx =>
                    {
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
                        if (!_acCtrl.IsEngaged)
                            return await Task.FromResult(false);

                        _acCtrl.Accept(ctx.InputLine);
                        return await Task.FromResult(true);
                    })
                    .AddHandler(ConsoleKey.UpArrow, async ctx =>
                    {
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
