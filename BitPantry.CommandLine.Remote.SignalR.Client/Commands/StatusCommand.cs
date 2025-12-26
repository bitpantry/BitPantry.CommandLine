using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Displays the current connection status and profile information.
    /// </summary>
    [Command(Group = typeof(ServerGroup), Name = "status")]
    [Description("Displays connection status and profile information")]
    public class StatusCommand : CommandBase
    {
        private readonly IServerProxy _serverProxy;
        private readonly IProfileManager _profileManager;

        [Argument]
        [Alias('v')]
        [Description("Show detailed status information")]
        public Option Verbose { get; set; }

        public StatusCommand(IServerProxy serverProxy, IProfileManager profileManager)
        {
            _serverProxy = serverProxy;
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            Console.WriteLine();
            
            // Connection Status
            RenderConnectionStatus();

            // Profile Information
            await RenderProfileStatus();

            Console.WriteLine();
        }

        private void RenderConnectionStatus()
        {
            Console.MarkupLine("[bold]Connection Status[/]");
            Console.MarkupLine("[dim]─────────────────[/]");

            switch (_serverProxy.ConnectionState)
            {
                case ServerProxyConnectionState.Connected:
                    var uri = _serverProxy.ConnectionUri;
                    Console.MarkupLine($"  Status:  [green]Connected[/]");
                    Console.MarkupLine($"  Server:  [cyan]{uri?.Host}[/]");
                    if (Verbose.IsPresent && uri != null)
                    {
                        Console.MarkupLine($"  Port:    [dim]{uri.Port}[/]");
                        Console.MarkupLine($"  Scheme:  [dim]{uri.Scheme}[/]");
                    }
                    break;

                case ServerProxyConnectionState.Connecting:
                    Console.MarkupLine($"  Status:  [yellow]Connecting...[/]");
                    break;

                case ServerProxyConnectionState.Reconnecting:
                    Console.MarkupLine($"  Status:  [yellow]Reconnecting...[/]");
                    break;

                case ServerProxyConnectionState.Disconnected:
                default:
                    Console.MarkupLine($"  Status:  [dim]Disconnected[/]");
                    break;
            }

            Console.WriteLine();
        }

        private async Task RenderProfileStatus()
        {
            Console.MarkupLine("[bold]Profile Status[/]");
            Console.MarkupLine("[dim]──────────────[/]");

            var profiles = await _profileManager.GetAllProfilesAsync();
            var defaultProfile = await _profileManager.GetDefaultProfileAsync();

            if (!profiles.Any())
            {
                Console.MarkupLine("  [dim]No profiles configured[/]");
                Console.MarkupLine($"  [dim]Use 'server profile add' to create a profile[/]");
                return;
            }

            Console.MarkupLine($"  Profiles:  [cyan]{profiles.Count()}[/]");
            
            if (!string.IsNullOrEmpty(defaultProfile))
            {
                Console.MarkupLine($"  Default:   [cyan]{defaultProfile}[/]");
            }
            else
            {
                Console.MarkupLine($"  Default:   [dim]None set[/]");
            }

            // Show all profiles in verbose mode
            if (Verbose.IsPresent && profiles.Any())
            {
                Console.WriteLine();
                Console.MarkupLine("  [dim]Available profiles:[/]");
                foreach (var profile in profiles.OrderBy(p => p.Name))
                {
                    var isDefault = string.Equals(profile.Name, defaultProfile, StringComparison.OrdinalIgnoreCase);
                    var defaultMarker = isDefault ? " [green](default)[/]" : "";
                    Console.MarkupLine($"    • [cyan]{profile.Name}[/]{defaultMarker}");
                    Console.MarkupLine($"      [dim]{new Uri(profile.Uri).Host}[/]");
                }
            }
        }
    }
}
