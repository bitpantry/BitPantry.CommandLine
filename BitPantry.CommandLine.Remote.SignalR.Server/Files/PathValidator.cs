using System;
using System.IO;
using System.Web;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Files
{
    /// <summary>
    /// Validates file paths to ensure they don't escape the configured storage root.
    /// Prevents path traversal attacks using ../ sequences or absolute paths.
    /// </summary>
    public class PathValidator
    {
        private readonly string _storageRoot;

        /// <summary>
        /// Creates a new PathValidator with the specified storage root.
        /// </summary>
        /// <param name="storageRoot">The root directory that all paths must stay within</param>
        public PathValidator(string storageRoot)
        {
            if (string.IsNullOrWhiteSpace(storageRoot))
                throw new ArgumentException("Storage root cannot be null or empty", nameof(storageRoot));

            // Normalize the storage root to use consistent path separators and resolve any relative components
            _storageRoot = Path.GetFullPath(storageRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        }

        /// <summary>
        /// Validates the given path and returns the full path if valid.
        /// </summary>
        /// <param name="relativePath">The path to validate (can be relative or absolute)</param>
        /// <returns>The full validated path within the storage root</returns>
        /// <exception cref="ArgumentNullException">If path is null</exception>
        /// <exception cref="ArgumentException">If path is empty or whitespace</exception>
        /// <exception cref="UnauthorizedAccessException">If path would escape the storage root</exception>
        public string ValidatePath(string relativePath)
        {
            if (relativePath == null)
                throw new ArgumentNullException(nameof(relativePath));

            if (string.IsNullOrWhiteSpace(relativePath))
                throw new ArgumentException("Path cannot be empty or whitespace", nameof(relativePath));

            // Decode any URL-encoded characters (e.g., %2e%2e%2f for ../)
            var decodedPath = HttpUtility.UrlDecode(relativePath);

            // Normalize: treat leading / or \ as relative to sandbox root, not filesystem root
            // This allows users to use "/" to mean "root of the sandbox"
            decodedPath = decodedPath.TrimStart('/', '\\');

            // If the path is now empty (was just "/" or "\"), return the storage root
            if (string.IsNullOrEmpty(decodedPath))
            {
                return _storageRoot;
            }

            // Combine with the storage root and get the full path
            // This will resolve any ../ or ./ sequences
            string combinedPath;
            if (Path.IsPathRooted(decodedPath))
            {
                // If absolute path (e.g., C:\Windows\...), just use it directly for validation
                combinedPath = Path.GetFullPath(decodedPath);
            }
            else
            {
                // If relative, combine with storage root
                combinedPath = Path.GetFullPath(Path.Combine(_storageRoot, decodedPath));
            }

            // Ensure the resulting path is within the storage root
            // Use ordinal comparison for case-insensitive check on Windows
            var comparison = OperatingSystem.IsWindows() 
                ? StringComparison.OrdinalIgnoreCase 
                : StringComparison.Ordinal;

            // The path must start with the storage root followed by a path separator (or be exactly the root)
            if (!combinedPath.Equals(_storageRoot, comparison) &&
                !combinedPath.StartsWith(_storageRoot + Path.DirectorySeparatorChar, comparison))
            {
                throw new UnauthorizedAccessException($"Path '{relativePath}' resolves outside the allowed storage root.");
            }

            return combinedPath;
        }
    }
}
