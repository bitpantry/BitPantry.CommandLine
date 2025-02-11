using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    [Command(Namespace = "server", Name = "connect")]
    [Description("Connects to a remote command line server")]
    public class ConnectCommand : CommandBase
    {
        private IServerProxy _proxy;

        [Argument]
        [Alias('u')]
        [Description("The remote uri to connect to")]
        public string Uri { get; set; }

        [Argument]
        [Alias('d')]
        [Description("If present any existing connection will be disconnected without confirmation")]
        public Option ConfirmDisconnect { get; set; }

        public ConnectCommand(IServerProxy proxy)
        {
            _proxy = proxy;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            if (string.IsNullOrEmpty(Uri))
            {
                Console.Markup($"[red]Uri is required[/]");
                return;
            }

            if (_proxy.ConnectionState != ServerProxyConnectionState.Disconnected)
            {
                var currentRemoteAuthority = _proxy.ConnectionUri.Authority;

                if (ConfirmDisconnect.IsPresent && !Console.Prompt(new ConfirmationPrompt($"A connection to [yellow]{currentRemoteAuthority}[/] is currently active - do you want to disconnect?")))
                    return;

                await Console.Status().StartAsync($"Disconnecting from {currentRemoteAuthority} ...", async ctx => await _proxy.Disconnect());
            }

            try
            {
                await Console.Status().StartAsync("Connecting ...", async ctx => await _proxy.Connect(Uri));

                Console.WriteLine();
                Console.MarkupLineInterpolated($"Connected to [yellow]{_proxy.ConnectionUri.Authority}[/]");
                Console.MarkupLineInterpolated($"[yellow]{ctx.CommandRegistry.Commands.Where(c => c.IsRemote).Count()}[/] remote commands available");
                Console.WriteLine();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    var authCmd = ctx.CommandRegistry.Commands.Where(c => c.Type == typeof(AuthenticateCommand)).Single();

                    Console.WriteLine();
                    Console.MarkupLine($"[yellow]Authentication Required[/]");
                    Console.MarkupLine($"Use [blue]{authCmd.Namespace}.{authCmd.Name}[/]");
                    Console.WriteLine();
                }
                else
                {
                    throw;
                }
            }


        }
    }
}
