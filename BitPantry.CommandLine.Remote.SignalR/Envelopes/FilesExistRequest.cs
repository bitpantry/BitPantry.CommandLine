namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Request payload for batch file existence check.
    /// </summary>
    /// <param name="Directory">Remote directory path to check files in.</param>
    /// <param name="Filenames">Array of filenames to check for existence.</param>
    public record FilesExistRequest(string Directory, string[] Filenames);
}
