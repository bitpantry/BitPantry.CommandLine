using System;
using System.Reflection;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Component
{
    [Serializable]
    public class SerializablePropertyInfo
    {
        [JsonInclude]
        public string PropertyName { get; private set; }

        [JsonInclude]
        public string DeclaringTypeName { get; private set; }

        [JsonInclude]
        public string PropertyTypeName { get; private set; }

        [JsonInclude]
        public bool CanRead { get; private set; }

        [JsonInclude]
        public bool CanWrite { get; private set; }

        [JsonInclude]
        public bool IsStatic { get; private set; }

        [JsonInclude]
        public string GetMethodName { get; private set; }

        [JsonInclude]
        public string SetMethodName { get; private set; }

        public SerializablePropertyInfo() { } // Required for deserialization

        public SerializablePropertyInfo(PropertyInfo property)
        {
            PropertyName = property.Name;
            DeclaringTypeName = property.DeclaringType.AssemblyQualifiedName;
            PropertyTypeName = property.PropertyType.AssemblyQualifiedName;
            CanRead = property.CanRead;
            CanWrite = property.CanWrite;
            IsStatic = property.GetMethod?.IsStatic ?? false;
            GetMethodName = property.GetMethod?.Name;
            SetMethodName = property.SetMethod?.Name;
        }

        public PropertyInfo GetPropertyInfo()
        {
            Type declaringType = Type.GetType(DeclaringTypeName);
            if (declaringType == null) throw new InvalidOperationException($"Command type '{DeclaringTypeName}' not found. Is this a remote command property?");

            PropertyInfo prop = declaringType.GetProperty(PropertyName);
            if (prop == null) throw new InvalidOperationException($"Property '{PropertyName}' not found in command type '{declaringType.Name}'. Is this a remote command property?");

            return prop;
        }

        public object GetValue(object obj, object[] index = null)
        {
            PropertyInfo prop = GetPropertyInfo();
            return prop.GetValue(IsStatic ? null : obj, index);
        }

        public void SetValue(object obj, object value, object[] index = null)
        {
            PropertyInfo prop = GetPropertyInfo();
            prop.SetValue(IsStatic ? null : obj, value, index);
        }

    }
}
