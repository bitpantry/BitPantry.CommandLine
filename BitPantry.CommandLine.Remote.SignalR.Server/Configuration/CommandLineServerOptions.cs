﻿using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Configuration
{
    /// <summary>
    /// Used to configure the command line server
    /// </summary>
    public class CommandLineServerOptions : CommandRegistryApplicationBuilder<CommandLineServerOptions>
    {
        internal ApplicationConfigurationHooks ConfigurationHooks { get; } = new ApplicationConfigurationHooks();

        /// <summary>
        /// The ASP.NET application builder service collection
        /// </summary>
        public IServiceCollection Services { get; }

        /// <summary>
        /// The URL pattern for the <see cref="CommandLineHub"/>. e.g, a pattern of "/cli" for a web application with a root of 
        /// http://localhost will make the hub's uri, "http://localhost/cli".
        /// </summary>
        public string HubUrlPattern { get; set; } = "/cli";

        public CommandLineServerOptions(IServiceCollection services)
        {
            Services = services;
        }
    }
}
