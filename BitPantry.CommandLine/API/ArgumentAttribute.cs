using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class ArgumentAttribute : Attribute
    {
        public string Name { get; set; }

        /// <summary>
        // The name of the function in the same class that can provide auto complete values - the function should accept an AutoCompleteContext and return a List<string>
        /// </summary>
        public string AutoCompleteFunctionName { get; set; }

        /// <summary>
        /// Whether or not the argument is required
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// The zero-based position for positional arguments. A value of -1 (default) indicates a named argument.
        /// Position values must be contiguous starting from 0 with no gaps.
        /// </summary>
        public int Position { get; set; } = -1;

        /// <summary>
        /// When true, this positional argument captures all remaining positional values.
        /// Only valid on collection-typed properties (arrays, List&lt;T&gt;, IEnumerable&lt;T&gt;).
        /// Must be the last positional argument (highest Position value) and only one IsRest per command.
        /// </summary>
        public bool IsRest { get; set; } = false;
    }
}
