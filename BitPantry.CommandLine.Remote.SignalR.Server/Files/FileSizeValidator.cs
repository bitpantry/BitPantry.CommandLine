namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Exception thrown when a file exceeds the configured size limit.
    /// </summary>
    public class FileSizeLimitExceededException : Exception
    {
        public long FileSize { get; }
        public long MaxSize { get; }

        public FileSizeLimitExceededException(long fileSize, long maxSize)
            : base($"File size {fileSize} bytes exceeds the maximum allowed limit of {maxSize} bytes.")
        {
            FileSize = fileSize;
            MaxSize = maxSize;
        }

        public FileSizeLimitExceededException(long fileSize, long maxSize, string message)
            : base(message)
        {
            FileSize = fileSize;
            MaxSize = maxSize;
        }
    }

    /// <summary>
    /// Validates file sizes against configured limits.
    /// </summary>
    public class FileSizeValidator
    {
        private readonly FileTransferOptions _options;

        public FileSizeValidator(FileTransferOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Validates the Content-Length header value against the maximum file size.
        /// </summary>
        /// <param name="contentLength">The content length from the request header, or null if not provided</param>
        /// <exception cref="FileSizeLimitExceededException">Thrown if content length exceeds the limit</exception>
        public void ValidateContentLength(long? contentLength)
        {
            if (contentLength.HasValue && contentLength.Value > _options.MaxFileSizeBytes)
            {
                throw new FileSizeLimitExceededException(
                    contentLength.Value,
                    _options.MaxFileSizeBytes,
                    $"Content-Length {contentLength.Value} bytes exceeds the maximum allowed limit of {_options.MaxFileSizeBytes} bytes.");
            }
        }

        /// <summary>
        /// Validates accumulated bytes during streaming against the maximum file size.
        /// Call this periodically during file upload to abort early if limit is exceeded.
        /// </summary>
        /// <param name="totalBytesRead">Total bytes read so far</param>
        /// <exception cref="FileSizeLimitExceededException">Thrown if bytes read exceeds the limit</exception>
        public void ValidateStreamingBytes(long totalBytesRead)
        {
            if (totalBytesRead > _options.MaxFileSizeBytes)
            {
                throw new FileSizeLimitExceededException(
                    totalBytesRead,
                    _options.MaxFileSizeBytes,
                    $"Upload size {totalBytesRead} bytes exceeds the maximum allowed limit of {_options.MaxFileSizeBytes} bytes.");
            }
        }
    }
}
