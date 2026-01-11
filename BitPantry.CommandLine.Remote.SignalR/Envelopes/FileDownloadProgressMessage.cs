using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Server-to-client SignalR message for download progress updates.
    /// Mirrors FileUploadProgressMessage for consistent patterns.
    /// </summary>
    public class FileDownloadProgressMessage : PushMessage
    {
        [JsonIgnore]
        public long TotalRead
        {
            get { return ParseString<long>(MessageArgNames.FileDownloadProgressUpdate.TotalRead); }
            set { Data[MessageArgNames.FileDownloadProgressUpdate.TotalRead] = value.ToString(); }
        }

        [JsonIgnore]
        public long TotalSize
        {
            get { return ParseString<long>(MessageArgNames.FileDownloadProgressUpdate.TotalSize); }
            set { Data[MessageArgNames.FileDownloadProgressUpdate.TotalSize] = value.ToString(); }
        }

        [JsonIgnore]
        public string Error
        {
            get { return TryGetValue(MessageArgNames.FileDownloadProgressUpdate.Error); }
            set { Data[MessageArgNames.FileDownloadProgressUpdate.Error] = value?.ToString(); }
        }

        [JsonConstructor]
        public FileDownloadProgressMessage(Dictionary<string, string> data) : base(data) { }

        public FileDownloadProgressMessage(long totalRead, long totalSize) : base(PushMessageType.FileDownloadProgress)
        {
            TotalRead = totalRead;
            TotalSize = totalSize;
        }
    }
}
