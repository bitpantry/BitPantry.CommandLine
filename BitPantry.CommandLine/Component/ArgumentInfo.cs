using System;
using System.Reflection;

namespace BitPantry.CommandLine.Component
{
    public class ArgumentInfo
    {
        /// <summary>
        /// The name of the argument
        /// </summary>
        public string Name { get; internal set; }

        /// <summary>
        /// The alias of the argument, or default(char) if no alias is defined
        /// </summary>
        public char Alias { get; internal set; }

        /// <summary>
        /// The data type of the argument property
        /// </summary>
        public Type DataType => PropertyInfo.PropertyType;

        /// <summary>
        /// The argument description defined by the Description attribute on the property
        /// </summary>
        public string Description { get; internal set; }

        /// <summary>
        /// The PropertyInfo object that represents the argument property
        /// </summary>
        public PropertyInfo PropertyInfo { get; internal set; }
    }
}
