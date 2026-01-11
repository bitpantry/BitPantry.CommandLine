namespace BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server
{
    /// <summary>
    /// Constants for download command configuration.
    /// </summary>
    public static class DownloadConstants
    {
        /// <summary>
        /// Maximum number of concurrent file downloads.
        /// </summary>
        public const int MaxConcurrentDownloads = 4;

        /// <summary>
        /// Minimum total size in bytes to show progress display (25 MB).
        /// For single file downloads, this is the file size.
        /// For multi-file downloads, this is the total size of all files.
        /// </summary>
        public const long ProgressDisplayThreshold = 25 * 1024 * 1024;

        /// <summary>
        /// Streaming buffer size for file downloads (80KB).
        /// </summary>
        public const int ChunkSize = 81920;

        /// <summary>
        /// Minimum milliseconds between progress updates.
        /// </summary>
        public const int ProgressThrottleMs = 100;
    }
}
