using System;
using Spectre.Console;

namespace BitPantry.CommandLine
{
    public static class StringExtensions
    {
        /// <summary>
        /// Removes all markup and unescapes all markup
        /// </summary>
        /// <param name="str">The string to unmarkup</param>
        /// <returns>A new string with all markup removed and all escaped markup unescaped</returns>
        public static string Unmarkup(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            // Step 1: Remove all Spectre.Console markup
            string withoutMarkup = str.RemoveMarkup();

            // Step 2: Unescape escaped markup characters
            string unescaped = withoutMarkup
                .Replace("[[", "[")   // Unescape '['
                .Replace("]]", "]");   // Unescape ']'

            return unescaped;
        }

        /// <summary>
        /// Get's the length of a string stripped of all markup and with all escaped markup removed
        /// </summary>
        /// <param name="str">The string to get the length of</param>
        /// <returns>The terminal display length of the string</returns>
        public static int GetTerminalDisplayLength(this string str)
            => str.Unmarkup().Length;

        /// <summary>
        /// Returns a new string with normalized line endings.
        /// </summary>
        /// <param name="value">The string to normalize line endings for.</param>
        /// <returns>A new string with normalized line endings.</returns>
        public static string NormalizeLineEndings(this string value)
        {
            if (value != null)
            {
                value = value.Replace("\r\n", "\n");
                return value.Replace("\r", string.Empty);
            }

            return string.Empty;
        }

        /// <summary>
        /// Determines if the specified position in the buffer is inside a quoted string.
        /// Uses simple quote counting (odd count = inside).
        /// </summary>
        /// <param name="buffer">The input buffer text.</param>
        /// <param name="position">The cursor position to check.</param>
        /// <returns>True if position is inside quotes, false otherwise.</returns>
        public static bool IsInsideQuotes(this string buffer, int position)
        {
            if (string.IsNullOrEmpty(buffer))
                return false;

            int quoteCount = 0;
            int maxIndex = Math.Min(position, buffer.Length);
            
            for (int i = 0; i < maxIndex; i++)
            {
                if (buffer[i] == '"')
                    quoteCount++;
            }
            
            return (quoteCount % 2) == 1;
        }
    }
}
