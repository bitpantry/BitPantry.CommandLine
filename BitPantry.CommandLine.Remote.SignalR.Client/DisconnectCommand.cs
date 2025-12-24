using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Group = typeof(ServerGroup), Name = "disconnect")]
    [Description("Disconnects from a command line server")]
    public class DisconnectCommand : CommandBase
    {
        private IServerProxy _proxy;
        private Prompt _prompt;

        public DisconnectCommand(IServerProxy proxy, Prompt prompt)
        {
            _proxy = proxy;
            _prompt = prompt;
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

            _prompt.Reset();
        }
    }
}
