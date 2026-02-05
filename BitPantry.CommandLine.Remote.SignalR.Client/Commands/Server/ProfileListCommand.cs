using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
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
        private readonly IAnsiConsole _console;

        public ProfileListCommand(IProfileManager profileManager, IAnsiConsole console)
        {
            _profileManager = profileManager;
            _console = console;
        }

        public Task Execute(CommandExecutionContext ctx)
        {
            // TODO: Implement in later batch
            throw new NotImplementedException("ProfileListCommand.Execute not yet implemented");
        }
    }
}
