using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using BitPantry.CommandLine.AutoComplete.Attributes;

namespace BitPantry.CommandLine.Component
{
    public class ArgumentInfo
    {
        /// <summary>
        /// The name of the argument
        /// </summary>
        [JsonInclude]
        public string Name { get; internal set; }

        /// <summary>
        /// The alias of the argument, or default(char) if no alias is defined
        /// </summary>
        [JsonInclude]
        public char Alias { get; internal set; }

        /// <summary>
        /// The argument description defined by the Description attribute on the property
        /// </summary>
        [JsonInclude]
        public string Description { get; internal set; }

        /// <summary>
        /// The PropertyInfo object that represents the argument property
        /// </summary>
        [JsonInclude]
        public SerializablePropertyInfo PropertyInfo { get; internal set; }

        /// <summary>
        /// The completion attribute on the property, if any.
        /// Provides completion configuration (method name, static values, or provider type).
        /// </summary>
        [JsonIgnore]
        public CompletionAttribute CompletionAttribute { get; internal set; }

        /// <summary>
        /// Whether the completion method (if any) is asynchronous.
        /// </summary>
        [JsonInclude]
        public bool IsCompletionMethodAsync { get; internal set; }

        /// <summary>
        /// Whether or not the argument is required
        /// </summary>
        [JsonInclude]
        public bool IsRequired { get; internal set; }

        /// <summary>
        /// The zero-based position for positional arguments. A value of -1 indicates a named argument.
        /// </summary>
        [JsonInclude]
        public int Position { get; internal set; } = -1;

        /// <summary>
        /// When true, this positional argument captures all remaining positional values.
        /// </summary>
        [JsonInclude]
        public bool IsRest { get; internal set; }

        /// <summary>
        /// Returns true if this argument is positional (Position >= 0)
        /// </summary>
        [JsonIgnore]
        public bool IsPositional => Position >= 0;

        /// <summary>
        /// Returns true if the property type is a collection (array, List, IEnumerable, etc.) but not string
        /// </summary>
        [JsonIgnore]
        public bool IsCollection
        {
            get
            {
                if (PropertyInfo?.PropertyTypeName == null)
                    return false;

                var type = Type.GetType(PropertyInfo.PropertyTypeName);
                if (type == null)
                    return false;

                // Strings are IEnumerable but not considered collections for our purposes
                if (type == typeof(string))
                    return false;

                // Check for arrays
                if (type.IsArray)
                    return true;

                // Check for IEnumerable<T> (List<T>, IList<T>, etc.)
                if (type.IsGenericType)
                {
                    var genericDef = type.GetGenericTypeDefinition();
                    if (genericDef == typeof(List<>) ||
                        genericDef == typeof(IList<>) ||
                        genericDef == typeof(ICollection<>) ||
                        genericDef == typeof(IEnumerable<>))
                        return true;
                }

                // Check if type implements IEnumerable (but isn't string)
                return typeof(IEnumerable).IsAssignableFrom(type);
            }
        }
    }
}
