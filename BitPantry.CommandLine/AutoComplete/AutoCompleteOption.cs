using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete
{
    public class AutoCompleteOption
    {
        public string Value { get; private set; }
        public string Format { get; private set; }

        public AutoCompleteOption(string value, string format = null)
        {
            Value = value;
            Format = format;
        }

        public string GetFormattedValue()
            => string.IsNullOrEmpty(Format) ? Value : string.Format(Format, Value);

        public override string ToString()
            => GetFormattedValue();
    }
}
