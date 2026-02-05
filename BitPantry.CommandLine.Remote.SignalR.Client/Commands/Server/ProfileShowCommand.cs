using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    /// <summary>
    /// Shows details for a specific server connection profile.
    /// Usage: server profile show [name]
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "show")]
    [Description("Show details of a server connection profile")]
    public class ProfileShowCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly IAnsiConsole _console;

        /// <summary>
        /// Profile name to show.
        /// </summary>
        [Argument(Position = 0, Name = "name", IsRequired = true)]
        [Description("Profile name to show")]
        [AutoComplete<ProfileNameProvider>]
        public string Name { get; set; } = string.Empty;

        public ProfileShowCommand(IProfileManager profileManager, IAnsiConsole console)
        {
            _profileManager = profileManager;
            _console = console;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // 1. Get profile by name
            var profile = await _profileManager.GetProfileAsync(Name, ctx.CancellationToken);

            if (profile == null)
            {
                _console.MarkupLine($"[red]Error:[/] Profile '{Markup.Escape(Name)}' not found");
                return;
            }

            // 2. Check if it's the default profile
            var defaultProfileName = await _profileManager.GetDefaultProfileNameAsync(ctx.CancellationToken);
            var isDefault = string.Equals(profile.Name, defaultProfileName, StringComparison.OrdinalIgnoreCase);

            // 3. Check if it has credentials stored
            var hasCredential = await _profileManager.HasCredentialAsync(profile.Name, ctx.CancellationToken);

            // 4. Display profile details
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.Title($"[bold]Profile: {Markup.Escape(profile.Name)}[/]");
            table.AddColumn("Property");
            table.AddColumn("Value");

            table.AddRow("Name", Markup.Escape(profile.Name));
            table.AddRow("URI", Markup.Escape(profile.Uri));
            table.AddRow("Default", isDefault ? "[green]Yes[/]" : "No");
            table.AddRow("API Key", hasCredential ? "[green]Configured[/]" : "[dim]Not set[/]");

            _console.Write(table);
        }
    }
}
