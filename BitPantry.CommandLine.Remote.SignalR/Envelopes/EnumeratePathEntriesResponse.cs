using System.Text.Json;
using System.Text.Json.Serialization;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Response containing path entries (directories and optionally files).
    /// Used for both client→server and server→client enumerate path entries responses.
    /// </summary>
    public class EnumeratePathEntriesResponse : MessageBase
    {
        /// <summary>
        /// Array of path entries (serialized as JSON string).
        /// </summary>
        [JsonIgnore]
        public PathEntry[] Entries
        {
            get
            {
                var json = TryGetValue(MessageArgNames.PathEntries.Entries);
                if (string.IsNullOrEmpty(json))
                    return Array.Empty<PathEntry>();
                return JsonSerializer.Deserialize<PathEntry[]>(json);
            }
            set
            {
                Data[MessageArgNames.PathEntries.Entries] = value != null
                    ? JsonSerializer.Serialize(value)
                    : null;
            }
        }

        /// <summary>
        /// Error message if the operation failed.
        /// </summary>
        [JsonIgnore]
        public string Error
        {
            get { return TryGetValue(MessageArgNames.PathEntries.Error); }
            set { Data[MessageArgNames.PathEntries.Error] = value; }
        }

        [JsonConstructor]
        public EnumeratePathEntriesResponse(Dictionary<string, string> data) : base(data) { }

        public EnumeratePathEntriesResponse(string correlationId, PathEntry[] entries, string error = null)
            : base(new Dictionary<string, string>())
        {
            CorrelationId = correlationId;
            Entries = entries;
            Error = error;
        }
    }
}
