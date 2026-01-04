using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Removes a saved server profile.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "remove")]
    [Description("Removes a saved server profile")]
    public class ProfileRemoveCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;

        [Argument(Position = 0, IsRequired = true)]
        [Description("Profile name")]
        [Completion(typeof(ProfileNameProvider))]
        public string Name { get; set; }

        public ProfileRemoveCommand(IProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            var deleted = await _profileManager.DeleteProfileAsync(Name);

            if (deleted)
            {
                Console.MarkupLine($"[green]Profile '{Name}' removed[/]");
            }
            else
            {
                Console.MarkupLine($"[red]Profile '{Name}' not found[/]");
            }
        }
    }
}
