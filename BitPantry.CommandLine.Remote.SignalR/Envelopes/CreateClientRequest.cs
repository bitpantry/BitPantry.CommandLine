using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class CreateClientRequest : ServerRequest
    {
        [JsonConstructor]
        public CreateClientRequest(Dictionary<string, string> data) : base(data)
        {
            RequestType = ServerRequestType.CreateClient;
        }

        public CreateClientRequest() : base(ServerRequestType.CreateClient) { }
    }
}
