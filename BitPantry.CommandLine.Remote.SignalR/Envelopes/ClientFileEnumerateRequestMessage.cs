using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ClientFileEnumerateRequestMessage : PushMessage
    {
        [JsonIgnore]
        public string GlobPattern
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.GlobPattern); }
            set { Data[MessageArgNames.ClientFileAccess.GlobPattern] = value; }
        }

        [JsonConstructor]
        public ClientFileEnumerateRequestMessage(Dictionary<string, string> data) : base(data) { }

        public ClientFileEnumerateRequestMessage(string globPattern)
            : base(PushMessageType.ClientFileEnumerateRequest)
        {
            GlobPattern = globPattern;
        }
    }
}
