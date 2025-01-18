using System;

namespace BitPantry.CommandLine.API
{
    [AttributeUsage(AttributeTargets.Property)]
    public class ArgumentAttribute : Attribute
    {
        public string Name { get; set; }

        /// <summary>
        // The name of the function in the same class that can provide auto complete values - the function should accept an AutoCompleteContext and return a List<string>
        /// </summary>
        public string AutoCompleteFunctionName { get; set; }
    }
}
