using BitPantry.CommandLine.Processing.Parsing;
using Spectre.Console;
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
        /// <summary>
        /// The option value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// The format string to apply to the option value when calling GetFormattedValue
        /// </summary>
        public string Format { get; private set; }

        /// <summary>
        /// Creates an instances of the AutoCompleteOption class
        /// </summary>
        /// <param name="value">The option value</param>
        /// <param name="format">A format string to apply to the option value</param>
        public AutoCompleteOption(string value, string format = null)
        {
            Value = value;
            Format = format;
        }

        /// <summary>
        /// Gets the option value formatted within the provided format string (if any) applying any provided markup to the option value
        /// </summary>
        /// <param name="markup">Markup to apply to the value - e.g., "green" or "default on silver" or "bold underlined</param>
        /// <returns>A formatted option string</returns>
        public string GetFormattedValue(string markup = null)
        => string.IsNullOrEmpty(Format) 
            ? string.IsNullOrEmpty(markup) ? Value : $"[{markup}]{Value}[/]"
            : string.Format(Format, string.IsNullOrEmpty(markup) ? Value : $"[{markup}]{Value}[/]");

        public override string ToString()
            => GetFormattedValue();

    }
}
