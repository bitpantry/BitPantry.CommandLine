using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class ClientFileAccessResponseMessage : ServerRequest
    {
        [JsonIgnore]
        public bool Success
        {
            get { return ParseString<bool>(MessageArgNames.ClientFileAccess.Success); }
            set { Data[MessageArgNames.ClientFileAccess.Success] = value.ToString(); }
        }

        [JsonIgnore]
        public string Error
        {
            get { return TryGetValue(MessageArgNames.ClientFileAccess.Error); }
            set { Data[MessageArgNames.ClientFileAccess.Error] = value; }
        }

        [JsonIgnore]
        public FileInfoEntry[] FileInfoEntries
        {
            get
            {
                var json = TryGetValue(MessageArgNames.ClientFileAccess.FileInfoEntries);
                if (string.IsNullOrEmpty(json))
                    return Array.Empty<FileInfoEntry>();
                return JsonSerializer.Deserialize<FileInfoEntry[]>(json);
            }
            set
            {
                Data[MessageArgNames.ClientFileAccess.FileInfoEntries] = value != null
                    ? JsonSerializer.Serialize(value)
                    : null;
            }
        }

        [JsonConstructor]
        public ClientFileAccessResponseMessage(Dictionary<string, string> data) : base(data) { }

        public ClientFileAccessResponseMessage(bool success, string error = null, FileInfoEntry[] fileInfoEntries = null)
            : base(ServerRequestType.ClientFileAccessResponse)
        {
            Success = success;
            Error = error;
            FileInfoEntries = fileInfoEntries;
        }
    }
}
