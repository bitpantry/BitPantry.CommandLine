using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
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

        /// <summary>
        /// Gets the service collection for registering DI services.
        /// Derived classes must implement this to provide access to their service collection.
        /// </summary>
        protected abstract IServiceCollection ModuleServices { get; }

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

        /// <summary>
        /// Installs a command module that registers its commands, services, and autocomplete handlers.
        /// </summary>
        /// <typeparam name="TModule">The module type implementing ICommandModule with a parameterless constructor.</typeparam>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TType InstallModule<TModule>() where TModule : ICommandModule, new()
        {
            return InstallModule<TModule>(null);
        }

        /// <summary>
        /// Installs a command module with configuration, allowing module properties to be set before Configure is called.
        /// </summary>
        /// <typeparam name="TModule">The module type implementing ICommandModule with a parameterless constructor.</typeparam>
        /// <param name="configure">Optional action to configure the module instance before Configure is called.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        public TType InstallModule<TModule>(Action<TModule>? configure) where TModule : ICommandModule, new()
        {
            var module = new TModule();
            configure?.Invoke(module);

            var context = CreateModuleContext();
            module.Configure(context);

            return (TType)this;
        }

        /// <summary>
        /// Loads and installs command modules from all subdirectories in the specified plugins directory.
        /// Each subdirectory is expected to contain a DLL with the same name as the subdirectory.
        /// For example, plugins/MyModule/MyModule.dll would be loaded from the MyModule subdirectory.
        /// </summary>
        /// <param name="pluginsDirectoryPath">Path to the plugins directory. If the directory does not exist, this is a no-op.</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <remarks>
        /// Each plugin assembly is loaded into its own AssemblyLoadContext for dependency isolation.
        /// The plugin's dependencies (copied via EnableDynamicLoading) are resolved from the plugin's directory,
        /// while shared types (CommandBase, ICommandModule, etc.) are resolved from the host's default context.
        /// </remarks>
        public TType InstallModulesFromDirectory(string pluginsDirectoryPath)
        {
            if (!Directory.Exists(pluginsDirectoryPath))
            {
                // Per Requirement 15: If the plugins directory does not exist, this is a no-op (no error thrown)
                return (TType)this;
            }

            foreach (var subdirectory in Directory.GetDirectories(pluginsDirectoryPath))
            {
                var directoryName = Path.GetFileName(subdirectory);
                var dllPath = Path.Combine(subdirectory, $"{directoryName}.dll");

                if (File.Exists(dllPath))
                {
                    InstallModuleFromAssembly(dllPath);
                }
            }

            return (TType)this;
        }

        /// <summary>
        /// Loads and installs command modules from a single assembly file.
        /// All types implementing ICommandModule with a parameterless constructor will be instantiated and configured.
        /// </summary>
        /// <param name="assemblyPath">Full path to the assembly file (.dll).</param>
        /// <returns>The builder instance for fluent chaining.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the assembly file does not exist.</exception>
        /// <remarks>
        /// The assembly is loaded into its own AssemblyLoadContext for dependency isolation.
        /// </remarks>
        public TType InstallModuleFromAssembly(string assemblyPath)
        {
            if (!File.Exists(assemblyPath))
            {
                throw new FileNotFoundException($"Plugin assembly not found: {assemblyPath}", assemblyPath);
            }

            var loadContext = new ModuleLoadContext(assemblyPath);
            var assemblyName = new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath));
            
            Assembly assembly;
            try
            {
                assembly = loadContext.LoadFromAssemblyName(assemblyName);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to load plugin assembly: {assemblyPath}", ex);
            }

            var context = CreateModuleContext();
            var moduleInterfaceType = typeof(ICommandModule);

            // Find all types implementing ICommandModule
            var moduleTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && 
                            !t.IsInterface && 
                            moduleInterfaceType.IsAssignableFrom(t) &&
                            t.GetConstructor(Type.EmptyTypes) != null);

            foreach (var moduleType in moduleTypes)
            {
                var module = (ICommandModule)Activator.CreateInstance(moduleType)!;
                module.Configure(context);
            }

            return (TType)this;
        }

        /// <summary>
        /// Creates a module context for module configuration.
        /// </summary>
        private CommandModuleContext CreateModuleContext()
        {
            return new CommandModuleContext(
                CommandRegistryBuilder,
                ModuleServices,
                AutoCompleteHandlerRegistryBuilder);
        }
    }
}
