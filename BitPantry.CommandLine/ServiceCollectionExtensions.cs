using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Help;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using System.Reflection;

namespace BitPantry.CommandLine
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the IFileSystem service as a singleton FileSystem implementation
        /// </summary>
        /// <param name="services">The service collection to add the file system to</param>
        /// <returns>The service collection (same one passed in)</returns>
        public static IServiceCollection AddFileSystem(this IServiceCollection services)
        {
            services.AddSingleton<IFileSystem, FileSystem>();
            return services;
        }

        /// <summary>
        /// Adds the IHelpFormatter service as a singleton HelpFormatter implementation
        /// </summary>
        /// <param name="services">The service collection to add the help formatter to</param>
        /// <returns>The service collection (same one passed in)</returns>
        public static IServiceCollection AddHelpFormatter(this IServiceCollection services)
        {
            services.AddSingleton<IHelpFormatter, HelpFormatter>();
            return services;
        }

        /// <summary>
        /// Adds all commands found to the service collection as transient dependencies
        /// </summary>
        /// <param name="services">The service collection to add found commands to</param>
        /// <param name="searchAssemblyTargets">Reference types that tell the function which assemblies to look in</param>
        /// <returns>The service collection (same one passed in)</returns>
        public static IServiceCollection AddCommands(this IServiceCollection services, params Type[] searchAssemblyTargets)
        {
            var searchedAssemblies = new List<Assembly>();

            foreach (var type in searchAssemblyTargets)
            {
                if(!searchedAssemblies.Contains(type.Assembly))
                {
                    foreach (var cmdType in type.Assembly.GetTypes().Where(t => t.BaseType == typeof(CommandBase)))
                        services.AddTransient(cmdType);

                    searchedAssemblies.Add(type.Assembly);
                }
            }

            return services;
        }

        /// <summary>
        /// Adds the completion system services including cache, providers, and orchestrator.
        /// </summary>
        /// <param name="services">The service collection to add completion services to.</param>
        /// <param name="configureCache">Optional action to configure the cache options.</param>
        /// <returns>The service collection (same one passed in).</returns>
        public static IServiceCollection AddCompletionServices(
            this IServiceCollection services,
            Action<CompletionCacheOptions>? configureCache = null)
        {
            var cacheOptions = new CompletionCacheOptions();
            configureCache?.Invoke(cacheOptions);

            // Register cache
            services.AddSingleton<ICompletionCache>(sp =>
                new CompletionCache(cacheOptions.MaxEntries, cacheOptions.TimeProvider));

            // Register InputLog as singleton for history-based completion
            services.AddSingleton<Input.InputLog>();

            // Register built-in providers
            services.AddSingleton<ICompletionProvider, CommandCompletionProvider>();
            services.AddSingleton<ICompletionProvider, HistoryProvider>();
            services.AddSingleton<ICompletionProvider, PositionalArgumentProvider>();
            services.AddSingleton<ICompletionProvider, ArgumentNameProvider>();
            services.AddSingleton<ICompletionProvider, ArgumentAliasProvider>();
            services.AddSingleton<ICompletionProvider, EnumProvider>();
            services.AddSingleton<ICompletionProvider, StaticValuesProvider>();
            services.AddSingleton<ICompletionProvider, FilePathProvider>();
            services.AddSingleton<ICompletionProvider, DirectoryPathProvider>();
            services.AddSingleton<ICompletionProvider, MethodProvider>();
            // Additional providers will be added in subsequent phases:
            // services.AddSingleton<ICompletionProvider, ArgumentNameCompletionProvider>();
            // services.AddSingleton<ICompletionProvider, EnumCompletionProvider>();
            // services.AddSingleton<ICompletionProvider, FilePathCompletionProvider>();
            // services.AddSingleton<ICompletionProvider, DirectoryPathCompletionProvider>();
            // services.AddSingleton<ICompletionProvider, StaticValuesCompletionProvider>();
            // services.AddSingleton<ICompletionProvider, MethodCompletionProvider>();

            // Register orchestrator
            services.AddSingleton<ICompletionOrchestrator, CompletionOrchestrator>();

            return services;
        }
    }
}

/// <summary>
/// Options for configuring the completion cache.
/// </summary>
public class CompletionCacheOptions
{
    /// <summary>
    /// Gets or sets the maximum number of cache entries. Default is 100.
    /// </summary>
    public int MaxEntries { get; set; } = 100;

    /// <summary>
    /// Gets or sets the time provider for cache expiration (for testing). Default is system time.
    /// </summary>
    public TimeProvider? TimeProvider { get; set; }
}
