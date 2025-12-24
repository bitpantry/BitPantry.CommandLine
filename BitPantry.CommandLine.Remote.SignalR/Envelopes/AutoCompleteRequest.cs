using BitPantry.CommandLine.AutoComplete;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class AutoCompleteRequest : ServerRequest
    {
        [JsonIgnore]
        public string GroupPath
        {
            get { return Data[MessageArgNames.AutoCompleteRequest.GroupPath]; }
            set { Data[MessageArgNames.AutoCompleteRequest.GroupPath] = value; }
        }

        [JsonIgnore]
        public string CmdName
        {
            get { return Data[MessageArgNames.AutoCompleteRequest.CmdName]; }
            set { Data[MessageArgNames.AutoCompleteRequest.CmdName] = value; }
        }

        [JsonIgnore]
        public string FunctionName
        {
            get { return Data[MessageArgNames.AutoCompleteRequest.FunctionName]; }
            set { Data[MessageArgNames.AutoCompleteRequest.FunctionName] = value; }
        }

        [JsonIgnore]
        public bool IsFunctionAsync
        {
            get { return ParseString<bool>(MessageArgNames.AutoCompleteRequest.IsFunctionAsync); }
            set { Data[MessageArgNames.AutoCompleteRequest.IsFunctionAsync] = value.ToString(); }
        }

        [JsonIgnore]
        public AutoCompleteContext Context
        {
            get { return DeserializeObject<AutoCompleteContext>(MessageArgNames.AutoCompleteRequest.AutoCompleteContext); }
            set { SerializeObject(value, MessageArgNames.AutoCompleteRequest.AutoCompleteContext); }
        }

        public AutoCompleteRequest(Dictionary<string, string> data) : base(data) { }

        public AutoCompleteRequest(string groupPath, string cmdName, string functionName, bool isFunctionAsync, AutoCompleteContext ctx) : this([])
        {
            GroupPath = groupPath;
            CmdName = cmdName;
            RequestType = ServerRequestType.AutoComplete;
            FunctionName = functionName;
            IsFunctionAsync = isFunctionAsync;
            Context = ctx;
        }
    }
}