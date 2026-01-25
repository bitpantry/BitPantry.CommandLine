using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Input;
using Spectre.Console;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine
{
    public class CommandLineApplication : IDisposable
    {
        public IServiceProvider Services { get; }

        private ILogger<CommandLineApplication> _logger;
        private IAnsiConsole _console;
        private CommandLineApplicationCore _core;
        private InputBuilder _prompt;

        public CommandLineApplication(IServiceProvider serviceProvider, IAnsiConsole console, CommandLineApplicationCore core, InputBuilder prompt)
        {
            Services = serviceProvider;

            _logger = Services.GetRequiredService<ILogger<CommandLineApplication>>();
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
                        {
                            await ExecuteScript(input, token);
                        }
                        else
                        {
                            var result = await Run(input, token);
                            if(result.RunError == null && result.Result != null)
                                _console.WriteLine(result.Result.ToString());
                        }
                    }
                    else
                    {
                        return; // exit gracefully when canceled
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    // Cancellation requested - exit gracefully without logging
                    return;
                }
                catch (Exception ex)
                {
                    HandleError(ex);
                }
            } while (true);
        }

        public async Task<RunResult> Run(string input, CancellationToken token = default)
        {
            var result = await _core.Run(input, token);
            if(result.RunError != null)
                HandleError(result.RunError);
            return result;
        }

        private async Task ExecuteScript(string input, CancellationToken token = default)
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
                    HandleError(resp.RunError);

                    _console.WriteLine();
                    _console.WriteLine();
                    _console.WriteLine("[red]Script execution cannot continue.[/red]");
                    _console.WriteLine();
                }
            }
        }

        private void HandleError(Exception ex)
        {
            if (ex == null)
                return; // if the command failed remotely, the error will have been handled on the server and will be null

            _logger.LogError(ex, "An unhandled exception occured");

            _console.WriteLine();

            if (ex is ServerException msgEx)
                _console.MarkupLineInterpolated($"[white]Message Correlation Id:[/] [red]{msgEx.CorrelationId}[/]");

            _console.WriteException(ex, ExceptionFormats.ShortenEverything);
            _console.WriteLine();
        }

        public void Dispose()
        {
            _core.Dispose();
            _prompt.Dispose();
        }
    }
}
