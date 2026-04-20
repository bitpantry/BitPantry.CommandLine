using System;
using System.Linq;

namespace BitPantry.CommandLine.Remote.SignalR
{
    /// <summary>
    /// Shared path-security utilities used by both client and server file access layers.
    /// </summary>
    public static class PathSecurity
    {
        /// <summary>
        /// Validates that a glob pattern does not contain path traversal sequences.
        /// Rejects literal ".." segments and URL-encoded variants ("%2e%2e").
        /// </summary>
        /// <param name="pattern">The glob pattern to validate.</param>
        /// <exception cref="ArgumentException">Thrown if the pattern contains path traversal.</exception>
        public static void ValidateNoPathTraversal(string pattern)
        {
            var decoded = Uri.UnescapeDataString(pattern);
            var segments = decoded.Replace('\\', '/').Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
                throw new ArgumentException($"Glob pattern must not contain path traversal: '{pattern}'", nameof(pattern));
        }
    }
}
