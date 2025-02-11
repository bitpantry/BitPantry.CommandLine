using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Serialization
{
    public class SystemTypeJsonConverter : JsonConverter<Type>
    {
        public override void Write(Utf8JsonWriter writer, Type value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.AssemblyQualifiedName);
        }

        public override Type Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var typeName = reader.GetString();
            return Type.GetType(typeName);
        }
    }

}
