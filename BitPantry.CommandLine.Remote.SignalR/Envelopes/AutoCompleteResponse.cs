using BitPantry.CommandLine.AutoComplete;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class AutoCompleteResponse : ResponseMessage
    {
        [JsonIgnore]
        public List<AutoCompleteOption> Results
        {
            get { return DeserializeObject<List<AutoCompleteOption>>(MessageArgNames.AutoCompleteResponse.Results); }
            set { SerializeObject(value, MessageArgNames.AutoCompleteResponse.Results); }
        }

        [JsonConstructor]
        public AutoCompleteResponse(Dictionary<string, string> data) : base(data) { }

        public AutoCompleteResponse(string correlationId, List<AutoCompleteOption> results) : base(correlationId)
        {
            Results = results;
        }
    }
}
