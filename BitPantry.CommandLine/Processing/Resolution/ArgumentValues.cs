using System.Collections.Generic;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// Wrapper class that holds the values for a single argument, supporting both single and multiple values.
    /// Used to represent values from positional arguments, named arguments, and repeated options.
    /// </summary>
    public class ArgumentValues
    {
        private readonly List<string> _values = new List<string>();

        /// <summary>
        /// Gets the list of all values for this argument
        /// </summary>
        public IReadOnlyList<string> Values => _values.AsReadOnly();

        /// <summary>
        /// Gets whether this argument has any values
        /// </summary>
        public bool HasValues => _values.Count > 0;

        /// <summary>
        /// Gets the count of values
        /// </summary>
        public int Count => _values.Count;

        /// <summary>
        /// Gets the first value, or null if no values exist
        /// </summary>
        public string FirstValue => _values.Count > 0 ? _values[0] : null;

        /// <summary>
        /// Creates an empty ArgumentValues instance
        /// </summary>
        public ArgumentValues()
        {
        }

        /// <summary>
        /// Creates an ArgumentValues instance with a single value
        /// </summary>
        public ArgumentValues(string value)
        {
            if (value != null)
                _values.Add(value);
        }

        /// <summary>
        /// Creates an ArgumentValues instance with multiple values
        /// </summary>
        public ArgumentValues(IEnumerable<string> values)
        {
            if (values != null)
                _values.AddRange(values);
        }

        /// <summary>
        /// Appends a value to the list. Used for repeated options (--opt a --opt b)
        /// </summary>
        public void Append(string value)
        {
            if (value != null)
                _values.Add(value);
        }

        /// <summary>
        /// Appends multiple values to the list. Used for IsRest positional arguments
        /// </summary>
        public void AppendRange(IEnumerable<string> values)
        {
            if (values != null)
                _values.AddRange(values);
        }

        /// <summary>
        /// Returns the values as an array
        /// </summary>
        public string[] ToArray() => _values.ToArray();

        public override string ToString()
        {
            return _values.Count == 1 ? _values[0] : $"[{string.Join(", ", _values)}]";
        }
    }
}
