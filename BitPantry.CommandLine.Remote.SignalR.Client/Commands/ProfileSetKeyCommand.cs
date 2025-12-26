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
    /// Updates the API key for a server profile.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "set-key")]
    [Description("Updates the API key for a server profile")]
    public class ProfileSetKeyCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialStore _credentialStore;

        [Argument(Position = 0)]
        [Description("Profile name")]
        [Completion(typeof(ProfileNameProvider))]
        public string Name { get; set; }

        [Argument]
        [Alias('k')]
        [Description("New API key (prompts if not provided)")]
        public string ApiKey { get; set; }

        public ProfileSetKeyCommand(IProfileManager profileManager, ICredentialStore credentialStore)
        {
            _profileManager = profileManager;
            _credentialStore = credentialStore;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            if (string.IsNullOrWhiteSpace(Name))
            {
                Console.MarkupLine("[red]✗ Profile name is required[/]");
                return;
            }

            var profile = await _profileManager.GetProfileAsync(Name);

            if (profile == null)
            {
                Console.MarkupLine($"[red]✗ Profile '{Name}' not found[/]");
                return;
            }

            // Prompt for API key if not provided
            var apiKey = ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Write("New API Key: ");
                apiKey = ReadPasswordMasked();
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.MarkupLine("[red]✗ API key is required[/]");
                return;
            }

            await _credentialStore.StoreAsync(Name, apiKey);
            
            // Update profile to mark credentials as saved
            profile.HasCredentials = true;
            await _profileManager.SaveProfileAsync(profile);

            Console.MarkupLine($"[green]✓ API key updated for profile '{Name}'[/]");
        }

        private string ReadPasswordMasked()
        {
            var password = new System.Text.StringBuilder();
            ConsoleKeyInfo key;

            do
            {
                key = System.Console.ReadKey(intercept: true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password.Append(key.KeyChar);
                    System.Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password.Remove(password.Length - 1, 1);
                    System.Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);

            System.Console.WriteLine();
            return password.ToString();
        }
    }
}
