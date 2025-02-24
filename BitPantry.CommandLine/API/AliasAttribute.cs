using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AliasAttribute : Attribute
    {
        public char Alias { get; set; }

        public AliasAttribute(char alias)
        {
            if (alias == ' ')
                throw new ArgumentNullException($"{nameof(alias)} must be a non-empty value");
            Alias = alias;
        }
    }
}
