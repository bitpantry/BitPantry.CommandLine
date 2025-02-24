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

        public CreateClientResponse(string correlationId, List<CommandInfo> commands) : base(correlationId)
        {
            Commands = commands;
        }

        [JsonConstructor]
        public CreateClientResponse(Dictionary<string, string> data) : base(data) { }
    }
}
