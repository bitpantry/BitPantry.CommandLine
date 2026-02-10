using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Input;
using Spectre.Console;
using System;
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

        /// <summary>
        /// The result of the most recently executed command in the REPL loop.
        /// Updated after each command completes in <see cref="RunInteractive"/>.
        /// </summary>
        public RunResult LastRunResult { get; private set; }

        private ILogger<CommandLineApplication> _logger;
        private IAnsiConsole _console;
        private CommandLineApplicationCore _core;
        private InputBuilder _prompt;
        private IAutoConnectHandler _autoConnectHandler;
        private IServerProxy _serverProxy;

        internal CommandLineApplication(IServiceProvider serviceProvider, IAnsiConsole console, CommandLineApplicationCore core, InputBuilder prompt)
        {
            Services = serviceProvider;

            _logger = Services.GetRequiredService<ILogger<CommandLineApplication>>();
            _console = console;
            _core = core;
            _prompt = prompt;
            _autoConnectHandler = Services.GetService<IAutoConnectHandler>();
            _serverProxy = Services.GetRequiredService<IServerProxy>();
        }

        /// <summary>
        /// Runs the application in interactive REPL mode with autocomplete, syntax highlighting,
        /// and command history. The input loop runs until cancellation is requested.
        /// </summary>
        /// <param name="token">Cancellation token to stop the REPL loop.</param>
        public async Task RunInteractive(CancellationToken token = default)
        {
            do
            {
                try
                {
                    var input = await _prompt.GetInput(token);

                    if (!token.IsCancellationRequested) // make sure read was not canceled
                    {
                        LastRunResult = await _core.Run(input, token);
                        if (LastRunResult.RunError != null)
                            HandleError(LastRunResult.RunError);
                        else if (LastRunResult.Result != null)
                            _console.WriteLine(LastRunResult.Result.ToString());
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

        /// <summary>
        /// Executes a single command and returns the result. Intended for non-interactive,
        /// one-shot CLI usage (e.g., <c>mycli server download file.txt dest/</c>).
        /// Auto-connect is enabled for the duration of the call. Any active server connection
        /// is disconnected before returning.
        /// </summary>
        /// <param name="input">The command string to execute.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The result of command execution.</returns>
        public async Task<RunResult> RunOnce(string input, CancellationToken token = default)
        {
            // Enable auto-connect for single-command execution
            if (_autoConnectHandler != null)
                _autoConnectHandler.AutoConnectEnabled = true;

            try
            {
                var result = await _core.Run(input, token);
                if(result.RunError != null)
                    HandleError(result.RunError);
                return result;
            }
            finally
            {
                // Disable auto-connect after execution
                if (_autoConnectHandler != null)
                    _autoConnectHandler.AutoConnectEnabled = false;

                // Always disconnect — single-command mode owns the full connection lifecycle
                if (_serverProxy.ConnectionState == ServerProxyConnectionState.Connected)
                    await _serverProxy.Disconnect(token);
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
