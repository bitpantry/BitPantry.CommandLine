using System;
using System.Collections.Generic;
using System.Text;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NamespaceAttribute : Attribute
    {
        public char Namespace { get; set; }

        public NamespaceAttribute(char @namespace)
        {
            if (@namespace == ' ')
                throw new ArgumentNullException($"{nameof(@namespace)} must be a non-empty value");
            Namespace = @namespace;
        }
    }
}
