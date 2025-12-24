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
using BitPantry.CommandLine.Help;
using System.Text;
using System.Collections.Generic;
using System;
using System.IO.Abstractions;

namespace BitPantry.CommandLine
{
    public class CommandLineApplicationBuilder : CommandRegistryApplicationBuilder<CommandLineApplicationBuilder>
    {
        public IServiceCollection Services { get; }
        public IAnsiConsole Console { get; private set; }
        public IConsoleService ConsoleService { get; private set; } = new SystemConsoleService();
        public List<Action<IServiceProvider>> BuildActions { get; } = new List<Action<IServiceProvider>>();

        public CommandLineApplicationBuilder()
        {
            System.Console.OutputEncoding = Encoding.UTF8;
            Console = AnsiConsole.Create(new AnsiConsoleSettings());

            Services = new ServiceCollection();

            // the server proxy is disabled by default

            Services.AddFileSystem();
            Services.AddSingleton<IServerProxy, NoopServerProxy>();

            // core commands

            CommandRegistry.RegisterCommand<ListCommandsCommand>();
        }

        /// <summary>
        /// Configures the application to use the given IAnsiConsole
        /// </summary>
        /// <param name="console">The implementation to use</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder UsingConsole(IAnsiConsole console)
            => UsingConsole(console, ConsoleService);

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
        /// Configures the application to use the given IFileSystem implementation
        /// </summary>
        /// <param name="fileSystem">The implementation to use</param>
        /// <returns>The <see cref="CommandLineApplicationBuilder"/></returns>
        public CommandLineApplicationBuilder UsingFileSystem(IFileSystem fileSystem)
        {
            Services.AddSingleton(fileSystem);
            return this;
        }

        /// <summary>
        /// Configures the application to use a custom IHelpFormatter implementation.
        /// The formatter will be resolved from the service container.
        /// </summary>
        /// <typeparam name="T">The IHelpFormatter implementation type</typeparam>
        /// <returns>The <see cref="CommandLineApplicationBuilder"/></returns>
        public CommandLineApplicationBuilder UseHelpFormatter<T>() where T : class, IHelpFormatter
        {
            Services.AddSingleton<IHelpFormatter, T>();
            return this;
        }

        /// <summary>
        /// Configures the application to use a custom IHelpFormatter instance.
        /// </summary>
        /// <param name="formatter">The IHelpFormatter instance to use</param>
        /// <returns>The <see cref="CommandLineApplicationBuilder"/></returns>
        public CommandLineApplicationBuilder UseHelpFormatter(IHelpFormatter formatter)
        {
            Services.AddSingleton(formatter);
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

            var prompt = new Prompt();

            Services.AddSingleton(CommandRegistry);
            Services.AddSingleton(ConsoleService);
            Services.AddSingleton(prompt);

            // register null logging if logging not configured

            if (Services.BuildServiceProvider().GetService<ILoggerFactory>() == null)
            {
                Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            }

            // build components

            CommandRegistry.ConfigureServices(Services);
            var svcProvider = Services.BuildServiceProvider();

            foreach (var act in BuildActions)
                act.Invoke(svcProvider);

            var logger = svcProvider.GetService<ILogger<CommandLineApplication>>();
            
            var serverProxy = svcProvider.GetService<IServerProxy>();

            var core = new CommandLineApplicationCore(
                Console,
                CommandRegistry, 
                new CommandActivator(svcProvider),
                serverProxy);

            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(CommandRegistry, serverProxy, svcProvider));

            var input = new InputBuilder(Console, prompt, acCtrl);

            // build the command line application

            return new CommandLineApplication(svcProvider, Console, core, input);
        }
    }
}
