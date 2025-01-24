using BitPantry.CommandLine.AutoComplete;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Prompt
{
    public class CommandLinePrompt : IDisposable
    {
        private IAnsiConsole _console;
        private AutoCompleteController _acCtrl;

        public string PromptText { get; private set; } = "$ ";

        public CommandLinePrompt(IAnsiConsole console, AutoCompleteController acCtrl)
        {
            _console = console;
            _acCtrl = acCtrl;
        }

        public async Task<string> GetInput(CancellationToken token = default)
        {
            _console.Markup(PromptText);

            return await new ConsoleInputInterceptor(_console)
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
                .AddDefaultHandler(async ctx =>
                {
                    if (_acCtrl.IsEngaged)
                        _acCtrl.End(ctx.InputLine);
                    return await Task.FromResult(false);
                })
                .ReadLine(token);
        }

        public void Dispose()
        {
            _acCtrl.Dispose();
        }
    }
}
