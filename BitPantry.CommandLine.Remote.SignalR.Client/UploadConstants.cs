namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Constants for upload command configuration.
    /// </summary>
    public static class UploadConstants
    {
        /// <summary>
        /// Maximum number of files to check in a single batch existence request.
        /// </summary>
        public const int BatchExistsChunkSize = 100;

        /// <summary>
        /// Maximum number of concurrent file uploads.
        /// </summary>
        public const int MaxConcurrentUploads = 4;

        /// <summary>
        /// Minimum file size in bytes to show progress display (1 MB).
        /// </summary>
        public const long ProgressDisplayThreshold = 1024 * 1024;

        /// <summary>
        /// Status value returned when a file was skipped on the server.
        /// </summary>
        public const string StatusSkipped = "skipped";

        /// <summary>
        /// Status value returned when a file was successfully uploaded.
        /// </summary>
        public const string StatusUploaded = "uploaded";
    }
}
