using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Rpc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public static class CommandLineApplicationBuilderExtensions
    {
        public static CommandLineApplicationBuilder ConfigureSignalRClient(this CommandLineApplicationBuilder builder)
        {
            builder.Services.AddSingleton<RpcMessageRegistry>();
            builder.Services.AddSingleton<IRpcScope, SingletonRpcScope>();

            builder.Services.AddSingleton<IServerProxy>(provider =>
                new SignalRServerProxy(
                    provider.GetRequiredService<ILogger<SignalRServerProxy>>(),
                    new ClientLogic(provider.GetRequiredService<CommandRegistry>()),
                    provider.GetRequiredService<IAnsiConsole>(),
                    provider.GetRequiredService<CommandRegistry>(),
                    provider.GetRequiredService<RpcMessageRegistry>()));

            builder.RegisterCommand<AuthenticateCommand>();
            builder.RegisterCommand<ConnectCommand>();
            builder.RegisterCommand<DisconnectCommand>();

            return builder;
        }
    }
}
