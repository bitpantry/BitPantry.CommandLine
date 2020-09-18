using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property)]
    public class OptionAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
