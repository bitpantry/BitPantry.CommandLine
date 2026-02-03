using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Input;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    [InGroup<ServerGroup>]
    [Command(Name = "disconnect")]
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

            await _proxy.Disconnect(); 

            // Prompt is now handled by ServerConnectionSegment which reads from IServerProxy
        }
    }
}
