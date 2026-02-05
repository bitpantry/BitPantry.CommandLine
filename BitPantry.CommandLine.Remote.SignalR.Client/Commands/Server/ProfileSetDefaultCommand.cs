using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    /// <summary>
    /// Sets or clears the default server connection profile.
    /// Usage: server profile set-default [name] [--none]
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "set-default")]
    [Description("Set or clear the default server profile")]
    public class ProfileSetDefaultCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly IAnsiConsole _console;

        /// <summary>
        /// Profile name to set as default.
        /// </summary>
        [Argument(Position = 0, Name = "name", IsRequired = false)]
        [Description("Profile name to set as default")]
        [AutoComplete<ProfileNameProvider>]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Clear the default profile setting.
        /// </summary>
        [Argument(Name = "none")]
        [Alias('n')]
        [Flag]
        [Description("Clear the default profile")]
        public bool ClearDefault { get; set; }

        public ProfileSetDefaultCommand(IProfileManager profileManager, IAnsiConsole console)
        {
            _profileManager = profileManager;
            _console = console;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Handle --none flag to clear the default
            if (ClearDefault)
            {
                await _profileManager.SetDefaultProfileAsync(null, ctx.CancellationToken);
                _console.MarkupLine("[green]Default profile cleared[/]");
                return;
            }

            // Validate that a profile name was provided
            if (string.IsNullOrWhiteSpace(Name))
            {
                _console.MarkupLine("[red]Error:[/] Profile name is required (or use --none to clear default)");
                return;
            }

            // Check if profile exists
            var exists = await _profileManager.ExistsAsync(Name, ctx.CancellationToken);
            if (!exists)
            {
                _console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' not found");
                return;
            }

            // Set the profile as default
            await _profileManager.SetDefaultProfileAsync(Name, ctx.CancellationToken);
            _console.MarkupLine($"[green]Profile '{Markup.Escape(Name)}' set as default[/]");
        }
    }
}
