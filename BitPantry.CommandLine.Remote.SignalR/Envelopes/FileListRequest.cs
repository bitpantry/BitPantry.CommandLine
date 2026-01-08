using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    /// <summary>
    /// Request to list files and directories in a remote path for autocomplete.
    /// </summary>
    public class FileListRequest : ServerRequest
    {
        [JsonIgnore]
        public string Path
        {
            get { return Data.ContainsKey(MessageArgNames.FileListRequest.Path) ? Data[MessageArgNames.FileListRequest.Path] : string.Empty; }
            set { Data[MessageArgNames.FileListRequest.Path] = value; }
        }

        [JsonIgnore]
        public string? SearchPrefix
        {
            get { return Data.ContainsKey(MessageArgNames.FileListRequest.SearchPrefix) ? Data[MessageArgNames.FileListRequest.SearchPrefix] : null; }
            set 
            { 
                if (value != null)
                    Data[MessageArgNames.FileListRequest.SearchPrefix] = value; 
            }
        }

        [JsonIgnore]
        public bool FilesOnly
        {
            get { return Data.ContainsKey(MessageArgNames.FileListRequest.FilesOnly) && bool.Parse(Data[MessageArgNames.FileListRequest.FilesOnly]); }
            set { Data[MessageArgNames.FileListRequest.FilesOnly] = value.ToString(); }
        }

        public FileListRequest(Dictionary<string, string> data) : base(data) { }

        public FileListRequest(string path, string? searchPrefix, bool filesOnly) : this(new Dictionary<string, string>())
        {
            Path = path;
            SearchPrefix = searchPrefix;
            FilesOnly = filesOnly;
            RequestType = ServerRequestType.FileList;
        }
    }
}
