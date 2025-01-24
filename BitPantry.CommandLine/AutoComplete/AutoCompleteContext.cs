using BitPantry.CommandLine.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    public record AutoCompleteContext(string QueryString, Dictionary<ArgumentInfo, string> Values) { }
}
