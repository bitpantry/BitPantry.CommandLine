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

        public CommandLineApplicationBuilder RegisterCommand<T>() where T : CommandBase
        {
            _registry.RegisterCommand<T>();
            return this;
        }

        public CommandLineApplicationBuilder RegisterCommand(Type type)
        {
            _registry.RegisterCommand(type);
            return this;
        }

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

        public CommandLineApplicationBuilder UsingDependencyContainer(IContainer container)
        {
            _container = container;
            return this;
        }

        public CommandLineApplicationBuilder UsingInterface(IInterface interfc)
        {
            _interface = interfc;
            return this;
        }

        public CommandLineApplication Build()
        {
            return new CommandLineApplication(
                _registry,
                _container,
                _interface);
        }
    }
}
