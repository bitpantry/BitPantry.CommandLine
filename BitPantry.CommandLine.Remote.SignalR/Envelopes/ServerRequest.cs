using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public enum ServerRequestType
    {
        CreateClient = 1,
        Run = 2,
        AutoComplete = 3
    }

    public class ServerRequest : MessageBase
    {
        [JsonIgnore]
        public ServerRequestType RequestType
        {
            get { return ParseString<ServerRequestType>(MessageArgNames.Request.RequestType); }
            set { Data[MessageArgNames.Request.RequestType] = value.ToString(); }
        }

        [JsonConstructor]
        public ServerRequest(Dictionary<string, string> data) : base(data) { }

        public ServerRequest(ServerRequestType requestType) : this([]) { RequestType = requestType; }
    }
}
