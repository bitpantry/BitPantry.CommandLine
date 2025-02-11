using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class IsKeyAvailableResponse : ResponseMessage
    {
        [JsonIgnore]
        public bool IsKeyAvailable
        {
            get { return ParseString<bool>(MessageArgNames.IsKeyAvailableResponse.IsAvailable); }
            set { Data[MessageArgNames.IsKeyAvailableResponse.IsAvailable] = value.ToString(); }
        }

        [JsonConstructor]
        public IsKeyAvailableResponse(Dictionary<string, string> data) : base(data) { }

        public IsKeyAvailableResponse(string correlationId, bool isAvailable) : this([]) { CorrelationId = correlationId; }
    }
}
