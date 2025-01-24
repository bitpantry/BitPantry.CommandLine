using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public static class StringBuilderExtensions
    {
        /// <summary>
        /// Get's the length of the string stripped of any markup or escaped markup
        /// </summary>
        /// <param name="sb">The string builder to get the length for</param>
        /// <returns>The terminal display length of the given string builder</returns>
        public static int GetTerminalDisplayLength(this StringBuilder sb)
            => sb.ToString().Unmarkup().Length;
    }
}
