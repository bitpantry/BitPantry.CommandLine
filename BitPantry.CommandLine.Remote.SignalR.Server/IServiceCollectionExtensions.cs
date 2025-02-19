using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandLineHub(this IServiceCollection services, Action<CommandLineServerOptions> cliBldrAction = null)
        {
            // configure options

            var opt = new CommandLineServerOptions(services);
            cliBldrAction?.Invoke(opt);

            // add configure web application hooks

            opt.ConfigurationHooks.ConfigureWebApplication(app => app.UseEndpoints(ep => ep.MapHub<CommandLineHub>(opt.HubUrlPattern)));

            // configure signalR

            services.AddSignalR(opts => { opts.MaximumParallelInvocationsPerClient = 10; });

            // configure services

            opt.CommandRegistry.ConfigureServices(services);

            services.AddSingleton(opt.ConfigurationHooks);

            services.AddSingleton(new ServerSettings(opt.HubUrlPattern));
            
            services.AddSingleton(opt.CommandRegistry);

            services.AddScoped<ServerLogic>();

            services.AddScoped<RpcMessageRegistry>();
            services.AddScoped<IRpcScope, SignalRRpcScope>();

            return services;
        }
    }
}
