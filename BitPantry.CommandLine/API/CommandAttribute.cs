using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marks a class as a CLI command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class CommandAttribute : Attribute
    {
        /// <summary>
        /// The command name. If not specified, derived from class name.
        /// </summary>
        public string Name { get; set; }
    }
}
