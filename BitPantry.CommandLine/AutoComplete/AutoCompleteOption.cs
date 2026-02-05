using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.AutoComplete
{
    public class AutoCompleteOption
    {
        /// <summary>
        /// The option value
        /// </summary>
        [JsonInclude]
        public string Value { get; private set; }

        /// <summary>
        /// The format string to apply to the option value when calling GetFormattedValue
        /// </summary>
        [JsonInclude]
        public string Format { get; private set; }

        /// <summary>
        /// Indicates whether this option represents a group (container) rather than a command.
        /// Groups are displayed with distinct styling (e.g., cyan color) to indicate they contain subcommands.
        /// </summary>
        [JsonInclude]
        public bool IsGroup { get; private set; }

        /// <summary>
        /// Creates an instances of the AutoCompleteOption class
        /// </summary>
        /// <param name="value">The option value</param>
        /// <param name="format">A format string to apply to the option value</param>
        /// <param name="isGroup">Whether this option represents a group</param>
        public AutoCompleteOption(string value, string format = null, bool isGroup = false)
        {
            Value = value;
            Format = format;
            IsGroup = isGroup;
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
