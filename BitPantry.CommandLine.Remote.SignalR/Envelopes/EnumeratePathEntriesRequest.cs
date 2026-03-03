using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Client→Server request to enumerate path entries (directories and optionally files)
    /// within a directory on the server's file system.
    /// </summary>
    public class EnumeratePathEntriesRequest : ServerRequest
    {
        /// <summary>
        /// Directory path to enumerate (relative to storage root).
        /// Empty string means "current directory" (storage root on server).
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
        public EnumeratePathEntriesRequest(Dictionary<string, string> data) : base(data) { }

        public EnumeratePathEntriesRequest(string directoryPath, bool includeFiles)
            : base(ServerRequestType.EnumeratePathEntries)
        {
            DirectoryPath = directoryPath;
            IncludeFiles = includeFiles;
        }
    }
}
