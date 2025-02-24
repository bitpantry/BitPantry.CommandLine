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
    }
}
