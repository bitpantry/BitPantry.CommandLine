using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server.Profiles
{
    /// <summary>
    /// Creates a new server connection profile.
    /// Usage: server profile add [name] -u [uri] [-k api-key] [--default]
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "add")]
    [Description("Create a new server connection profile")]
    public class ProfileAddCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;

        /// <summary>
        /// Profile name (unique identifier).
        /// </summary>
        [Argument(Position = 0, Name = "name", IsRequired = true)]
        [Description("Profile name (must be unique)")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Server URI to connect to.
        /// </summary>
        [Argument(Name = "uri", IsRequired = true)]
        [Alias('u')]
        [Description("Server URI (e.g., https://api.example.com)")]
        public string Uri { get; set; } = string.Empty;

        /// <summary>
        /// API key for authentication (optional, can be added later).
        /// </summary>
        [Argument(Name = "api-key")]
        [Alias('k')]
        [Description("API key for authentication")]
        public string ApiKey { get; set; }

        /// <summary>
        /// Set this profile as the default.
        /// </summary>
        [Argument(Name = "default")]
        [Flag]
        [Description("Set as default profile")]
        public bool SetAsDefault { get; set; }

        [Argument(Name = "allow-path")]
        [Alias('a')]
        [Description("Client paths the server may access without prompting (glob patterns)")]
        public string[] AllowPaths { get; set; }

        [Argument(Name = "consent-mode")]
        [AutoComplete<ConsentModeProvider>]
        [Description("Consent mode for uncovered paths: Prompt (default), AllowAll, or DenyAll")]
        public string ConsentModeArg { get; set; }

        public ProfileAddCommand(IProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // 1. Validate URI format
            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            {
                Console.MarkupLine($"[red]Error:[/] '{Markup.Escape(Uri)}' is an invalid URI. Use format: https://example.com");
                return;
            }

            // 2. Check if profile already exists
            if (await _profileManager.ExistsAsync(Name, ctx.CancellationToken))
            {
                Console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' already exists");
                return;
            }

            // 3. Parse consent mode if provided
            var consentMode = ConsentMode.Prompt;
            if (!string.IsNullOrEmpty(ConsentModeArg))
            {
                if (!Enum.TryParse<ConsentMode>(ConsentModeArg, ignoreCase: true, out consentMode))
                {
                    Console.MarkupLine($"[red]Error:[/] Invalid consent mode '{Markup.Escape(ConsentModeArg)}'. Valid values: Prompt, AllowAll, DenyAll");
                    return;
                }
            }

            // 4. Create the profile
            var profile = new ServerProfile
            {
                Name = Name,
                Uri = Uri,
                ApiKey = ApiKey,
                AllowPaths = AllowPaths?.ToList() ?? new List<string>(),
                ConsentMode = consentMode
            };

            await _profileManager.CreateProfileAsync(profile, ctx.CancellationToken);

            // 5. Set as default if requested
            if (SetAsDefault)
            {
                await _profileManager.SetDefaultProfileAsync(Name, ctx.CancellationToken);
                Console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' created and set as default[/]");
            }
            else
            {
                Console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' created successfully[/]");
            }
        }
    }
}
