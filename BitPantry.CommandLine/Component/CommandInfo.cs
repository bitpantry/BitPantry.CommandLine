using BitPantry.CommandLine.Processing.Description;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Component
{
    public class CommandInfo
    {
        private static CodeDomProvider _codeDomProvider = CodeDomProvider.CreateProvider("C#");

        private string _namespace = null;

        /// <summary>
        /// The command namespace, or null if no namespace is defined
        /// </summary>
        public string Namespace
        {
            get { return _namespace; }
            internal set 
            { 
                _namespace = value;
                ValidateNamespace(Type, value);
            }
        }

        private string _name = null;

        /// <summary>
        /// The command name by which the command can be resolved
        /// </summary>
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
        /// The description of the command defined by the Description attribute on the command class
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// The type of the command
        /// </summary>
        public Type Type { get; internal set; }

        /// <summary>
        /// The collection of arguments defined in the command type
        /// </summary>
        public IReadOnlyCollection<ArgumentInfo> Arguments { get; internal set; }

        /// <summary>
        /// If the command type contains an asynchronous Execute function, then true - false otherwise
        /// </summary>
        public bool IsExecuteAsync { get; internal set; }

        /// <summary>
        /// If the generic CommandExecutionContext<T> is used for the Execute function of the command type, this will be the type of the generic type parameter, or null if the non-generic CommandExecutionContext type is used
        /// </summary>
        public Type InputType { get; internal set; } = null;

        /// <summary>
        /// The return type of the Execute function of the command type (including void, if the return type is void)
        /// </summary>
        public Type ReturnType { get; internal set; } = typeof(void);

        private static void ValidateNamespace(Type commandType, string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                var namespaceSegments = str.Split('.');
                foreach (var segment in namespaceSegments)
                    if (!_codeDomProvider.IsValidIdentifier(segment))
                        throw new CommandDescriptionException(commandType, $"The namespace segment, \"{segment}\" is not valid. Each segment must be a valid identifier");
            }
        }

        private static void ValidateName(Type commandType, string name)
        {
            if (!_codeDomProvider.IsValidIdentifier(name))
                throw new CommandDescriptionException(commandType, $"Command name, \"{name}\" is not valid. Command names must be valid identifiers");

        }
    }
}
