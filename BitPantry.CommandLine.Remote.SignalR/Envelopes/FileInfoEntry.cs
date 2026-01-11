using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Represents a remote file with metadata needed for download operations.
    /// Used in EnumerateFilesResponse to provide file information including size for progress calculation.
    /// </summary>
    public class FileInfoEntry
    {
        /// <summary>
        /// Relative path from search root. Uses forward slashes as canonical separator.
        /// </summary>
        [JsonPropertyName("Path")]
        public string Path { get; set; }

        /// <summary>
        /// File size in bytes.
        /// </summary>
        [JsonPropertyName("Size")]
        public long Size { get; set; }

        /// <summary>
        /// Last write time (UTC).
        /// </summary>
        [JsonPropertyName("LastModified")]
        public DateTime LastModified { get; set; }

        public FileInfoEntry() { }

        public FileInfoEntry(string path, long size, DateTime lastModified)
        {
            Path = path;
            Size = size;
            LastModified = lastModified;
        }
    }
}
