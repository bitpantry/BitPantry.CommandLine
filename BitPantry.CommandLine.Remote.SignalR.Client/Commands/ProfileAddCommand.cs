using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;
using System;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands
{
    /// <summary>
    /// Adds a new server profile or updates an existing one.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "add")]
    [Description("Adds a new server profile")]
    public class ProfileAddCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly ICredentialStore _credentialStore;

        [Argument(Position = 0)]
        [Description("Profile name")]
        public string Name { get; set; }

        [Argument]
        [Description("Server URI")]
        public string Uri { get; set; }

        [Argument]
        [Alias('k')]
        [Description("API key (prompts if not provided)")]
        public string ApiKey { get; set; }

        [Argument]
        [Description("Set as default profile")]
        public Option Default { get; set; }

        [Argument]
        [Alias('f')]
        [Description("Overwrite existing profile without confirmation")]
        public Option Force { get; set; }

        public ProfileAddCommand(IProfileManager profileManager, ICredentialStore credentialStore)
        {
            _profileManager = profileManager;
            _credentialStore = credentialStore;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Validate required arguments
            if (string.IsNullOrWhiteSpace(Name))
            {
                Console.MarkupLine("[red]✗ Profile name is required[/]");
                return;
            }

            if (string.IsNullOrWhiteSpace(Uri))
            {
                Console.MarkupLine("[red]✗ --uri is required[/]");
                return;
            }

            // Validate profile name
            if (!_profileManager.IsValidProfileName(Name))
            {
                Console.MarkupLine("[red]✗ Invalid profile name. Use only letters, numbers, hyphens, and underscores.[/]");
                return;
            }

            // Validate URI format
            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            {
                Console.MarkupLine($"[red]✗ Invalid URI format: {Uri}[/]");
                return;
            }

            // Check if profile exists
            var existingProfile = await _profileManager.GetProfileAsync(Name);
            if (existingProfile != null && !Force.IsPresent)
            {
                Console.MarkupLine($"[yellow]⚠ Profile '{Name}' already exists ({new System.Uri(existingProfile.Uri).Host})[/]");
                Console.Write("Overwrite? [y/N]: ");
                var response = System.Console.ReadLine()?.Trim().ToLowerInvariant();
                if (response != "y" && response != "yes")
                {
                    return;
                }
            }

            // Prompt for API key if not provided
            var apiKey = ApiKey;
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Write("API Key: ");
                apiKey = ReadPasswordMasked();
            }

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.MarkupLine("[red]✗ API key is required[/]");
                return;
            }

            // Create/update profile
            var profile = new ServerProfile
            {
                Name = Name,
                Uri = Uri,
                HasCredentials = true
            };

            await _profileManager.SaveProfileAsync(profile);
            await _credentialStore.StoreAsync(Name, apiKey);

            // Set as default if requested
            if (Default.IsPresent)
            {
                await _profileManager.SetDefaultProfileAsync(Name);
            }

            var verb = existingProfile != null ? "updated" : "created";
            var defaultMsg = Default.IsPresent ? " (set as default)" : "";
            Console.MarkupLine($"[green]✓ Profile '{Name}' {verb}{defaultMsg}[/]");
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
