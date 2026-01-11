using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Response with file listing including size information.
    /// Enhanced version that returns FileInfoEntry array with path, size, and last modified.
    /// </summary>
    public class EnumerateFilesResponse : MessageBase
    {
        /// <summary>
        /// Array of file info entries (serialized as JSON string).
        /// </summary>
        [JsonIgnore]
        public FileInfoEntry[] Files
        {
            get 
            { 
                var json = TryGetValue(MessageArgNames.FileSystem.Files);
                if (string.IsNullOrEmpty(json))
                    return Array.Empty<FileInfoEntry>();
                return JsonSerializer.Deserialize<FileInfoEntry[]>(json);
            }
            set 
            { 
                Data[MessageArgNames.FileSystem.Files] = value != null 
                    ? JsonSerializer.Serialize(value) 
                    : null; 
            }
        }

        /// <summary>
        /// Error message if operation failed.
        /// </summary>
        [JsonIgnore]
        public string Error
        {
            get { return TryGetValue(MessageArgNames.FileSystem.Error); }
            set { Data[MessageArgNames.FileSystem.Error] = value; }
        }

        [JsonConstructor]
        public EnumerateFilesResponse(Dictionary<string, string> data) : base(data) { }

        public EnumerateFilesResponse(string correlationId, FileInfoEntry[] files, string error = null) 
            : base(new Dictionary<string, string>())
        {
            CorrelationId = correlationId;
            Files = files;
            Error = error;
        }
    }
}
