using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Provides some helper functions for parsing strings
    /// </summary>
    internal static class StringParsing
    {

        /// <summary>
        /// Splits a command string
        /// </summary>
        /// <param name="cmdString">The command string to split</param>
        /// <returns>The string elements of the split command string</returns>
        public static List<string> SplitCommandString(string cmdString)
            => Split(cmdString, ' ');

        /// <summary>
        /// Splits an input string into the individual command strings
        /// </summary>
        /// <param name="inputString">The string to split</param>
        /// <returns>A list of individual command strings</returns>
        public static List<string> SplitInputString(string inputString)
            => Split(inputString, '|');

        private static List<string> Split(string str, char delimiter)
        {
            Regex csvPreservingQuotedStrings = new Regex(string.Format("(\"[^\"]*\"|[^{0}])+|(\\s?)+", delimiter));
            var values =
                 csvPreservingQuotedStrings.Matches(str)
                .Cast<Match>()
                .Where(m => !string.IsNullOrEmpty(m.Value))
                .Select(m => m.Value);
            return values.ToList();
        }
    }
}
