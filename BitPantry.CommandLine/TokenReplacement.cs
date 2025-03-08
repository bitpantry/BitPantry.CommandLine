using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public static class TokenReplacement
    {
        public static string ReplaceTokens(string input, Dictionary<string, string> tokensAndValues)
        {
            return Regex.Replace(input, @"\{([^\}]+)\}", match =>
            {
                var key = match.Groups[1].Value;
                return tokensAndValues.TryGetValue(key, out var value) ? value : match.Value;
            });
        }
    }
}
