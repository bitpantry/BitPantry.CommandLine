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
        public TestServer Server { get; }
        public CommandLineApplication Cli { get; }
        public VirtualAnsiConsole Console { get; }

        public TestEnvironment(Action<TestEnvironmentOptions> optsAction = null)
        {
            var envOpts = new TestEnvironmentOptions();
            optsAction?.Invoke(envOpts);

            var webHostBuilder = new WebHostBuilder()
                .UseStartup(_ => new TestStartup(envOpts));

            Console = new VirtualAnsiConsole();

            Server = new TestServer(webHostBuilder);

            var cliBuilder = new CommandLineApplicationBuilder()
                .ConfigureSignalRClient(opt =>
                {
                    opt.HttpClientFactory = new TestHttpClientFactory(Server);
                    opt.HttpMessageHandlerFactory = new TestHttpMessageHandlerFactory(Server);
                    opt.TokenRefreshMonitorInterval = envOpts.TokenRefreshMonitorInterval;
                })
                .UsingConsole(Console);

            cliBuilder.Services.AddSingleton(typeof(ILogger<>), typeof(TestLogger<>));

            Cli = cliBuilder.Build();
        }

        public void Dispose()
        {
            Server.Dispose();
            Cli.Dispose();
            Console.Dispose();
        }
    }
}
