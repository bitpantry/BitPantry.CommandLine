using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Namespace = "server", Name = "disconnect")]
    [Description("Disconnects from a command line server")]
    public class DisconnectCommand : CommandBase
    {
        private IServerProxy _proxy;

        public DisconnectCommand(IServerProxy proxy)
        {
            _proxy = proxy;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                Console.MarkupLine("[yellow]No active connection[/]");
                return;
            }

            var currentRemoteAuthority = _proxy.ConnectionUri.Authority;

            await Console.Status()
                .StartAsync($"Disconnecting from {currentRemoteAuthority} ...", async ctx =>
                {
                    await _proxy.Disconnect();
                });

            Prompt.ServerName = null;
        }
    }
}
