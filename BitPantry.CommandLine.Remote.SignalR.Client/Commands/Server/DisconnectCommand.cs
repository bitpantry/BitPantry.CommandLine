using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    [InGroup<ServerGroup>]
    [Command(Name = "disconnect")]
    [Description("Disconnects from a command line server")]
    public class DisconnectCommand : CommandBase
    {
        private IServerProxy _proxy;
        private IProfileConnectionState _profileConnectionState;

        public DisconnectCommand(IServerProxy proxy, IProfileConnectionState profileConnectionState)
        {
            _proxy = proxy;
            _profileConnectionState = profileConnectionState;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
            {
                Console.MarkupLine("[yellow]No active connection[/]");
                return;
            }

            await _proxy.Disconnect();

            // Clear profile connection state
            _profileConnectionState.ConnectedProfileName = null;

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
        }
    }
}
