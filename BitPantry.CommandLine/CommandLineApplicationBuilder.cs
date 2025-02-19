using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Client;

namespace BitPantry.CommandLine
{
    public class CommandLineApplicationBuilder : CommandRegistryApplicationBuilder<CommandLineApplicationBuilder>
    {
        public IServiceCollection Services { get; }
        public IAnsiConsole Console { get; private set; } = AnsiConsole.Create(new AnsiConsoleSettings());
        public IConsoleService ConsoleService { get; private set; } = new SystemConsoleService();

        public CommandLineApplicationBuilder()
        {
            Services = new ServiceCollection();

            // Allows default null logger resolution

            Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);  
            Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));

            // the server proxy is disabled by default

            Services.AddSingleton<IServerProxy, NoopServerProxy>();
        }

        /// <summary>
        /// Configures the application to use the given IAnsiConsole and IConsoleService implementations
        /// </summary>
        /// <param name="console">The implementation to use</param>
        /// <param name="consoleSvc">The implementation of IConsoleService to use</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder UsingConsole(IAnsiConsole console, IConsoleService consoleSvc = null)
        {
            Console = console;
            
            if(consoleSvc != null)
                ConsoleService = consoleSvc;

            return this;
        }

        /// <summary>
        /// Builds and returns the CommandLineApplication
        /// </summary>
        /// <returns>The CommandLineApplication</returns>
        public CommandLineApplication Build()
        {
            Services.AddSingleton(Console);

            // register services

            Services.AddSingleton(CommandRegistry);
            Services.AddSingleton(ConsoleService);

            // core commands

            CommandRegistry.RegisterCommand<ListCommandsCommand>();

            // build components

            CommandRegistry.ConfigureServices(Services);
            var svcProvider = Services.BuildServiceProvider();

            var serverProxy = svcProvider.GetService<IServerProxy>();

            var core = new CommandLineApplicationCore(
                Console,
                CommandRegistry, 
                new CommandActivator(svcProvider),
                serverProxy);

            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(CommandRegistry, serverProxy, svcProvider));

            var prompt = new CommandLinePrompt(Console, acCtrl);

            // build the command line application

            return new CommandLineApplication(svcProvider, Console, core, prompt);
        }
    }
}
