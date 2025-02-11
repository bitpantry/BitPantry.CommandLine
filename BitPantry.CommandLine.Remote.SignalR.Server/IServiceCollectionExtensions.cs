using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddCommandLineHub(this IServiceCollection services, Action<CommandLineServerOptions> cliBldrAction = null)
        {
            var builder = new CommandLineServerOptions(services);
            cliBldrAction?.Invoke(builder);

            ServerSettings.HubUrlPattern = builder.HubUrlPattern;

            // core services

            services.AddSignalR(bldr =>
            {
                bldr.MaximumParallelInvocationsPerClient = 10;
            });

            services.AddHttpContextAccessor();

            // cli components

            builder.CommandRegistry.ConfigureServices(services);
            services.AddSingleton(builder.CommandRegistry);

            // client server components

            services.AddScoped<ServerLogic>();

            services.AddScoped<RpcMessageRegistry>();
            services.AddScoped<IRpcScope, SignalRRpcScope>();

            return services;
        }
    }
}
