using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server.Profiles
{
    /// <summary>
    /// Removes a server connection profile.
    /// Usage: server profile remove [name]
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "remove")]
    [Description("Remove a server connection profile")]
    public class ProfileRemoveCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly IAnsiConsole _console;

        /// <summary>
        /// Profile name to remove.
        /// </summary>
        [Argument(Position = 0, Name = "name", IsRequired = true)]
        [Description("Profile name to remove")]
        [AutoComplete<ProfileNameProvider>]
        public string Name { get; set; } = string.Empty;

        public ProfileRemoveCommand(IProfileManager profileManager, IAnsiConsole console)
        {
            _profileManager = profileManager;
            _console = console;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // 1. Check if profile exists
            var exists = await _profileManager.ExistsAsync(Name, ctx.CancellationToken);

            if (!exists)
            {
                _console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' not found");
                return;
            }

            // 2. Check if this profile is the default - if so, clear the default setting
            var defaultName = await _profileManager.GetDefaultProfileNameAsync(ctx.CancellationToken);
            if (string.Equals(Name, defaultName, StringComparison.OrdinalIgnoreCase))
            {
                await _profileManager.SetDefaultProfileAsync(null, ctx.CancellationToken);
            }

            // 3. Delete the profile (this also removes credentials)
            var deleted = await _profileManager.DeleteProfileAsync(Name, ctx.CancellationToken);

            if (deleted)
            {
                _console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' removed[/]");
            }
            else
            {
                _console.MarkupLine($"[red]Error:[/] Failed to remove profile '{Markup.Escape(Name)}'");
            }
        }
    }
}
