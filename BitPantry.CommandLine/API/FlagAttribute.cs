using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Marks a bool argument as a presence-only flag.
    /// When present in command input (e.g., --verbose or -v), the property is set to true.
    /// When absent, the property is set to false.
    /// Flags cannot accept values - using "--flag true" will result in an error.
    /// This attribute can only be applied to bool properties.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FlagAttribute : Attribute
    {
    }
}
