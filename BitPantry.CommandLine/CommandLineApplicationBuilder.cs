using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Interface;
using BitPantry.CommandLine.Interface.Console;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine
{
    public class CommandLineApplicationBuilder
    {
        private IServiceCollection _services;
        private CommandRegistry _registry;
        private IInterface _interface = new ConsoleInterface();

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
        /// Configures the CommandLineApplicationBuilder to build a CommandApplication that uses the given IInterface implementation
        /// </summary>
        /// <param name="interfc">The interface implementation to use</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder UsingInterface(IInterface interfc)
        {
            _interface = interfc;
            return this;
        }

        /// <summary>
        /// Builds and returns the CommandLineApplication
        /// </summary>
        /// <returns>The CommandLineApplication</returns>
        public CommandLineApplication Build()
        {
            return new CommandLineApplication(
                _registry,
                _services.BuildServiceProvider(),
                _interface);
        }
    }
}
