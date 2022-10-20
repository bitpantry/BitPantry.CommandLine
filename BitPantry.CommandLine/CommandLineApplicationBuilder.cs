using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Interface;
using BitPantry.CommandLine.Interface.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine
{
    public class CommandLineApplicationBuilder
    {
        private CommandRegistry _registry = new CommandRegistry();
        private IContainer _container;
        private IInterface _interface = new ConsoleInterface();

        private List<Assembly> _commandAssembliesSearched = new List<Assembly>();

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
        {
            var searchedAssemblies = new List<Assembly>();

            foreach (var type in assemblyTargetTypes)
            {
                if (!_commandAssembliesSearched.Contains(type.Assembly))
                {
                    foreach (var cmdType in type.Assembly.GetTypes().Where(t => t.BaseType == typeof(CommandBase)))
                        _registry.RegisterCommand(cmdType);

                    _commandAssembliesSearched.Add(type.Assembly);
                }
            }

            return this;
        }

        /// <summary>
        /// Configures the CommandLineApplicationBuilder to build a CommandLineApplication that uses the given IContainer implementation for instance and dependency management
        /// </summary>
        /// <param name="container">The container implementation to use</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public CommandLineApplicationBuilder UsingDependencyContainer(IContainer container)
        {
            _container = container;
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
                _container,
                _interface);
        }
    }
}
