using BitPantry.VirtualConsole.Testing;
using BitPantry.VirtualConsole.AnsiParser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BitPantry.CommandLine.Tests.Infrastructure.Http;
using BitPantry.CommandLine.Tests.Infrastructure.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.Infrastructure
{
    /// <summary>
    /// Test environment for integration testing CommandLine applications.
    /// Provides a virtual console, optional test server, and CLI application.
    /// Automatically starts the CLI input loop in the background.
    /// </summary>
    public class TestEnvironment : IDisposable
    {
        private readonly TestServer _server;
        private readonly TestRemoteFileSystem _remoteFileSystem;
        private readonly CancellationTokenSource _pumpCts;
        private readonly Task _pumpTask;
        private readonly IDisposable _keyProcessedSubscription;

        /// <summary>
        /// Unique identifier for this test environment.
        /// </summary>
        public string EnvironmentId { get; }

        /// <summary>
        /// The test server. Throws InvalidOperationException if server was not configured.
        /// </summary>
        public TestServer Server => _server ?? throw new InvalidOperationException(
            "Server is not configured. Call opt.ConfigureServer() in the TestEnvironment constructor to enable the server.");

        /// <summary>
        /// The command line application.
        /// </summary>
        public CommandLineApplication Cli { get; }

        /// <summary>
        /// The virtual console adapter with input simulation capabilities.
        /// </summary>
        public VirtualConsoleAnsiAdapter Console { get; }

        /// <summary>
        /// Convenience accessor for console input simulation.
        /// </summary>
        public TestConsoleInput Input => Console.Input;

        /// <summary>
        /// Convenience accessor for keyboard simulation.
        /// </summary>
        public IKeyboardSimulator Keyboard => Console.Keyboard;

        /// <summary>
        /// File system utilities for creating and managing remote test files.
        /// Throws InvalidOperationException if server was not configured.
        /// </summary>
        public TestRemoteFileSystem RemoteFileSystem => _remoteFileSystem ?? throw new InvalidOperationException(
            "RemoteFileSystem is not available because the server is not configured. Call opt.ConfigureServer() in the TestEnvironment constructor to enable the server.");

        /// <summary>
        /// Captures all unrecognized ANSI sequences for debugging.
        /// </summary>
        public ConcurrentBag<CsiSequence> UnrecognizedSequences { get; } = new ConcurrentBag<CsiSequence>();

        /// <summary>
        /// Whether the server is configured and available.
        /// </summary>
        public bool HasServer => _server != null;

        /// <summary>
        /// Creates a new test environment with the specified options.
        /// </summary>
        /// <param name="configure">Optional action to configure the environment options</param>
        public TestEnvironment(Action<TestEnvironmentOptions>? configure = null)
        {
            var opts = new TestEnvironmentOptions();
            configure?.Invoke(opts);

            EnvironmentId = opts.EnvironmentId;

            // Create virtual console
            var virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(opts.ConsoleWidth, opts.ConsoleHeight);
            virtualConsole.StrictMode = opts.StrictAnsiMode;
            virtualConsole.UnrecognizedSequenceReceived += (sender, seq) => UnrecognizedSequences.Add(seq);
            Console = new VirtualConsoleAnsiAdapter(virtualConsole);

            // Build CLI application
            var cliBuilder = new CommandLineApplicationBuilder()
                .UsingConsole(Console);

            // Apply user's client command configuration
            opts.CommandConfiguration?.Invoke(cliBuilder.CommandRegistryBuilder);

            // Apply user's client autocomplete configuration
            opts.AutoCompleteConfiguration?.Invoke(cliBuilder.AutoCompleteHandlerRegistryBuilder);

            // Apply user's client services configuration
            opts.ServicesConfiguration?.Invoke(cliBuilder.Services);

            // Configure server if requested
            if (opts.ServerOptions != null)
            {
                var serverOpts = opts.ServerOptions;

                // Initialize remote file system
                _remoteFileSystem = new TestRemoteFileSystem(serverOpts);

                // Create and start test server
                var webHostBuilder = new WebHostBuilder()
                    .UseStartup(_ => new TestStartup(serverOpts));

                _server = new TestServer(webHostBuilder);
                _server.PreserveExecutionContext = true;

                // Configure SignalR client (auto-registers Connect/Disconnect/Upload/Download commands)
                cliBuilder.ConfigureSignalRClient(opt =>
                {
                    opt.HttpClientFactory = new TestHttpClientFactory(_server);
                    opt.HttpMessageHandlerFactory = new TestHttpMessageHandlerFactory(_server);
                    opt.TokenRefreshMonitorInterval = serverOpts.TokenRefreshMonitorInterval;
                    opt.TokenRefreshThreshold = serverOpts.TokenRefreshThreshold;
                    // Use Long Polling to avoid WebSocket timeout delay in TestServer
                    // TestServer only supports HTTP, so WebSockets cause a ~4 second timeout before fallback
                    opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
                });
            }

            // Configure logging
            var testLoggerOutput = new TestLoggerOutput();
            cliBuilder.Services.AddSingleton(testLoggerOutput);

            cliBuilder.Services.AddLogging(loggingBuilder =>
            {
                loggingBuilder.ClearProviders();
                loggingBuilder.AddFilter((provider, category, logLevel) =>
                {
                    if (category?.StartsWith("BitPantry") == true)
                        return logLevel >= LogLevel.Debug;
                    return false;
                });
                loggingBuilder.AddConsole();

                var logSvcs = loggingBuilder.Services.BuildServiceProvider();
                loggingBuilder.AddProvider(new TestLoggerProvider(logSvcs.GetRequiredService<ILoggerFactory>(), testLoggerOutput));
            });

            Cli = cliBuilder.Build();

            // Wire up key processed notifications from the input loop to the test console input
            // This enables async keyboard simulation methods (e.g., TypeTextAsync) to wait for processing
            var notifier = Cli.Services.GetRequiredService<IKeyProcessedObservable>();
            _keyProcessedSubscription = notifier.Subscribe(() => Console.Input.NotifyKeyProcessed());

            // Start the CLI input loop in the background
            _pumpCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _pumpTask = Task.Run(async () =>
            {
                try
                {
                    await Cli.RunInteractive(_pumpCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected - run was cancelled
                }
            });
        }

        /// <summary>
        /// Gets client-side log entries for the specified type.
        /// </summary>
        public List<TestLoggerEntry> GetClientLogs<T>()
            => Cli.Services.GetService<TestLoggerOutput>()?.GetLogMessages<T>().ToList() ?? new List<TestLoggerEntry>();

        /// <summary>
        /// Gets server-side log entries for the specified type.
        /// Throws InvalidOperationException if server was not configured.
        /// </summary>
        public List<TestLoggerEntry> GetServerLogs<T>()
            => Server.Services.GetService<TestLoggerOutput>()?.GetLogMessages<T>().ToList() ?? new List<TestLoggerEntry>();

        /// <summary>
        /// Gets ALL server-side log entries from all categories.
        /// Useful for debugging when you're not sure which category logged the error.
        /// </summary>
        public List<TestLoggerEntry> GetAllServerLogs()
            => Server.Services.GetService<TestLoggerOutput>()?.GetAllLogMessages().ToList() ?? new List<TestLoggerEntry>();

        /// <summary>
        /// Gets all server-side ERROR level log entries from all categories.
        /// </summary>
        public List<TestLoggerEntry> GetAllServerErrors()
            => Server.Services.GetService<TestLoggerOutput>()?.GetAllErrors().ToList() ?? new List<TestLoggerEntry>();

        /// <summary>
        /// Connects to the server and waits for the prompt to be ready.
        /// This is the standard method for all integration tests that need server connectivity.
        /// </summary>
        /// <param name="hubPath">The hub path for SignalR connection.</param>
        /// <param name="tokenRequestPath">The token request endpoint path.</param>
        /// <param name="apiKey">The API key for authentication.</param>
        /// <param name="timeoutMs">Maximum time to wait for prompt to appear (default 2000ms).</param>
        public async Task ConnectToServerAsync(
            string hubPath = "/cli",
            string tokenRequestPath = "/cli-auth/token-request",
            string apiKey = "key1",
            int timeoutMs = 2000)
        {
            var hubUri = $"{Server.BaseAddress.AbsoluteUri.TrimEnd('/')}/{hubPath.TrimStart('/')}";
            await Keyboard.SubmitAsync($"server connect -u {hubUri} -k {apiKey} -e {tokenRequestPath}");
            await WaitForInputReadyAsync(timeoutMs);
        }

        /// <summary>
        /// Submits a command through the REPL keyboard, waits for it to complete,
        /// and returns the result. This is the standard way for integration tests
        /// to execute commands against the running REPL.
        /// </summary>
        /// <param name="command">The command string to execute.</param>
        /// <param name="timeoutMs">Maximum time to wait for the command to complete (default 5000ms).</param>
        /// <returns>The result of the command execution.</returns>
        public async Task<RunResult> RunCommandAsync(string command, int timeoutMs = 5000)
        {
            await Keyboard.SubmitAsync(command);
            await WaitForInputReadyAsync(timeoutMs);
            return Cli.LastRunResult;
        }

        /// <summary>
        /// Waits for the input loop to be ready (prompt visible).
        /// Uses console text detection to determine when the prompt has been rendered.
        /// </summary>
        /// <param name="timeoutMs">Maximum time to wait for prompt (default 2000ms).</param>
        private async Task WaitForInputReadyAsync(int timeoutMs = 2000)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                var cursorRow = Console.VirtualConsole.CursorRow;
                var lineText = Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();
                if (lineText.EndsWith("> ") || lineText.EndsWith(">"))
                {
                    // Small delay to ensure input loop is fully ready
                    await Task.Delay(50);
                    return;
                }
                await Task.Delay(25);
            }
            // Don't throw - let test assertions provide better error messages
        }

        public void Dispose()
        {
            // Stop the input pump
            _pumpCts.Cancel();
            try
            {
                _pumpTask.Wait(500);
            }
            catch (Exception)
            {
                // Ignore timeout or cancellation exceptions
            }
            _pumpCts.Dispose();

            // Dispose resources
            _keyProcessedSubscription?.Dispose();
            _server?.Dispose();
            Cli.Dispose();
            _remoteFileSystem?.Dispose();
        }

        #region Static Factory Methods

        /// <summary>
        /// Creates a test environment with a server configured with default settings.
        /// Shortcut for: new TestEnvironment(opt => opt.ConfigureServer(svr => { }))
        /// </summary>
        public static TestEnvironment WithServer()
            => new TestEnvironment(opt => opt.ConfigureServer(_ => { }));

        /// <summary>
        /// Creates a test environment with a server, allowing customization of server options.
        /// Shortcut for: new TestEnvironment(opt => opt.ConfigureServer(configureServer))
        /// </summary>
        public static TestEnvironment WithServer(Action<TestServerOptions> configureServer)
            => new TestEnvironment(opt => opt.ConfigureServer(configureServer));

        #endregion
    }
}
