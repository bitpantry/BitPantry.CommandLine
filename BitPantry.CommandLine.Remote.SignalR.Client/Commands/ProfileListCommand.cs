using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System.Linq;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Lists all saved server profiles.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "list")]
    [Description("Lists all saved server profiles")]
    public class ProfileListCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;

        public ProfileListCommand(IProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            var profiles = await _profileManager.GetAllProfilesAsync();
            var defaultProfile = await _profileManager.GetDefaultProfileAsync();

            if (profiles.Count == 0)
            {
                Console.WriteLine("No server profiles saved.");
                Console.WriteLine();
                Console.MarkupLine("[dim]Create one with:[/]");
                Console.WriteLine("  server profile add <name> --uri <uri>");
                return;
            }

            Console.WriteLine("Server Profiles");
            Console.WriteLine();

            foreach (var profile in profiles.OrderBy(p => p.Name))
            {
                var isDefault = string.Equals(profile.Name, defaultProfile, StringComparison.OrdinalIgnoreCase);
                var defaultMarker = isDefault ? " *" : "  ";
                
                Console.MarkupLine($"  {profile.Name}{defaultMarker}     [dim]{new Uri(profile.Uri).Host}[/]");
            }

            Console.WriteLine();
            Console.MarkupLine("[dim]* = default profile[/]");
        }
    }
}
