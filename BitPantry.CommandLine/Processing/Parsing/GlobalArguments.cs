using System.Collections.Generic;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Holds global arguments that are extracted from input before command routing.
    /// Global arguments apply to the entire application, not to specific commands.
    /// New global arguments can be added here as needed.
    /// </summary>
    public class GlobalArguments
    {
        /// <summary>
        /// The profile name specified via --profile or -P.
        /// Null if not specified.
        /// </summary>
        public string ProfileName { get; set; }

        /// <summary>
        /// Whether --help or -h was specified.
        /// </summary>
        public bool HelpRequested { get; set; }

        /// <summary>
        /// Returns the names of all reserved global argument names.
        /// Commands cannot use these as argument names.
        /// </summary>
        public static IReadOnlyList<string> ReservedNames { get; } = new[] { "profile", "help" };

        /// <summary>
        /// Returns the aliases of all reserved global argument aliases.
        /// Commands cannot use these as argument aliases.
        /// </summary>
        public static IReadOnlyList<char> ReservedAliases { get; } = new[] { 'p', 'P', 'h', 'H' };
    }
}
