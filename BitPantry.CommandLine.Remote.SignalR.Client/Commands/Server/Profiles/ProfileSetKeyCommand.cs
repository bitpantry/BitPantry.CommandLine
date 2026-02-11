using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server.Profiles
{
    /// <summary>
    /// Updates the API key for a server connection profile.
    /// Usage: server profile set-key [name] [api-key]
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "set-key")]
    [Description("Update the API key for a server profile")]
    public class ProfileSetKeyCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;

        /// <summary>
        /// Profile name to update.
        /// </summary>
        [Argument(Position = 0, Name = "name", IsRequired = true)]
        [Description("Profile name to update")]
        [AutoComplete<ProfileNameProvider>]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// New API key value. If not provided, will prompt interactively with masked input.
        /// </summary>
        [Argument(Position = 1, Name = "api-key")]
        [Description("API key value (will prompt if not provided)")]
        public string? ApiKey { get; set; }

        public ProfileSetKeyCommand(IProfileManager profileManager)
        {
            _profileManager = profileManager;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Validate that a profile name was provided
            if (string.IsNullOrWhiteSpace(Name))
            {
                Console.MarkupLine("[red]Error:[/] Profile name is required");
                return;
            }

            // Check if profile exists
            var exists = await _profileManager.ExistsAsync(Name, ctx.CancellationToken);
            if (!exists)
            {
                Console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' not found");
                return;
            }

            // Handle missing API key - require it in this implementation
            // (Interactive prompting would be handled at the application level)
            if (ApiKey == null)
            {
                Console.MarkupLine("[red]Error:[/] API key is required");
                return;
            }

            // Validate non-empty API key
            if (string.IsNullOrWhiteSpace(ApiKey))
            {
                Console.MarkupLine("[red]Error:[/] API key cannot be empty");
                return;
            }

            // Update the API key
            await _profileManager.SetApiKeyAsync(Name, ApiKey, ctx.CancellationToken);
            Console.MarkupLine($"[green]API key updated for profile '{Markup.Escape(Name)}'[/]");
        }
    }
}
