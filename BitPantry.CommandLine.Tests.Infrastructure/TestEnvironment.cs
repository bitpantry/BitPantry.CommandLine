#nullable enable

using BitPantry.VirtualConsole.Testing;
using BitPantry.VirtualConsole.AnsiParser;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using BitPantry.CommandLine.Remote.SignalR.Client;
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
    /// </summary>
    public class TestEnvironment : IDisposable
    {
        private readonly TestServer? _server;
        private readonly TestRemoteFileSystem? _remoteFileSystem;

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
                    if (category.StartsWith("BitPantry"))
                        return logLevel >= LogLevel.Debug;
                    return false;
                });
                loggingBuilder.AddConsole();

                var logSvcs = loggingBuilder.Services.BuildServiceProvider();
                loggingBuilder.AddProvider(new TestLoggerProvider(logSvcs.GetRequiredService<ILoggerFactory>(), testLoggerOutput));
            });

            Cli = cliBuilder.Build();
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
        /// Starts the test environment, running the CLI input loop in the background.
        /// The returned EnvironmentRun manages the background task and ensures proper cleanup
        /// of all resources (CLI, server if configured) when disposed.
        /// </summary>
        /// <param name="timeout">Maximum time the run can execute before auto-cancellation. Default is 5 seconds.</param>
        /// <returns>An EnvironmentRun that manages the running environment and handles cleanup on dispose.</returns>
        public EnvironmentRun Start(TimeSpan? timeout = null)
        {
            return new EnvironmentRun(this, timeout ?? TimeSpan.FromSeconds(5));
        }

        public void Dispose()
        {
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
