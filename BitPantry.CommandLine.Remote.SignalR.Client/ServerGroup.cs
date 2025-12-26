using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Server command group for remote server connection commands.
    /// </summary>
    [Group(Name = "server")]
    [Description("Remote server connection commands")]
    public class ServerGroup
    {
        /// <summary>
        /// Profile command group for managing connection profiles.
        /// </summary>
        [Group(Name = "profile")]
        [Description("Manage connection profiles for remote servers")]
        public class ProfileGroup { }
    }
}
