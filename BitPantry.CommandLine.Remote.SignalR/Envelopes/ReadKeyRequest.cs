using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ReadKeyRequest : ClientRequest
    {
        [JsonIgnore]
        public bool Intercept
        {
            get { return ParseString<bool>(MessageArgNames.ReadKeyRequest.Intercept); }
            set { Data[MessageArgNames.ReadKeyRequest.Intercept] = value.ToString(); }
        }

        [JsonConstructor]
        public ReadKeyRequest(Dictionary<string, string> data) : base(data)
        {
            RequestType = ClientRequestType.ReadKey;
        }

        public ReadKeyRequest(bool intercept) : this([]) { Intercept = intercept; }
    }
}
