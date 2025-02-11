using BitPantry.CommandLine.Remote.SignalR.Server.Auth.Jwt;
using Microsoft.AspNetCore.Mvc.RazorPages.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public class CommandLineServerOptions : CommandRegistryApplicationBuilder<CommandLineServerOptions>
    {
        public IServiceCollection Services { get; }
        public string HubUrlPattern { get; set; } = ServerSettings.HubUrlPattern;

        public CommandLineServerOptions(IServiceCollection services)
        {
            Services = services;
        }
    }
}
