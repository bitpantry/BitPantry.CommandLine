using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ClientFileUploadRequestMessage : PushMessage
    {
        [JsonIgnore]
        public string ClientPath
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.ClientPath); }
            set { Data[MessageArgNames.ClientFileAccess.ClientPath] = value; }
        }

        [JsonIgnore]
        public string ServerTempPath
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.ServerTempPath); }
            set { Data[MessageArgNames.ClientFileAccess.ServerTempPath] = value; }
        }

        [JsonConstructor]
        public ClientFileUploadRequestMessage(Dictionary<string, string> data) : base(data) { }

        public ClientFileUploadRequestMessage(string clientPath, string serverTempPath)
            : base(PushMessageType.ClientFileUploadRequest)
        {
            ClientPath = clientPath;
            ServerTempPath = serverTempPath;
        }
    }
}
