using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class FileUploadProgressMessage : PushMessage
    {
        [JsonIgnore]
        public int TotalRead
        {
            get { return ParseString<int>(MessageArgNames.FileUploadProgressUpdate.TotalRead); }
            set { Data[MessageArgNames.FileUploadProgressUpdate.TotalRead] = value.ToString(); }
        }

        [JsonIgnore]
        public string Error
        {
            get { return TryGetValue(MessageArgNames.FileUploadProgressUpdate.Error); }
            set { Data[MessageArgNames.FileUploadProgressUpdate.Error] = value.ToString(); }
        }

        [JsonConstructor]
        public FileUploadProgressMessage(Dictionary<string, string> data) : base(data) { }

        public FileUploadProgressMessage(int totalRead) : base(PushMessageType.FileUploadProgress) 
        { 
            TotalRead = totalRead;
        }
    }
}
