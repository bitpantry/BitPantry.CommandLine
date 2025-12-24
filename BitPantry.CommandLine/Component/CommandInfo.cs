using BitPantry.CommandLine.Processing.Description;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BitPantry.CommandLine.Component
{
    public class CommandInfo
    {
        private static CodeDomProvider _codeDomProvider = CodeDomProvider.CreateProvider("C#");

        /// <summary>
        /// The group this command belongs to, or null for root-level commands.
        /// </summary>
        [JsonIgnore]
        public GroupInfo Group { get; internal set; }

        /// <summary>
        /// The group path for serialization (space-separated hierarchy path).
        /// Used when transmitting commands over the wire and deserializing them.
        /// </summary>
        [JsonInclude]
        public string GroupPath
        {
            get => Group?.FullPath;
            internal set => _serializedGroupPath = value;
        }

        /// <summary>
        /// Stores the serialized group path for later restoration of group info.
        /// </summary>
        private string _serializedGroupPath;

        /// <summary>
        /// Gets the serialized group path (used for restoring group info after deserialization).
        /// </summary>
        internal string SerializedGroupPath => _serializedGroupPath;

        private string _name = null;

        /// <summary>
        /// The command name by which the command can be resolved
        /// </summary>
        [JsonInclude]
        public string Name
        {
            get { return _name; }
            internal set
            {
                _name = value;
                ValidateName(Type, value);
            }
        }

        /// <summary>
        /// The fully qualified command name including group path (space-separated).
        /// Example: "math advanced sqrt"
        /// </summary>
        public string FullyQualifiedName => Group == null
            ? Name
            : $"{Group.FullPath} {Name}";

        /// <summary>
        /// The description of the command defined by the Description attribute on the command class
        /// </summary>
        [JsonInclude]
        public string Description { get; internal set; }

        /// <summary>
        /// The type of the command
        /// </summary>
        [JsonInclude]
        public Type Type { get; internal set; }

        /// <summary>
        /// The collection of arguments defined in the command type
        /// </summary>
        [JsonInclude]
        public IReadOnlyCollection<ArgumentInfo> Arguments { get; internal set; }

        /// <summary>
        /// True if the command is a remote command associated with an active remote connection
        /// </summary>
        [JsonInclude]
        public bool IsRemote { get; internal set; } = false;

        /// <summary>
        /// If the command type contains an asynchronous Execute function, then true - false otherwise
        /// </summary>
        [JsonInclude]
        public bool IsExecuteAsync { get; internal set; }

        /// <summary>
        /// If the generic CommandExecutionContext<T> is used for the Execute function of the command type, this will be the type of the generic type parameter, or null if the non-generic CommandExecutionContext type is used
        /// </summary>
        [JsonInclude]
        public Type InputType { get; internal set; } = null;

        /// <summary>
        /// The return type of the Execute function of the command type (including void, if the return type is void)
        /// </summary>
        [JsonInclude]
        public Type ReturnType { get; internal set; } = typeof(void);

        private static void ValidateName(Type commandType, string name)
        {
            if (!_codeDomProvider.IsValidIdentifier(name))
                throw new CommandDescriptionException(commandType, $"Command name, \"{name}\" is not valid. Command names must be valid identifiers");

        }
    }
}
