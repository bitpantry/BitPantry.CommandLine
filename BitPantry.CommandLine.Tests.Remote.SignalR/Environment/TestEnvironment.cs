using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestEnvironment : IDisposable
    {
        private static int _instanceCounter = 0;
        
        public TestServer Server { get; }
        public CommandLineApplication Cli { get; }
        public IAnsiConsole Console { get; }
        public StringWriter Output { get; }
        
        /// <summary>
        /// Gets the console output as a single string.
        /// </summary>
        public string Buffer => Output.ToString();
        
        /// <summary>
        /// Gets the console output as a list of lines.
        /// </summary>
        public List<string> Lines => Output.ToString()
            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .ToList();
        
        /// <summary>
        /// Unique API key for this test environment instance.
        /// Used to isolate parallel tests from each other.
        /// </summary>
        public string ApiKey { get; }
        
        /// <summary>
        /// Unique client ID for this test environment instance.
        /// </summary>
        public string ClientId { get; }

        public TestEnvironment(Action<TestEnvironmentOptions> optsAction = null)
        {
            // Generate unique API key and client ID for this test instance
            var instanceId = Interlocked.Increment(ref _instanceCounter);
            ApiKey = $"test-key-{instanceId}-{Guid.NewGuid():N}";
            ClientId = $"test-client-{instanceId}";
            
            // Register the unique API key -> client ID mapping
            TestApiKeyStore.RegisterApiKey(ApiKey, ClientId);
            
            var envOpts = new TestEnvironmentOptions();
            optsAction?.Invoke(envOpts);

            var webHostBuilder = new WebHostBuilder()
                .UseStartup(_ => new TestStartup(envOpts));

            Output = new StringWriter();
            Console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(Output)
            });

            Server = new TestServer(webHostBuilder);
            Server.PreserveExecutionContext = true;

            var cliBuilder = new CommandLineApplicationBuilder()
                .ConfigureSignalRClient(opt =>
                {
                    opt.HttpClientFactory = new TestHttpClientFactory(Server);
                    opt.HttpMessageHandlerFactory = new TestHttpMessageHandlerFactory(Server);
                    opt.TokenRefreshMonitorInterval = envOpts.TokenRefreshMonitorInterval;
                    opt.TokenRefreshThreshold = envOpts.TokenRefreshThreshold;
                })
                .UsingConsole(Console);

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

        public List<TestLoggerEntry> GetClientLogs<T>()
            => Cli.Services.GetService<TestLoggerOutput>().GetLogMessages<T>().ToList();

        public List<TestLoggerEntry> GetServerLogs<T>()
            => Server.Services.GetService<TestLoggerOutput>().GetLogMessages<T>().ToList();

        public void Dispose()
        {
            Server.Dispose();
            Cli.Dispose();
            Output.Dispose();
            
            // Unregister the API key to clean up
            TestApiKeyStore.UnregisterApiKey(ApiKey);
        }
    }
}
