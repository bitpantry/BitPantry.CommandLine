using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property)]
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
