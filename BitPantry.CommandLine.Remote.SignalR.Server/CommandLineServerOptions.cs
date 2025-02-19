using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class CommandLineServerOptions : CommandRegistryApplicationBuilder<CommandLineServerOptions>
    {
        internal ApplicationConfigurationHooks ConfigurationHooks { get; } = new ApplicationConfigurationHooks();

        public IServiceCollection Services { get; }
        public string HubUrlPattern { get; set; } = "/cli";

        public CommandLineServerOptions(IServiceCollection services)
        {
            Services = services;
        }
    }
}
