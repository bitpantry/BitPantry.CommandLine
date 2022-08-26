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
        public string Name
        {
            get { return _name; }
            internal set
            {
                _name = value;
                ValidateName(Type, value);
            }
        }

        public string Description { get; internal set; }
        public Type Type { get; internal set; }
        public IReadOnlyCollection<ArgumentInfo> Arguments { get; internal set; }
        public bool IsExecuteAsync { get; internal set; }
        public Type InputType { get; internal set; } = null;
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
