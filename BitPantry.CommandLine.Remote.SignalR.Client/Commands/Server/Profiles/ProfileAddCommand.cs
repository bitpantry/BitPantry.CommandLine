using BitPantry.CommandLine.API;
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
        private readonly IAnsiConsole _console;

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
        public string? ApiKey { get; set; }

        /// <summary>
        /// Set this profile as the default.
        /// </summary>
        [Argument(Name = "default")]
        [Flag]
        [Description("Set as default profile")]
        public bool SetAsDefault { get; set; }

        public ProfileAddCommand(IProfileManager profileManager, IAnsiConsole console)
        {
            _profileManager = profileManager;
            _console = console;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // 1. Validate URI format
            if (!System.Uri.TryCreate(Uri, UriKind.Absolute, out var parsedUri) ||
                (parsedUri.Scheme != "http" && parsedUri.Scheme != "https"))
            {
                _console.MarkupLine($"[red]Error:[/] '{Markup.Escape(Uri)}' is an invalid URI. Use format: https://example.com");
                return;
            }

            // 2. Check if profile already exists
            if (await _profileManager.ExistsAsync(Name, ctx.CancellationToken))
            {
                _console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' already exists");
                return;
            }

            // 3. Create the profile
            var profile = new ServerProfile
            {
                Name = Name,
                Uri = Uri,
                ApiKey = ApiKey
            };

            await _profileManager.CreateProfileAsync(profile, ctx.CancellationToken);

            // 4. Set as default if requested
            if (SetAsDefault)
            {
                await _profileManager.SetDefaultProfileAsync(Name, ctx.CancellationToken);
                _console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' created and set as default[/]");
            }
            else
            {
                _console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' created successfully[/]");
            }
        }
    }
}
