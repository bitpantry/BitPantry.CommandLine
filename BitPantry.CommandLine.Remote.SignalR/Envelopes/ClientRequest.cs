using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public enum ClientRequestType
    {
        IsKeyAvailable = 1,
        ReadKey = 2
    }

    public class ClientRequest : MessageBase
    {
        public ClientRequestType RequestType
        {
            get { return ParseString<ClientRequestType>(MessageArgNames.Request.RequestType); }
            set { Data[MessageArgNames.Request.RequestType] = value.ToString(); }
        }


        [JsonConstructor]
        public ClientRequest(Dictionary<string, string> data) : base(data) { }

        public ClientRequest(ClientRequestType requestType) : this([]) { RequestType = requestType; }
    }
}
