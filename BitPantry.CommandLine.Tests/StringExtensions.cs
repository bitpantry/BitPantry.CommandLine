using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests
{
    internal static class StringExtensions
    {
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
    }
}
