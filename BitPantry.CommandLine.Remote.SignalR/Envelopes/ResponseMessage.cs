using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ResponseMessage : MessageBase
    {
        [JsonIgnore]
        public bool IsRemoteError
        {
            get { return ParseString(MessageArgNames.Response.IsRemoteError, false); }
            set { Data[MessageArgNames.Response.IsRemoteError] = value.ToString(); }
        }

        public ResponseMessage(string correlationId) : this([]) { CorrelationId = correlationId; }

        [JsonConstructor]
        public ResponseMessage(Dictionary<string, string> data) : base(data) { }
    }
}
