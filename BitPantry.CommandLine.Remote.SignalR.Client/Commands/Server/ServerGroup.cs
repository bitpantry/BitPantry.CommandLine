using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    /// <summary>
    /// Server command group for remote server connection commands.
    /// </summary>
    [Group(Name = "server")]
    [Description("Remote server connection commands")]
    public class ServerGroup 
    {
        /// <summary>
        /// Profile command group for managing server connection profiles.
        /// </summary>
        [Group(Name = "profile")]
        [Description("Manage server connection profiles")]
        public class ProfileGroup { }
    }
}
