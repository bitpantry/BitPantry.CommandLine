namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Represents metadata for a file or directory in the remote file system.
    /// Used for autocomplete and file listing operations.
    /// </summary>
    public class FileMetadata
    {
        /// <summary>
        /// The name of the file or directory (without path).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether this entry represents a directory.
        /// </summary>
        public bool IsDirectory { get; set; }
    }
}
