using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Commands.File
{
    /// <summary>
    /// File command group for remote file system operations.
    /// Server-side commands: ls, rm, mkdir, cat, info
    /// </summary>
    [Group(Name = "file")]
    [Description("Remote file system operations")]
    public class FileGroup { }
}
