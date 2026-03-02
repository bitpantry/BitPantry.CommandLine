using BitPantry.CommandLine.AutoComplete;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class CreateClientRequest : ServerRequest
    {
        [JsonIgnore]
        public Theme Theme
        {
            get { return DeserializeObject<Theme>(MessageArgNames.CreateClientRequest.Theme); }
            set { SerializeObject(value, MessageArgNames.CreateClientRequest.Theme); }
        }

        [JsonConstructor]
        public CreateClientRequest(Dictionary<string, string> data) : base(data)
        {
            RequestType = ServerRequestType.CreateClient;
        }

        public CreateClientRequest(Theme theme) : base(ServerRequestType.CreateClient)
        {
            Theme = theme;
        }
    }
}
