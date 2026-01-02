using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.Remote.SignalR.Client.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Shows details of a saved server profile.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "show")]
    [Description("Shows details of a saved server profile")]
    public class ProfileShowCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialStore _credentialStore;

        [Argument(Position = 0, IsRequired = true)]
        [Description("Profile name")]
        [Completion(typeof(ProfileNameProvider))]
        public string Name { get; set; }

        public ProfileShowCommand(IProfileManager profileManager, ICredentialStore credentialStore)
        {
            _profileManager = profileManager;
            _credentialStore = credentialStore;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            var profile = await _profileManager.GetProfileAsync(Name);

            if (profile == null)
            {
                Console.MarkupLine($"[red]✗ Profile '{Name}' not found[/]");
                return;
            }

            var defaultProfile = await _profileManager.GetDefaultProfileAsync();
            var isDefault = string.Equals(profile.Name, defaultProfile, StringComparison.OrdinalIgnoreCase);
            var hasCredentials = await _credentialStore.ExistsAsync(profile.Name);

            Console.MarkupLine($"[bold]Profile: {profile.Name}[/]");
            Console.MarkupLine("[dim]─────────────[/]");
            Console.MarkupLine($"  URI:          [cyan]{new Uri(profile.Uri).Host}[/]");
            Console.MarkupLine($"  Credentials:  {(hasCredentials ? "[green]Saved[/]" : "[yellow]Not saved[/]")}");
            Console.MarkupLine($"  Default:      {(isDefault ? "[green]Yes[/]" : "[dim]No[/]")}");
        }
    }
}
