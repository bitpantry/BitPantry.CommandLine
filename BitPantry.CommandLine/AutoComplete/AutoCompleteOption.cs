using System.Text.Json.Serialization;
using Spectre.Console;

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
        /// A string.Format template for menu display (e.g., "{0} (default)"). When null, displays Value as-is.
        /// </summary>
        [JsonInclude]
        public string MenuFormat { get; set; }

        /// <summary>
        /// A string.Format template for writing to the input line on acceptance (e.g., "{0} ").
        /// When null, writes Value as-is.
        /// </summary>
        [JsonInclude]
        public string AcceptFormat { get; set; }

        /// <summary>
        /// An optional Spectre Style to apply when rendering this option in the menu.
        /// Serializes as a markup string (e.g., "cyan") via SpectreStyleJsonConverter.
        /// </summary>
        [JsonInclude]
        public Style MenuStyle { get; set; }

        /// <summary>
        /// Creates an instance of the AutoCompleteOption class
        /// </summary>
        /// <param name="value">The option value</param>
        /// <param name="menuFormat">A string.Format template for menu display</param>
        /// <param name="acceptFormat">A string.Format template for acceptance into input</param>
        /// <param name="menuStyle">An optional Spectre Style for menu rendering</param>
        public AutoCompleteOption(string value, string menuFormat = null, string acceptFormat = null, Style menuStyle = null)
        {
            Value = value;
            MenuFormat = menuFormat;
            AcceptFormat = acceptFormat;
            MenuStyle = menuStyle;
        }

        /// <summary>
        /// Gets the value formatted for display in the autocomplete menu.
        /// Uses MenuFormat when set, otherwise returns Value as-is.
        /// </summary>
        public string GetMenuValue()
            => string.IsNullOrEmpty(MenuFormat) ? Value : string.Format(MenuFormat, Value);

        /// <summary>
        /// Gets the value formatted for acceptance into the input line.
        /// Uses AcceptFormat when set, otherwise returns Value as-is.
        /// </summary>
        public string GetAcceptedValue()
            => string.IsNullOrEmpty(AcceptFormat) ? Value : string.Format(AcceptFormat, Value);

        public override string ToString()
            => GetMenuValue();

    }
}
