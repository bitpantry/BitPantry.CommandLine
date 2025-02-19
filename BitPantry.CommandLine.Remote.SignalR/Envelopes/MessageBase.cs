using BitPantry.CommandLine.Remote.SignalR.Serialization;
using BitPantry.Parsing.Strings;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Remote.SignalR.Envelopes
{
    public class MessageBase
    {
        public Dictionary<string, string> Data { get; }

        [JsonIgnore]
        public string CorrelationId
        {
            get { return Data[MessageArgNames.Common.CorrelationId]; }
            set { Data[MessageArgNames.Common.CorrelationId] = value; }
        }

        [JsonConstructor]
        public MessageBase(Dictionary<string, string> data)
        {
            Data = data;
        }

        protected void SerializeObject(object data, string dataJsonKey, string dataTypeKey = null)
        {
            if (data == null)
                return;

            Data[dataJsonKey] = JsonSerializer.Serialize(data, RemoteJsonOptions.Default);

            if (!string.IsNullOrEmpty(dataTypeKey))
                Data[dataTypeKey] = JsonSerializer.Serialize(data.GetType(), RemoteJsonOptions.Default);
        }

        protected object DeserializeObject(string dataJsonKey, string dataTypeKey)
        {
            if (!Data.ContainsKey(dataJsonKey))
                return null;

            return ConcreteObjectSerializer.DeserializeConcreteObject(Data[dataJsonKey], Data[dataTypeKey]);
        }

        protected T DeserializeObject<T>(string key)
        {
            if (!Data.ContainsKey(key))
                return default;

            return JsonSerializer.Deserialize<T>(Data[key], RemoteJsonOptions.Default);
        }

        protected T DeserializeObject<T>(string key, T defaultValue)
            => DeserializeObject<T>(key) ?? defaultValue;

        protected T ParseString<T>(string key)
        {
            if (!Data.ContainsKey(key))
                throw new Exception($"Expected message property not found, {key}");

            return StringParsing.Parse<T>(Data[key]);
        }

        protected T ParseString<T>(string key, T defaultValue)
        {
            if (!Data.ContainsKey(key))
                return defaultValue;

            return StringParsing.Parse<T>(Data[key]);
        }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (MessageBase)obj;
            return Data.Count == other.Data.Count && !Data.Except(other.Data).Any();
        }

        public override int GetHashCode()
        {
            return Data != null ? Data.GetHashCode() : 0;
        }      

    }
}
