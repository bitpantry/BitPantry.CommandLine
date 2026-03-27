using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server.Profiles
{
    /// <summary>
    /// Lists all saved server connection profiles.
    /// Usage: server profile list
    /// </summary>
    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "list")]
    [Description("List all saved server connection profiles")]
    public class ProfileListCommand : CommandBase
    {
        private readonly IProfileManager _profileManager;
        private readonly Theme _theme;

        public ProfileListCommand(IProfileManager profileManager, Theme theme)
        {
            _profileManager = profileManager;
            _theme = theme;
        }

        public async Task Execute(CommandExecutionContext ctx)
        {
            // Get all profiles
            var profiles = await _profileManager.GetAllProfilesAsync(ctx.CancellationToken);

            if (profiles.Count == 0)
            {
                Console.MarkupLine("[yellow]No profiles configured[/]");
                Console.MarkupLine("Use [blue]server profile add[/] to create a profile.");
                return;
            }

            // Get default profile name
            var defaultProfileName = await _profileManager.GetDefaultProfileNameAsync(ctx.CancellationToken);

            // Build table with borderless styling and themed headers
            var table = new Table();
            table.Border(TableBorder.None);
            table.AddColumn(new TableColumn($"[{_theme.TableHeader.ToMarkup()}]Name[/]") { Padding = new Padding(0, 0, 3, 0) });
            table.AddColumn(new TableColumn($"[{_theme.TableHeader.ToMarkup()}]URI[/]") { Padding = new Padding(0, 0, 3, 0) });
            table.AddColumn(new TableColumn($"[{_theme.TableHeader.ToMarkup()}]Default[/]") { Padding = new Padding(0, 0, 3, 0) });
            table.AddColumn(new TableColumn($"[{_theme.TableHeader.ToMarkup()}]API Key[/]"));

            foreach (var profile in profiles)
            {
                var isDefault = string.Equals(profile.Name, defaultProfileName, StringComparison.OrdinalIgnoreCase);
                var hasCredential = await _profileManager.HasCredentialAsync(profile.Name, ctx.CancellationToken);

                table.AddRow(
                    profile.Name,
                    profile.Uri,
                    isDefault ? "[green]Yes[/]" : "No",
                    hasCredential ? "[green]Yes[/]" : "No"
                );
            }

            Console.Write(table);
        }
    }
}
