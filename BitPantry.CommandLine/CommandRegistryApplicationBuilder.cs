using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine
{
    public abstract class CommandRegistryApplicationBuilder<TType> where TType : CommandRegistryApplicationBuilder<TType>
    {
        /// <summary>
        /// Gets the command registry builder for configuring command registrations.
        /// </summary>
        public CommandRegistryBuilder CommandRegistryBuilder { get; }

        /// <summary>
        /// Gets the autocomplete handler registry builder for configuring autocomplete handlers.
        /// </summary>
        public AutoCompleteHandlerRegistryBuilder AutoCompleteHandlerRegistryBuilder { get; }

        private List<Assembly> _commandAssembliesSearched = new List<Assembly>();

        public CommandRegistryApplicationBuilder()
        {
            CommandRegistryBuilder = new CommandRegistryBuilder();
            AutoCompleteHandlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
        }

        /// <summary>
        /// Configures the autocomplete handler registry using the provided action.
        /// </summary>
        /// <param name="configure">Action to configure the autocomplete handler registry builder</param>
        /// <returns>The builder instance for fluent chaining</returns>
        public TType ConfigureAutoComplete(Action<AutoCompleteHandlerRegistryBuilder> configure)
        {
            configure(AutoCompleteHandlerRegistryBuilder);
            return (TType)this;
        }

        /// <summary>
        /// Registers the command by the given type parameter, T
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommand<T>() where T : CommandBase
        {
            CommandRegistryBuilder.RegisterCommand<T>();
            return (TType)this;
        }

        /// <summary>
        /// Registers the command by the given type
        /// </summary>
        /// <param name="type">The type of the command to register</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommand(Type type)
        {
            CommandRegistryBuilder.RegisterCommand(type);
            return (TType)this;
        }

        /// <summary>
        /// Registers all types that extend CommandBase for all assemblies represented by the types provided
        /// </summary>
        /// <param name="assemblyTargetTypes">The types that represent assemblies to be searched for commands to register</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommands(params Type[] assemblyTargetTypes)
            => RegisterCommands(assemblyTargetTypes, new Type[] { });

        /// <summary>
        /// Registers all types that extend CommandBase for all assemblies represented by the types provided.
        /// Groups are automatically registered when commands with [InGroup] attribute are discovered.
        /// </summary>
        /// <param name="assemblyTargetTypes">The types that represent assemblies to be searched for commands to register</param>
        /// <param name="ignoreTypes">Types to ignore when processing assembly types</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommands(Type[] assemblyTargetTypes, Type[] ignoreTypes)
        {
            var searchedAssemblies = new List<Assembly>();

            foreach (var type in assemblyTargetTypes)
            {
                if (!_commandAssembliesSearched.Contains(type.Assembly))
                {
                    // Register all Command classes (groups are auto-registered via [InGroup<T>] attribute)
                    foreach (var cmdType in type.Assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract))
                    {
                        if (!ignoreTypes.Contains(cmdType))
                            CommandRegistryBuilder.RegisterCommand(cmdType);
                    }

                    _commandAssembliesSearched.Add(type.Assembly);
                }
            }

            return (TType)this;
        }
    }
}
