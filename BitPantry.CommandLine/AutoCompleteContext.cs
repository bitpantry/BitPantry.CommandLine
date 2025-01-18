using BitPantry.CommandLine.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine
{
    public record AutoCompleteContext(Dictionary<ArgumentInfo, string> Values) { }
}
