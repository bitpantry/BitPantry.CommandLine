using BitPantry.CommandLine.API;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BitPantry.CommandLine
{
    public static class ServiceCollectionExtensions
    {

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
    }
}
