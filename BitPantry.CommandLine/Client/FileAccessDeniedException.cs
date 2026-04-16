using System;

namespace BitPantry.CommandLine.Client
{
    /// <summary>
    /// Thrown when a file access operation is denied (e.g., insufficient permissions).
    /// </summary>
    public class FileAccessDeniedException : Exception
    {
        public string FilePath { get; }

        public FileAccessDeniedException(string filePath, string message)
            : base(message)
        {
            FilePath = filePath;
        }

        public FileAccessDeniedException(string filePath, string message, Exception innerException)
            : base(message, innerException)
        {
            FilePath = filePath;
        }
    }
}
