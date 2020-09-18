using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CommandAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
