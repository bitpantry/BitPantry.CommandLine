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

        public CreateClientResponse(string correlationId, string connectionId, List<CommandInfo> commands) : base(correlationId)
        {
            ConnectionId = connectionId;
            Commands = commands;
        }

        [JsonConstructor]
        public CreateClientResponse(Dictionary<string, string> data) : base(data) { }
    }
}
