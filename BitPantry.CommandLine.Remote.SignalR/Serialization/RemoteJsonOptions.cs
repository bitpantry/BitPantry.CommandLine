using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Serialization
{
    public static class RemoteJsonOptions
    {
        public static readonly JsonSerializerOptions Default = new()
        {
            WriteIndented = false,
            Converters = { new SystemTypeJsonConverter() }
        };
    }
}
