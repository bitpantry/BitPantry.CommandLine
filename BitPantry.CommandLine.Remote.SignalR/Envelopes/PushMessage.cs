using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public enum PushMessageType
    {
        FileUploadProgress
    }

    public class PushMessage : MessageBase
    {
        [JsonIgnore]
        public PushMessageType MessageType
        {
            get { return ParseString<PushMessageType>(MessageArgNames.PushMessage.MessageType); }
            set { Data[MessageArgNames.PushMessage.MessageType] = value.ToString(); }
        }

        [JsonConstructor]
        public PushMessage(Dictionary<string, string> data) : base(data) { }
        public PushMessage(PushMessageType messageType) : base(new Dictionary<string, string>()) 
        {
            MessageType = messageType;
        }
    }
}
