namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Configuration options for file transfer operations
    /// </summary>
    public class FileTransferOptions
    {
        /// <summary>
        /// The root path where files will be stored on the server.
        /// All file operations are restricted to this directory.
        /// </summary>
        public string StorageRootPath { get; set; }

        /// <summary>
        /// Maximum allowed file size in bytes. Default is 100MB.
        /// </summary>
        public long MaxFileSizeBytes { get; set; } = 100 * 1024 * 1024; // 100MB default

        /// <summary>
        /// Allowed file extensions. If null, all extensions are allowed.
        /// Extensions should include the leading dot (e.g., ".txt", ".pdf")
        /// </summary>
        public string[] AllowedExtensions { get; set; }

        /// <summary>
        /// Validates the configuration options
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when StorageRootPath is null or empty</exception>
        /// <exception cref="ArgumentException">Thrown when MaxFileSizeBytes is less than or equal to zero</exception>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(StorageRootPath))
            {
                throw new InvalidOperationException("StorageRootPath must be configured. File transfer operations require a valid storage directory.");
            }

            if (MaxFileSizeBytes <= 0)
            {
                throw new ArgumentException("MaxFileSizeBytes must be greater than zero.", nameof(MaxFileSizeBytes));
            }
        }
    }
}
