using System.Text;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Client-side rule engine that determines whether a server-initiated file access request
    /// requires user consent based on allow-path patterns and the configured consent mode.
    /// </summary>
    public class FileAccessConsentPolicy
    {
        private readonly List<string> _allowedPatterns = new();
        private readonly List<Regex> _compiledPatterns = new();

        /// <summary>
        /// Controls how uncovered paths (not matching any allow pattern) are handled.
        /// Default is Prompt.
        /// </summary>
        public ConsentMode Mode { get; set; } = ConsentMode.Prompt;

        /// <summary>
        /// Sets the allowed path patterns, replacing any previously configured patterns.
        /// Patterns support *, **, and ? glob wildcards.
        /// </summary>
        /// <param name="patterns">Glob patterns for allowed paths.</param>
        public void SetAllowedPatterns(IEnumerable<string> patterns)
        {
            _allowedPatterns.Clear();
            _compiledPatterns.Clear();

            foreach (var pattern in patterns)
            {
                _allowedPatterns.Add(pattern);
                _compiledPatterns.Add(GlobToRegex(pattern));
            }
        }

        /// <summary>
        /// Appends additional allowed patterns without replacing existing ones.
        /// Used for session-scoped remembered consent and merging profile + CLI patterns.
        /// </summary>
        public void AddPatterns(IEnumerable<string> patterns)
        {
            foreach (var pattern in patterns)
            {
                _allowedPatterns.Add(pattern);
                _compiledPatterns.Add(GlobToRegex(pattern));
            }
        }

        /// <summary>
        /// Returns true if the given path is allowed — either by matching a pattern
        /// or because the consent mode is AllowAll.
        /// </summary>
        public bool IsAllowed(string path)
        {
            if (Mode == ConsentMode.AllowAll)
                return true;

            if (_compiledPatterns.Count == 0)
                return false;

            var normalizedPath = NormalizePath(path);
            return _compiledPatterns.Any(regex => regex.IsMatch(normalizedPath));
        }

        /// <summary>
        /// Returns true if the path should be silently denied (DenyAll mode, uncovered path).
        /// </summary>
        public bool IsDenied(string path)
        {
            if (Mode != ConsentMode.DenyAll)
                return false;

            return !IsAllowedByPatterns(path);
        }

        /// <summary>
        /// Returns true if the given path requires an interactive user consent prompt.
        /// Only true in Prompt mode for uncovered paths.
        /// </summary>
        public bool RequiresConsent(string path)
        {
            if (Mode == ConsentMode.AllowAll)
                return false;
            if (Mode == ConsentMode.DenyAll)
                return false;

            return !IsAllowedByPatterns(path);
        }

        /// <summary>
        /// Returns the subset of paths that require an interactive consent prompt.
        /// </summary>
        public IReadOnlyList<string> GetPathsRequiringConsent(IEnumerable<string> paths)
            => paths.Where(RequiresConsent).ToList();

        private bool IsAllowedByPatterns(string path)
        {
            if (_compiledPatterns.Count == 0)
                return false;

            var normalizedPath = NormalizePath(path);
            return _compiledPatterns.Any(regex => regex.IsMatch(normalizedPath));
        }

        /// <summary>
        /// Normalizes a path by replacing backslashes with forward slashes for consistent matching.
        /// </summary>
        private static string NormalizePath(string path) => path.Replace('\\', '/');

        /// <summary>
        /// Converts a glob pattern to a regex for full-path matching.
        /// Supports *, **, and ? wildcards with case-insensitive matching.
        /// </summary>
        private static Regex GlobToRegex(string pattern)
        {
            var normalized = NormalizePath(pattern);
            var regex = new StringBuilder();
            regex.Append('^');

            int i = 0;
            while (i < normalized.Length)
            {
                char c = normalized[i];

                if (c == '*')
                {
                    if (i + 1 < normalized.Length && normalized[i + 1] == '*')
                    {
                        // ** matches any characters including path separators
                        regex.Append(".*");
                        i += 2;

                        // Skip trailing / after ** so that dir/** matches dir/file (zero subdirs)
                        if (i < normalized.Length && normalized[i] == '/')
                            i++;

                        continue;
                    }

                    // * matches any characters except path separator
                    regex.Append("[^/]*");
                }
                else if (c == '?')
                {
                    // ? matches a single character (not a path separator)
                    regex.Append("[^/]");
                }
                else
                {
                    regex.Append(Regex.Escape(c.ToString()));
                }

                i++;
            }

            regex.Append('$');

            return new Regex(regex.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }
    }
}
