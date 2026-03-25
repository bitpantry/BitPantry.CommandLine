using System;
using System.IO;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Configuration options for file transfer operations
    /// </summary>
    public class FileTransferOptions
    {
        /// <summary>
        /// The default subdirectory name for CLI file storage.
        /// </summary>
        public const string DefaultStorageDirectoryName = "cli-files";

        /// <summary>
        /// The root path where files will be stored on the server.
        /// All file operations are restricted to this directory.
        /// Default is a 'cli-files' subdirectory in the application's base directory.
        /// </summary>
        public string StorageRootPath { get; set; } = Path.Combine(AppContext.BaseDirectory, DefaultStorageDirectoryName);

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
        /// Gets a value indicating whether file transfer features are enabled.
        /// When false, file transfer endpoints and commands are not registered.
        /// </summary>
        public bool IsEnabled { get; private set; } = true;

        /// <summary>
        /// Disables file transfer features. When disabled, file transfer endpoints and commands
        /// are not registered, and validation is skipped.
        /// </summary>
        /// <returns>This instance for method chaining.</returns>
        public FileTransferOptions Disable()
        {
            IsEnabled = false;
            return this;
        }

        /// <summary>
        /// Validates the configuration options.
        /// Validation is skipped when file transfers are disabled.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when StorageRootPath is null or empty (only when enabled)</exception>
        /// <exception cref="ArgumentException">Thrown when MaxFileSizeBytes is less than or equal to zero (only when enabled)</exception>
        public void Validate()
        {
            // Skip validation if file transfer is disabled
            if (!IsEnabled)
            {
                return;
            }

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
