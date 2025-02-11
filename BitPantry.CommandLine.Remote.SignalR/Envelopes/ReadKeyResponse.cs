using BitPantry.CommandLine.Remote.SignalR.Serialization;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ReadKeyResponse : ResponseMessage
    {
        [JsonIgnore]
        public SerializableConsoleKeyInfo KeyInfo
        {
            get { return DeserializeObject<SerializableConsoleKeyInfo>(MessageArgNames.ReadKeyResponse.KeyInfo); }
            set { SerializeObject(value, MessageArgNames.ReadKeyResponse.KeyInfo); }
        }

        [JsonConstructor]
        public ReadKeyResponse(Dictionary<string, string> data) : base(data) { }

        public ReadKeyResponse(string correlationId, ConsoleKeyInfo? keyInfo) : base(correlationId)
        {
            KeyInfo = new SerializableConsoleKeyInfo(keyInfo);
        }
    }
}
