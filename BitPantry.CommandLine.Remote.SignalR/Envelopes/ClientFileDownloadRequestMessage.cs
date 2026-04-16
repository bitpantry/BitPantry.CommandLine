using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ClientFileDownloadRequestMessage : PushMessage
    {
        [JsonIgnore]
        public string ServerPath
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.ServerPath); }
            set { Data[MessageArgNames.ClientFileAccess.ServerPath] = value; }
        }

        [JsonIgnore]
        public string ClientPath
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.ClientPath); }
            set { Data[MessageArgNames.ClientFileAccess.ClientPath] = value; }
        }

        [JsonIgnore]
        public long FileSize
        {
            get { return ParseString<long>(MessageArgNames.ClientFileAccess.FileSize); }
            set { Data[MessageArgNames.ClientFileAccess.FileSize] = value.ToString(); }
        }

        [JsonConstructor]
        public ClientFileDownloadRequestMessage(Dictionary<string, string> data) : base(data) { }

        public ClientFileDownloadRequestMessage(string serverPath, string clientPath, long fileSize)
            : base(PushMessageType.ClientFileDownloadRequest)
        {
            ServerPath = serverPath;
            ClientPath = clientPath;
            FileSize = fileSize;
        }
    }
}
