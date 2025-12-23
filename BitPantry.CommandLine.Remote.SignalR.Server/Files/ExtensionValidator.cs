namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Exception thrown when a file has an extension that is not in the allowed list.
    /// </summary>
    public class FileExtensionNotAllowedException : Exception
    {
        public string Extension { get; }
        public string[] AllowedExtensions { get; }

        public FileExtensionNotAllowedException(string extension, string[] allowedExtensions)
            : base($"File extension '{extension}' is not allowed. Allowed extensions: {string.Join(", ", allowedExtensions)}")
        {
            Extension = extension;
            AllowedExtensions = allowedExtensions;
        }
    }

    /// <summary>
    /// Validates file extensions against configured allowed list.
    /// </summary>
    public class ExtensionValidator
    {
        private readonly FileTransferOptions _options;

        public ExtensionValidator(FileTransferOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Validates that the file has an allowed extension.
        /// </summary>
        /// <param name="filePath">The file path to validate (can include directories)</param>
        /// <exception cref="FileExtensionNotAllowedException">Thrown if extension is not in allowed list</exception>
        public void ValidateExtension(string filePath)
        {
            // If no allowed extensions configured, all extensions are allowed
            if (_options.AllowedExtensions == null)
                return;

            // Get the file extension (including the dot)
            var extension = Path.GetExtension(filePath);

            // Check if the extension is in the allowed list (case insensitive)
            var isAllowed = _options.AllowedExtensions.Any(allowed =>
                string.Equals(allowed, extension, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                throw new FileExtensionNotAllowedException(
                    string.IsNullOrEmpty(extension) ? "(no extension)" : extension,
                    _options.AllowedExtensions);
            }
        }
    }
}
