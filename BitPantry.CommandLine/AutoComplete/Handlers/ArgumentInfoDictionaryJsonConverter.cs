using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using BitPantry.CommandLine.Component;

namespace BitPantry.CommandLine.AutoComplete.Handlers
{
    /// <summary>
    /// Custom JSON converter for Dictionary&lt;ArgumentInfo, string&gt; that serializes
    /// the dictionary as an array of key-value pair objects instead of using ArgumentInfo
    /// as a property key (which is not supported by System.Text.Json).
    /// </summary>
    public class ArgumentInfoDictionaryJsonConverter : JsonConverter<IReadOnlyDictionary<ArgumentInfo, string>>
    {
        public override IReadOnlyDictionary<ArgumentInfo, string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return new Dictionary<ArgumentInfo, string>();
            }

            if (reader.TokenType != JsonTokenType.StartArray)
            {
                throw new JsonException($"Expected StartArray token, got {reader.TokenType}");
            }

            var result = new Dictionary<ArgumentInfo, string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return result;
                }

                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException($"Expected StartObject token, got {reader.TokenType}");
                }

                ArgumentInfo key = null;
                string value = null;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException($"Expected PropertyName token, got {reader.TokenType}");
                    }

                    var propertyName = reader.GetString();
                    reader.Read();

                    if (propertyName == "Key")
                    {
                        key = JsonSerializer.Deserialize<ArgumentInfo>(ref reader, options);
                    }
                    else if (propertyName == "Value")
                    {
                        value = reader.GetString();
                    }
                }

                if (key != null)
                {
                    result[key] = value ?? string.Empty;
                }
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyDictionary<ArgumentInfo, string> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartArray();

            foreach (var kvp in value)
            {
                writer.WriteStartObject();
                
                writer.WritePropertyName("Key");
                JsonSerializer.Serialize(writer, kvp.Key, options);
                
                writer.WriteString("Value", kvp.Value);
                
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }
    }
}
