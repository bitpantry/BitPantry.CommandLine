using BitPantry.CommandLine.API;

namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.File
{
    /// <summary>
    /// File command group for file transfer operations.
    /// Client-side commands: upload, download
    /// </summary>
    [Group(Name = "file")]
    [Description("Remote file system operations")]
    public class FileGroup { }
}
