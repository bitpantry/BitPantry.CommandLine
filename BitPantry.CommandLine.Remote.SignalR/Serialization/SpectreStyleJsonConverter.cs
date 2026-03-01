using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace BitPantry.CommandLine.Remote.SignalR.Serialization
{
    /// <summary>
    /// JSON converter for Spectre.Console <see cref="Style"/> objects.
    /// Serializes as a markup string (e.g., "bold cyan") via <see cref="Style.ToMarkup"/>
    /// and deserializes via <see cref="Style.Parse"/>.
    /// </summary>
    public class SpectreStyleJsonConverter : JsonConverter<Style>
    {
        public override Style Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var markup = reader.GetString();
            if (string.IsNullOrEmpty(markup))
                return Style.Plain;
            return Style.Parse(markup);
        }

        public override void Write(Utf8JsonWriter writer, Style value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToMarkup());
        }
    }
}
