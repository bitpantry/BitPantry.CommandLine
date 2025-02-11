using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Serialization
{
    public class EnumJsonConverter<T> : JsonConverter<T> where T : struct, Enum
    {
        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string enumString = reader.GetString();
                if (Enum.TryParse(enumString, true, out T enumValue))
                {
                    return enumValue;
                }
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                int intValue = reader.GetInt32();
                if (Enum.IsDefined(typeof(T), intValue))
                {
                    return (T)Enum.ToObject(typeof(T), intValue);
                }
            }

            throw new JsonException($"Invalid value for enum {typeof(T).Name}: {reader.GetString()}");
        }
    }

}
