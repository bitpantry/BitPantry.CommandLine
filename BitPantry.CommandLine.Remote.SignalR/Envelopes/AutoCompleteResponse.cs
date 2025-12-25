using BitPantry.CommandLine.AutoComplete;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class AutoCompleteResponse : ResponseMessage
    {
        [JsonIgnore]
        public CompletionResult Result
        {
            get { return DeserializeObject<CompletionResult>(MessageArgNames.AutoCompleteResponse.Results); }
            set { SerializeObject(value, MessageArgNames.AutoCompleteResponse.Results); }
        }

        [JsonConstructor]
        public AutoCompleteResponse(Dictionary<string, string> data) : base(data) { }

        public AutoCompleteResponse(string correlationId, CompletionResult result) : base(correlationId)
        {
            Result = result;
        }
    }
}
