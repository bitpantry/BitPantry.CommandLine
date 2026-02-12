using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Result of pattern validation.
    /// </summary>
    public class PatternValidationResult
    {
        public bool IsValid { get; }
        public string ErrorMessage { get; }
        public string SuggestedFormat { get; }

        private PatternValidationResult(bool isValid, string errorMessage = null, string suggestedFormat = null)
        {
            IsValid = isValid;
            ErrorMessage = errorMessage;
            SuggestedFormat = suggestedFormat;
        }

        public static PatternValidationResult Success() => new(true);

        public static PatternValidationResult Error(string message, string suggestedFormat) 
            => new(false, message, suggestedFormat);
    }

    /// <summary>
    /// Utility class for handling glob patterns and path operations.
    /// Used by both UploadCommand and DownloadCommand for pattern matching.
    /// </summary>
    public static class GlobPatternHelper
    {
        private const string ValidPatternFormats = "Valid formats: 'file.txt', '*.txt', 'folder/*.log', '**/*.json'";

        /// <summary>
        /// Validates a glob pattern for correctness.
        /// </summary>
        /// <param name="pattern">The pattern to validate.</param>
        /// <returns>Validation result with error message and suggested format if invalid.</returns>
        public static PatternValidationResult ValidatePattern(string pattern)
        {
            // Check for empty or whitespace
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return PatternValidationResult.Error(
                    "Source pattern cannot be empty or whitespace.",
                    ValidPatternFormats);
            }

            return PatternValidationResult.Success();
        }

        /// <summary>
        /// Checks if a path contains glob wildcard characters (* or ?).
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path contains glob characters, false otherwise.</returns>
        public static bool ContainsGlobCharacters(string path)
        {
            return path.Contains('*') || path.Contains('?');
        }

        /// <summary>
        /// Parses a glob pattern to extract the base directory and pattern portion.
        /// Normalizes path separators and splits at the first segment containing wildcards.
        /// </summary>
        /// <param name="source">The glob pattern source path.</param>
        /// <param name="fileSystem">The file system abstraction for path operations.</param>
        /// <returns>A tuple containing the base directory and the pattern.</returns>
        public static (string baseDir, string pattern) ParseGlobPattern(string source, IFileSystem fileSystem)
        {
            // Normalize path separators to forward slashes for consistent parsing
            var normalizedSource = source.Replace('\\', '/');
            var segments = normalizedSource.Split('/');

            var baseSegments = new List<string>();
            var patternSegments = new List<string>();
            var inPattern = false;

            foreach (var seg in segments)
            {
                if (inPattern || seg.Contains('*') || seg.Contains('?'))
                {
                    inPattern = true;
                    patternSegments.Add(seg);
                }
                else
                {
                    baseSegments.Add(seg);
                }
            }

            var baseDir = baseSegments.Count > 0
                ? string.Join(fileSystem.Path.DirectorySeparatorChar.ToString(), baseSegments)
                : fileSystem.Directory.GetCurrentDirectory();

            // Handle empty base dir for patterns like "*.txt"
            if (string.IsNullOrWhiteSpace(baseDir))
            {
                baseDir = fileSystem.Directory.GetCurrentDirectory();
            }

            var pattern = string.Join("/", patternSegments);

            return (baseDir, pattern);
        }

        /// <summary>
        /// Converts a glob pattern to a regex for post-filtering.
        /// Handles * (any characters) and ? (single character) wildcards.
        /// Uses case-insensitive matching for cross-platform safety.
        /// </summary>
        /// <param name="pattern">The glob pattern to convert.</param>
        /// <returns>A regex that matches the pattern.</returns>
        public static Regex GlobPatternToRegex(string pattern)
        {
            // Extract just the filename pattern (last segment)
            var segments = pattern.Replace('\\', '/').Split('/');
            var filePattern = segments[^1];
            
            // Escape regex special characters except our glob wildcards
            var regexPattern = Regex.Escape(filePattern)
                .Replace("\\*", ".*")    // * -> .* (any characters)
                .Replace("\\?", ".");     // ? -> . (single character)
            
            return new Regex($"^{regexPattern}$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Resolves a destination path by appending the source filename if destination ends with a separator.
        /// Used by both UploadCommand and DownloadCommand for consistent path resolution.
        /// </summary>
        /// <param name="destination">The destination path (directory or file).</param>
        /// <param name="sourceFileName">The source file name to append if destination is a directory.</param>
        /// <returns>The resolved destination path.</returns>
        public static string ResolveDestinationPath(string destination, string sourceFileName)
        {
            if (destination.EndsWith('/') || destination.EndsWith('\\'))
            {
                // Destination is a directory - append the source filename
                return destination.TrimEnd('/', '\\') + "/" + sourceFileName;
            }
            // Destination is a specific filename
            return destination;
        }

        /// <summary>
        /// Reconstructs a full path by prepending a base directory to a relative path.
        /// Normalizes to forward slashes for server-side paths.
        /// </summary>
        /// <param name="baseDir">The base directory (e.g., "logs").</param>
        /// <param name="relativePath">The relative path from the base (e.g., "app.log").</param>
        /// <returns>The reconstructed full path (e.g., "logs/app.log").</returns>
        public static string ReconstructFullPath(string baseDir, string relativePath)
        {
            if (string.IsNullOrEmpty(baseDir) || baseDir == ".")
            {
                return relativePath;
            }
            
            var baseDirNormalized = baseDir.Replace('\\', '/').TrimEnd('/');
            var relativePathNormalized = relativePath.TrimStart('/');
            return baseDirNormalized + "/" + relativePathNormalized;
        }

        /// <summary>
        /// Filters a collection of file entries using a question-mark wildcard pattern.
        /// Microsoft.Extensions.FileSystemGlobbing does NOT support ? wildcards, so this
        /// applies regex post-filtering for single-character matching.
        /// </summary>
        /// <typeparam name="T">The type of file entry.</typeparam>
        /// <param name="files">The files to filter.</param>
        /// <param name="pattern">The glob pattern containing ? wildcards.</param>
        /// <param name="getFileName">Function to extract filename from the entry.</param>
        /// <returns>Filtered collection matching the ? wildcard pattern.</returns>
        public static IEnumerable<T> ApplyQuestionMarkFilter<T>(
            IEnumerable<T> files, 
            string pattern, 
            Func<T, string> getFileName)
        {
            if (!pattern.Contains('?'))
            {
                return files;
            }

            var regex = GlobPatternToRegex(pattern);
            return files.Where(f => regex.IsMatch(getFileName(f)));
        }
    }
}
