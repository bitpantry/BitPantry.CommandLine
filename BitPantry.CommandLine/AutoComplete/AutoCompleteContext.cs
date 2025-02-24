using BitPantry.CommandLine.Component;
using System.Collections.Generic;

namespace BitPantry.CommandLine.AutoComplete
{
    public record AutoCompleteContext(string QueryString, Dictionary<ArgumentInfo, string> Values) { }
}
