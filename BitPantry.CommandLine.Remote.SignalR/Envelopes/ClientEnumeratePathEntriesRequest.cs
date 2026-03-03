using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Server→Client request to enumerate path entries (directories and optionally files)
    /// on the client's file system.
    /// </summary>
    public class ClientEnumeratePathEntriesRequest : ClientRequest
    {
        /// <summary>
        /// Directory path to enumerate on the client.
        /// Empty string means "current directory".
        /// </summary>
        [JsonIgnore]
        public string DirectoryPath
        {
            get { return TryGetValue(MessageArgNames.PathEntries.DirectoryPath); }
            set { Data[MessageArgNames.PathEntries.DirectoryPath] = value; }
        }

        /// <summary>
        /// Whether to include files in addition to directories.
        /// </summary>
        [JsonIgnore]
        public bool IncludeFiles
        {
            get { return TryGetValue(MessageArgNames.PathEntries.IncludeFiles) == "true"; }
            set { Data[MessageArgNames.PathEntries.IncludeFiles] = value ? "true" : "false"; }
        }

        [JsonConstructor]
        public ClientEnumeratePathEntriesRequest(Dictionary<string, string> data) : base(data) { }

        public ClientEnumeratePathEntriesRequest(string directoryPath, bool includeFiles)
            : base(ClientRequestType.EnumeratePathEntries)
        {
            DirectoryPath = directoryPath;
            IncludeFiles = includeFiles;
        }
    }
}
