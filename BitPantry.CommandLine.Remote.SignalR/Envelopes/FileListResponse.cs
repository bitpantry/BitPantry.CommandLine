using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Response containing file and directory listing results.
    /// </summary>
    public class FileListResponse : ResponseMessage
    {
        [JsonIgnore]
        public FileListingResult Result
        {
            get { return DeserializeObject<FileListingResult>(MessageArgNames.FileListResponse.Items); }
            set { SerializeObject(value, MessageArgNames.FileListResponse.Items); }
        }

        public FileListResponse(Dictionary<string, string> data) : base(data) { }

        public FileListResponse(string correlationId, FileListingResult result) : this(new Dictionary<string, string>())
        {
            CorrelationId = correlationId;
            Result = result;
        }
    }
}
