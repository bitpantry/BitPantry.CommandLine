using System;
using System.Reflection;

namespace BitPantry.CommandLine.Component
{
    public class ArgumentInfo
    {
        public string Name { get; internal set; }
        public char Alias { get; internal set; }
        public Type DataType => PropertyInfo.PropertyType;
        public string Description { get; internal set; }
        public PropertyInfo PropertyInfo { get; internal set; }
    }
}
