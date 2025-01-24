using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Prompt;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public class CommandLineApplication : IDisposable
    {
        private IAnsiConsole _console;
        private CommandLineApplicationCore _core;
        private CommandLinePrompt _prompt;

        public CommandLineApplication(IAnsiConsole console, CommandLineApplicationCore core, CommandLinePrompt prompt)
        {
            _console = console;
            _core = core;
            _prompt = prompt;
        }

        public async Task Run(CancellationToken token = default)
        {
            do
            {
                try
                {
                    var input = await _prompt.GetInput(token);

                    if (!token.IsCancellationRequested) // make sure read was not canceled
                    {
                        if (File.Exists(input))
                            await ExecuteScript(input, token);
                        else
                            await Run(input, token);
                    }
                    else
                    {
                        return; // exit gracefully when canceled
                    }
                }
                catch (Exception ex)
                {
                    _console.WriteException(ex);
                }
            } while (true);
        }

        public async Task<RunResult> Run(string input, CancellationToken token = default)
            => await _core.Run(input, token);

        private async Task ExecuteScript(string input, CancellationToken token)
        {
            var lines = File.ReadAllLines(input);
            foreach (var line in lines)
            {
                System.Console.WriteLine(line);
                var resp = await _core.Run(line, token);

                if (token.IsCancellationRequested)
                    return;

                if (resp.ResultCode != RunResultCode.Success)
                {
                    _console.WriteLine();
                    _console.WriteLine();
                    _console.WriteLine("[red]Script execution cannot continue.[/red]");
                    _console.WriteLine();
                }
            }
        }

        public void Dispose()
        {
            _core.Dispose();
            _prompt.Dispose();
        }
    }
}
