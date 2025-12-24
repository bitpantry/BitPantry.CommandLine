using BitPantry.CommandLine.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine
{
    public abstract class CommandRegistryApplicationBuilder<TType> where TType : CommandRegistryApplicationBuilder<TType>
    {
        public CommandRegistry CommandRegistry { get; }

        private List<Assembly> _commandAssembliesSearched = new List<Assembly>();

        public CommandRegistryApplicationBuilder()
        {
            CommandRegistry = new CommandRegistry();
        }

        /// <summary>
        /// Registers the command by the given type parameter, T
        /// </summary>
        /// <typeparam name="T">The type of the command to register</typeparam>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommand<T>() where T : CommandBase
        {
            CommandRegistry.RegisterCommand<T>();
            return (TType)this;
        }

        /// <summary>
        /// Registers the command by the given type
        /// </summary>
        /// <param name="type">The type of the command to register</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterCommand(Type type)
        {
            CommandRegistry.RegisterCommand(type);
            return (TType)this;
        }

        /// <summary>
        /// Registers a group marker type explicitly.
        /// </summary>
        /// <typeparam name="T">The group marker type (must have [Group] attribute)</typeparam>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterGroup<T>()
        {
            CommandRegistry.RegisterGroup(typeof(T));
            return (TType)this;
        }

        /// <summary>
        /// Registers a group marker type explicitly.
        /// </summary>
        /// <param name="groupType">The group marker type (must have [Group] attribute)</param>
        /// <returns>The CommandLineApplicationBuilder</returns>
        public TType RegisterGroup(Type groupType)
        {
            CommandRegistry.RegisterGroup(groupType);
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
        /// Also discovers and registers [Group] marker classes.
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
                    // First, discover and register all [Group] classes
                    foreach (var groupType in type.Assembly.GetTypes()
                        .Where(t => !t.IsAbstract && t.GetCustomAttributes(typeof(GroupAttribute), false).Any()))
                    {
                        if (!ignoreTypes.Contains(groupType))
                            CommandRegistry.RegisterGroup(groupType);
                    }

                    // Then, register all Command classes (they may reference groups)
                    foreach (var cmdType in type.Assembly.GetTypes()
                        .Where(t => t.IsSubclassOf(typeof(CommandBase)) && !t.IsAbstract))
                    {
                        if (!ignoreTypes.Contains(cmdType))
                            CommandRegistry.RegisterCommand(cmdType);
                    }

                    _commandAssembliesSearched.Add(type.Assembly);
                }
            }

            return (TType)this;
        }
    }
}
