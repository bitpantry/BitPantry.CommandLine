using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Input;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private PromptOptions _promptOptions = new PromptOptions();
        private AutoCompleteHandlerRegistryBuilder _autoCompleteHandlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();

        public CommandLineApplicationBuilder()
        {
            System.Console.OutputEncoding = Encoding.UTF8;
            Console = AnsiConsole.Create(new AnsiConsoleSettings());

            Services = new ServiceCollection();

            // Register built-in commands
            CommandRegistryBuilder.RegisterCommand<ListCommandsCommand>();

            // the server proxy is disabled by default

            Services.AddFileSystem();
            Services.AddSingleton<IServerProxy, NoopServerProxy>();
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
        /// Configures the prompt appearance including application name and suffix.
        /// Values support Spectre.Console markup.
        /// </summary>
        /// <param name="configure">Action to configure prompt options.</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        /// <example>
        /// <code>
        /// builder.ConfigurePrompt(prompt => prompt
        ///     .Name("myapp")
        ///     .WithSuffix("$ "));
        ///
        /// // With Spectre.Console markup:
        /// builder.ConfigurePrompt(prompt => prompt
        ///     .Name("[bold cyan]myapp[/]")
        ///     .WithSuffix("[green]>[/] "));
        /// </code>
        /// </example>
        public CommandLineApplicationBuilder ConfigurePrompt(Action<PromptOptions> configure)
        {
            configure(_promptOptions);
            return this;
        }

        /// <summary>
        /// Configures autocomplete handlers for argument value suggestions.
        /// Use this to register custom handlers for specific types or attributes.
        /// </summary>
        /// <param name="configure">Action to configure the autocomplete handler registry.</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        /// <example>
        /// <code>
        /// builder.ConfigureAutoComplete(ac => ac
        ///     .RegisterTypeHandler&lt;DateTime, DateTimeAutoCompleteHandler&gt;()
        ///     .RegisterTypeHandler&lt;MyEnum, MyEnumHandler&gt;());
        /// </code>
        /// </example>
        public CommandLineApplicationBuilder ConfigureAutoComplete(Action<IAutoCompleteHandlerRegistryBuilder> configure)
        {
            configure(_autoCompleteHandlerRegistryBuilder);
            return this;
        }

        /// <summary>
        /// Builds and returns the CommandLineApplication
        /// </summary>
        /// <returns>The CommandLineApplication</returns>
        public CommandLineApplication Build()
        {
            Services.AddSingleton(Console);

            // register prompt options
            Services.AddSingleton(_promptOptions);

            // register core prompt segment with configured name
            Services.AddSingleton<IPromptSegment>(new AppNameSegment(_promptOptions.AppName));

            // Build the immutable command registry from the builder (also registers command types with DI)
            var commandRegistry = CommandRegistryBuilder.Build(Services);

            Services.AddSingleton<ICommandRegistry>(commandRegistry);
            Services.AddSingleton(ConsoleService);
            Services.AddHelpFormatter();

            // register null logging if logging not configured

            if (Services.BuildServiceProvider().GetService<ILoggerFactory>() == null)
            {
                Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
                Services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
            }

            // register composite prompt with configured suffix
            Services.AddSingleton<IPrompt>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<CompositePrompt>>();
                var segments = sp.GetServices<IPromptSegment>();
                var options = sp.GetRequiredService<PromptOptions>();
                return new CompositePrompt(logger, segments, options.Suffix);
            });

            // Build autocomplete handler registry BEFORE building service provider
            // This ensures handler types are registered with DI before the provider is created
            var handlerRegistry = _autoCompleteHandlerRegistryBuilder.Build(Services);

            // Register key processed notifier for input synchronization (used by tests)
            Services.AddSingleton<KeyProcessedNotifier>();
            Services.AddSingleton<IKeyProcessedObservable>(sp => sp.GetRequiredService<KeyProcessedNotifier>());

            // build components

            var svcProvider = Services.BuildServiceProvider();

            foreach (var act in BuildActions)
                act.Invoke(svcProvider);

            var logger = svcProvider.GetService<ILogger<CommandLineApplication>>();
            
            var serverProxy = svcProvider.GetService<IServerProxy>();
            var helpFormatter = svcProvider.GetRequiredService<IHelpFormatter>();

            var core = new CommandLineApplicationCore(
                Console,
                commandRegistry, 
                new CommandActivator(svcProvider),
                serverProxy,
                helpFormatter);

            // Create handler activator now that service provider is built
            var handlerActivator = new AutoCompleteHandlerActivator(svcProvider);

            // Create autocomplete controller with handler registry for value suggestions
            // Pass serverProxy to enable remote command autocomplete via RPC
            var acLogger = svcProvider.GetRequiredService<ILogger<AutoCompleteSuggestionProvider>>();
            var acCtrl = new AutoCompleteController(commandRegistry, Console, handlerRegistry, handlerActivator, serverProxy, acLogger);

            // Get the prompt from DI
            var prompt = svcProvider.GetRequiredService<IPrompt>();

            // Get the key processed notifier for input synchronization
            var notifier = svcProvider.GetRequiredService<KeyProcessedNotifier>();

            var input = new InputBuilder(Console, prompt, acCtrl, notifier);

            // build the command line application

            return new CommandLineApplication(svcProvider, Console, core, input);
        }
    }
}
