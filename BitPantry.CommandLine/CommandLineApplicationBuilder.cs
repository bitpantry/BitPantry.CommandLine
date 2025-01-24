using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Processing.Activation;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Prompt;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;

namespace BitPantry.CommandLine
{
    public class CommandLineApplicationBuilder
    {
        private IServiceCollection _services;
        private CommandRegistry _registry;
        private IAnsiConsole _console = AnsiConsole.Create(new AnsiConsoleSettings());
        private IConsoleService _consoleSvc = new SystemConsoleServices();

        private List<Assembly> _commandAssembliesSearched = new List<Assembly>();

        public IServiceCollection Services { get { return _services; } }

        public CommandLineApplicationBuilder()
        {
            _services = new ServiceCollection();
            _registry = new CommandRegistry(_services);
        }

        /// <summary>
        /// Registers the command by the given type parameter, T
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder RegisterCommand<T>() where T : CommandBase
        {
            _registry.RegisterCommand<T>();
            return this;
        }

        /// <summary>
        /// Registers the command by the given type
        /// </summary>
        /// <param name="type">The type of the command to register</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder RegisterCommand(Type type)
        {
            _registry.RegisterCommand(type);
            return this;
        }

        /// <summary>
        /// Registers all types that extend CommandBase for all assemblies represented by the types provided
        /// </summary>
        /// <param name="assemblyTargetTypes">The types that represent assemblies to be searched for commands to register</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder RegisterCommands(params Type[] assemblyTargetTypes)
            => RegisterCommands(assemblyTargetTypes, new Type[] { });

        /// <summary>
        /// Registers all types that extend CommandBase for all assemblies represented by the types provided
        /// </summary>
        /// <param name="assemblyTargetTypes">The types that represent assemblies to be searched for commands to register</param>
        /// <param name="ignoreTypes">Types to ignore when processing assembly types</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder RegisterCommands(Type[] assemblyTargetTypes, Type[] ignoreTypes)
        {
            var searchedAssemblies = new List<Assembly>();

            foreach (var type in assemblyTargetTypes)
            {
                if (!_commandAssembliesSearched.Contains(type.Assembly))
                {
                    foreach (var cmdType in type.Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract))
                    {
                        if(!ignoreTypes.Contains(cmdType))
                            _registry.RegisterCommand(cmdType);
                    }

                    _commandAssembliesSearched.Add(type.Assembly);
                }
            }

            return this;
        }

        /// <summary>
        /// Configures the application to use the given IAnsiConsole and IConsoleService implementations
        /// </summary>
        /// <param name="console">The implementation to use</param>
        /// <param name="consoleSvc">The implementation of IConsoleService to use</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder UsingConsole(IAnsiConsole console, IConsoleService consoleSvc = null)
        {
            _console = console;
            
            if(consoleSvc != null)
                _consoleSvc = consoleSvc;

            return this;
        }

        /// <summary>
        /// Builds and returns the CommandLineApplication
        /// </summary>
        /// <returns>The CommandLineApplication</returns>
        public CommandLineApplication Build()
        {
            // register services

            _services.AddSingleton(_registry);
            _services.AddSingleton(_consoleSvc);

            // build components

            var svcProvider = _services.BuildServiceProvider();

            var core = new CommandLineApplicationCore(
                _console, 
                _registry, 
                new CommandActivator(svcProvider));

            var consoleSvc = svcProvider.GetRequiredService<IConsoleService>();

            var acCtrl = new AutoCompleteController(
                new AutoCompleteOptionSetBuilder(_registry, svcProvider));

            var prompt = new CommandLinePrompt(_console, acCtrl);

            // build the command line application

            return new CommandLineApplication(_console, core, prompt);
        }
    }
}
