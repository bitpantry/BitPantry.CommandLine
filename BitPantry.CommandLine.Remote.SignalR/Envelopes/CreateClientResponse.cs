using BitPantry.CommandLine.Component;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class CreateClientResponse : ResponseMessage
    {
        [JsonIgnore]
        public List<CommandInfo> Commands
        {
            get { return DeserializeObject<List<CommandInfo>>(MessageArgNames.CreateClientResponse.CommandInfos, []); }
            set { SerializeObject(value, MessageArgNames.CreateClientResponse.CommandInfos); }
        }

        [JsonIgnore]
        public string ConnectionId
        {
            get { return Data[MessageArgNames.CreateClientResponse.ConnectionId]; }
            set { Data[MessageArgNames.CreateClientResponse.ConnectionId] = value; }
        }

        [JsonIgnore]
        public long MaxFileSizeBytes
        {
            get { return ParseString<long>(MessageArgNames.CreateClientResponse.MaxFileSizeBytes); }
            set { Data[MessageArgNames.CreateClientResponse.MaxFileSizeBytes] = value.ToString(); }
        }

        [JsonIgnore]
        public Dictionary<string, string> AssemblyVersions
        {
            get { return DeserializeObject<Dictionary<string, string>>(MessageArgNames.CreateClientResponse.AssemblyVersions, new()); }
            set { SerializeObject(value, MessageArgNames.CreateClientResponse.AssemblyVersions); }
        }

        [JsonIgnore]
        public string ExecutingAssemblyName
        {
            get
            {
                return Data.TryGetValue(MessageArgNames.CreateClientResponse.ExecutingAssemblyName, out var value)
                    ? value
                    : string.Empty;
            }
            set { Data[MessageArgNames.CreateClientResponse.ExecutingAssemblyName] = value ?? string.Empty; }
        }

        [JsonIgnore]
        public string ExecutingAssemblyVersion
        {
            get
            {
                return Data.TryGetValue(MessageArgNames.CreateClientResponse.ExecutingAssemblyVersion, out var value)
                    ? value
                    : string.Empty;
            }
            set { Data[MessageArgNames.CreateClientResponse.ExecutingAssemblyVersion] = value ?? string.Empty; }
        }

        public CreateClientResponse(string correlationId, string connectionId, List<CommandInfo> commands, long maxFileSizeBytes, Dictionary<string, string> assemblyVersions = null, string executingAssemblyName = "", string executingAssemblyVersion = "") : base(correlationId)
        {
            ConnectionId = connectionId;
            Commands = commands;
            MaxFileSizeBytes = maxFileSizeBytes;
            AssemblyVersions = assemblyVersions ?? new Dictionary<string, string>();
            ExecutingAssemblyName = executingAssemblyName;
            ExecutingAssemblyVersion = executingAssemblyVersion;
        }

        [JsonConstructor]
        public CreateClientResponse(Dictionary<string, string> data) : base(data) { }
    }
}
