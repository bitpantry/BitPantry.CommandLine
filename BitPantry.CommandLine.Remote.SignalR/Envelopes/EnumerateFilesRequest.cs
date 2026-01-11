using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Request file listing with metadata for a pattern.
    /// Enhanced version that returns FileInfoEntry array with path, size, and last modified.
    /// </summary>
    public class EnumerateFilesRequest : ServerRequest
    {
        /// <summary>
        /// Base directory to search (relative to storage root).
        /// </summary>
        [JsonIgnore]
        public string Path
        {
            get { return TryGetValue(MessageArgNames.FileSystem.Path); }
            set { Data[MessageArgNames.FileSystem.Path] = value; }
        }

        /// <summary>
        /// Glob pattern (supports *, **, ?).
        /// </summary>
        [JsonIgnore]
        public string SearchPattern
        {
            get { return TryGetValue(MessageArgNames.FileSystem.SearchPattern); }
            set { Data[MessageArgNames.FileSystem.SearchPattern] = value; }
        }

        /// <summary>
        /// "TopDirectoryOnly" or "AllDirectories".
        /// </summary>
        [JsonIgnore]
        public string SearchOption
        {
            get { return TryGetValue(MessageArgNames.FileSystem.SearchOption); }
            set { Data[MessageArgNames.FileSystem.SearchOption] = value; }
        }

        [JsonConstructor]
        public EnumerateFilesRequest(Dictionary<string, string> data) : base(data) { }

        public EnumerateFilesRequest(string path, string searchPattern, string searchOption) 
            : base(ServerRequestType.EnumerateFiles)
        {
            Path = path;
            SearchPattern = searchPattern;
            SearchOption = searchOption;
        }
    }
}
