using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Group = typeof(ServerGroup), Name = "disconnect")]
    [Description("Disconnects from a command line server")]
    public class DisconnectCommand : CommandBase
    {
        private readonly IServerProxy _proxy;

        [Argument]
        [Alias('f')]
        [Description("Disconnect without confirmation prompt")]
        public Option Force { get; set; }

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

            // Confirm disconnect unless --force is specified
            if (!Force.IsPresent)
            {
                var confirmed = Console.Prompt(
                    new ConfirmationPrompt($"Disconnect from [cyan]{currentRemoteAuthority}[/]?")
                    {
                        DefaultValue = true
                    });

                if (!confirmed)
                {
                    Console.MarkupLine("[dim]Cancelled[/]");
                    return;
                }
            }

            await Console.Status()
                .StartAsync($"Disconnecting from {currentRemoteAuthority} ...", async ctx =>
                {
                    await _proxy.Disconnect();
                });

            Console.MarkupLine($"[green]✓ Disconnected from {currentRemoteAuthority}[/]");
        }
    }
}
