using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Sets the default server profile.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "set-default")]
    [Description("Sets the default server profile")]
    public class ProfileSetDefaultCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;

        [Argument(Position = 0, IsRequired = true)]
        [Description("Profile name")]
        [Completion(typeof(ProfileNameProvider))]
        public string Name { get; set; }

        public ProfileSetDefaultCommand(IProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            var profile = await _profileManager.GetProfileAsync(Name);

            if (profile == null)
            {
                Console.MarkupLine($"[red]✗ Profile '{Name}' not found[/]");
                return;
            }

            await _profileManager.SetDefaultProfileAsync(Name);
            Console.MarkupLine($"[green]✓ Default profile set to '{Name}'[/]");
        }
    }
}
