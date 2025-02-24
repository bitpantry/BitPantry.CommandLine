using System.Text.Json;

namespace BitPantry.CommandLine.Remote.SignalR.Serialization
{
    public static class ConcreteObjectSerializer
    {
        public static object DeserializeConcreteObject(string json, string assemblyQualifiedTypeName)
        {
            if (json == null)
                throw new ArgumentNullException(nameof(json));

            if (string.IsNullOrEmpty(assemblyQualifiedTypeName))
                throw new ArgumentException($"{nameof(assemblyQualifiedTypeName)} cannot be null or empty");

            var type = Type.GetType(assemblyQualifiedTypeName) ?? throw new ArgumentException($"Cannot find type, {assemblyQualifiedTypeName}");

            return JsonSerializer.Deserialize(json, type, RemoteJsonOptions.Default);
        }

        public static string SerializeConcreteObject(object obj, out string assemblyQualifiedTypeName)
        {
            if (obj == null)
            {
                assemblyQualifiedTypeName = null;
                return null;
            }
            else
            {
                assemblyQualifiedTypeName = obj.GetType().AssemblyQualifiedName;

                string json = null;
                try { json = JsonSerializer.Serialize(obj, obj.GetType(), RemoteJsonOptions.Default); }
                catch (Exception ex) { throw new ArgumentException($"An error occured while serializing an object, {obj.GetType().FullName}", ex); }

                return json;
            }
        }
    }
}
