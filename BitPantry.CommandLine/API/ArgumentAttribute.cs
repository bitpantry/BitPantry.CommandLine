using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
