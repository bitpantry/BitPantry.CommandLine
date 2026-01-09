using BitPantry.CommandLine.Component;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class CreateClientResponse : ResponseMessage
    {
        [JsonIgnore]
        public List<CommandInfo> Commands
        {
            get { return DeserializeObject<List<CommandInfo>>(MessageArgNames.CreateClientResponse.CommandInfos, []); }
            set { SerializeObject(value, MessageArgNames.CreateClientResponse.CommandInfos); }
        }

        [JsonIgnore]
        public string ConnectionId
        {
            get { return Data[MessageArgNames.CreateClientResponse.ConnectionId]; }
            set { Data[MessageArgNames.CreateClientResponse.ConnectionId] = value; }
        }

        [JsonIgnore]
        public long MaxFileSizeBytes
        {
            get { return ParseString<long>(MessageArgNames.CreateClientResponse.MaxFileSizeBytes); }
            set { Data[MessageArgNames.CreateClientResponse.MaxFileSizeBytes] = value.ToString(); }
        }

        public CreateClientResponse(string correlationId, string connectionId, List<CommandInfo> commands, long maxFileSizeBytes) : base(correlationId)
        {
            ConnectionId = connectionId;
            Commands = commands;
            MaxFileSizeBytes = maxFileSizeBytes;
        }

        [JsonConstructor]
        public CreateClientResponse(Dictionary<string, string> data) : base(data) { }
    }
}
