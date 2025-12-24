using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marks a class as a command group container. Groups organize related commands
    /// into hierarchical structures. Groups are non-executable and display help when invoked.
    /// Use the [Description] attribute on the class to provide a description for help output.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GroupAttribute : Attribute
    {
        /// <summary>
        /// Optional group name override. If not specified, derived from the class name (lowercased).
        /// Example: "UserManagement" class → "usermanagement"
        /// </summary>
        public string Name { get; set; }
    }
}
