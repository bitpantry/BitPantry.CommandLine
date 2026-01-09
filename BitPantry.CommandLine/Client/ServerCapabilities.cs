using BitPantry.CommandLine.Component;
using System;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Represents the capabilities and connection information received from a connected server.
    /// This class consolidates all server-provided state, making it easy to manage connection lifecycle.
    /// </summary>
    public class ServerCapabilities
    {
        /// <summary>
        /// The URI of the connected server.
        /// </summary>
        public Uri ConnectionUri { get; }

        /// <summary>
        /// The server-assigned connection ID.
        /// </summary>
        public string ConnectionId { get; }

        /// <summary>
        /// The list of commands available on the server.
        /// </summary>
        public IReadOnlyList<CommandInfo> Commands { get; }

        /// <summary>
        /// The maximum file size in bytes that the server accepts for uploads.
        /// </summary>
        public long MaxFileSizeBytes { get; }

        /// <summary>
        /// Creates a new ServerCapabilities instance.
        /// </summary>
        /// <param name="connectionUri">The server connection URI.</param>
        /// <param name="connectionId">The server-assigned connection ID.</param>
        /// <param name="commands">The list of commands available on the server.</param>
        /// <param name="maxFileSizeBytes">The maximum file upload size in bytes.</param>
        public ServerCapabilities(Uri connectionUri, string connectionId, IReadOnlyList<CommandInfo> commands, long maxFileSizeBytes)
        {
            ConnectionUri = connectionUri ?? throw new ArgumentNullException(nameof(connectionUri));
            ConnectionId = connectionId ?? throw new ArgumentNullException(nameof(connectionId));
            Commands = commands ?? throw new ArgumentNullException(nameof(commands));
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        /// <summary>
        /// Formats a file size in bytes to a human-readable string (e.g., "100 MB", "1.5 GB").
        /// </summary>
        /// <param name="bytes">The size in bytes.</param>
        /// <returns>A human-readable file size string.</returns>
        public static string FormatFileSize(long bytes)
        {
            const long KB = 1024;
            const long MB = KB * 1024;
            const long GB = MB * 1024;

            if (bytes >= GB)
            {
                var gb = (double)bytes / GB;
                return gb % 1 == 0 ? $"{(long)gb} GB" : $"{gb:F1} GB";
            }
            if (bytes >= MB)
            {
                var mb = (double)bytes / MB;
                return mb % 1 == 0 ? $"{(long)mb} MB" : $"{mb:F1} MB";
            }
            if (bytes >= KB)
            {
                var kb = (double)bytes / KB;
                return kb % 1 == 0 ? $"{(long)kb} KB" : $"{kb:F1} KB";
            }
            return $"{bytes} bytes";
        }
    }
}
