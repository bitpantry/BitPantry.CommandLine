using BitPantry.CommandLine.Processing.Execution;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class RunResponse : ResponseMessage
    {
        [JsonIgnore]
        public RunResultCode ResultCode
        {
            get { return ParseString<RunResultCode>(MessageArgNames.RunResponse.ResultCode); }
            set { Data[MessageArgNames.RunResponse.ResultCode] = value.ToString(); }
        }

        [JsonIgnore]
        public object Result
        {
            get { return DeserializeObject(MessageArgNames.RunResponse.Result, MessageArgNames.RunResponse.ResultDataType); }
            set { SerializeObject(value, MessageArgNames.RunResponse.Result, MessageArgNames.RunResponse.ResultDataType); }
        }

        [JsonIgnore]
        public bool IsRunError
        {
            get { return ParseString(MessageArgNames.RunResponse.IsRunError, false); }
            set { Data[MessageArgNames.RunResponse.IsRunError] = value.ToString(); }
        }

        public RunResponse(string correlationId) : base(correlationId) { }

        [JsonConstructor]
        public RunResponse(Dictionary<string, string> data) : base(data) { }
    }
}
