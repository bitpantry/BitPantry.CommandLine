using BitPantry.CommandLine.Tests.VirtualConsole;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Environment
{
    public class TestEnvironment : IDisposable
    {
        private static int _instanceCounter = 0;
        
        public TestServer Server { get; }
        public CommandLineApplication Cli { get; }
        public VirtualAnsiConsole Console { get; }
        
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

            Console = new VirtualAnsiConsole();

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
            Console.Dispose();
            
            // Unregister the API key to clean up
            TestApiKeyStore.UnregisterApiKey(ApiKey);
        }
    }
}
