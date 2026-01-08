using System.Collections.Generic;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Result of a file listing operation containing file and directory metadata.
    /// </summary>
    public class FileListingResult
    {
        /// <summary>
        /// List of files and/or directories matching the request.
        /// </summary>
        public List<FileMetadata> Items { get; set; } = new List<FileMetadata>();

        /// <summary>
        /// Whether an error occurred during the listing operation.
        /// </summary>
        public bool IsError { get; set; }

        /// <summary>
        /// Error message if IsError is true.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
