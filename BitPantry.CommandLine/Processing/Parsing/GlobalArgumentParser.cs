using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Extracts global arguments (e.g., --profile, --help) from raw input strings before
    /// command parsing. Global arguments are stripped from the input so they don't
    /// interfere with command resolution.
    /// </summary>
    public static class GlobalArgumentParser
    {
        // Matches --profile <value> or -P <value> where value is a quoted or unquoted token.
        private static readonly Regex ProfilePattern = new(
            @"(--profile|-P)\s+(""[^""]*""|\S+)",
            RegexOptions.Compiled);

        // Matches --help or -h as standalone flags (not followed by a value that belongs to them).
        // Uses word boundary to avoid matching inside other words.
        private static readonly Regex HelpPattern = new(
            @"(--help|-h)\b",
            RegexOptions.Compiled);

        /// <summary>
        /// Extracts global arguments from the raw input string and returns the
        /// cleaned input with global arguments removed.
        /// </summary>
        /// <param name="input">The raw input string</param>
        /// <param name="cleanedInput">The input string with global arguments removed</param>
        /// <returns>The extracted global arguments</returns>
        public static GlobalArguments Parse(string input, out string cleanedInput)
        {
            var args = new GlobalArguments();
            cleanedInput = input ?? string.Empty;

            // Extract --profile or -P
            var profileMatch = ProfilePattern.Match(cleanedInput);
            if (profileMatch.Success)
            {
                args.ProfileName = profileMatch.Groups[2].Value.Trim('"');
                cleanedInput = cleanedInput.Substring(0, profileMatch.Index)
                    + cleanedInput.Substring(profileMatch.Index + profileMatch.Length);
            }

            // Extract --help or -h
            var helpMatch = HelpPattern.Match(cleanedInput);
            if (helpMatch.Success)
            {
                args.HelpRequested = true;
                cleanedInput = cleanedInput.Substring(0, helpMatch.Index)
                    + cleanedInput.Substring(helpMatch.Index + helpMatch.Length);
            }

            // Normalize multiple spaces into single space and trim
            cleanedInput = Regex.Replace(cleanedInput, @"\s{2,}", " ").Trim();

            return args;
        }
    }
}
