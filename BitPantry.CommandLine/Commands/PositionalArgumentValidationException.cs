using System;

namespace BitPantry.CommandLine.Commands
{
    /// <summary>
    /// Exception thrown when positional argument validation fails during command description/registration.
    /// </summary>
    public class PositionalArgumentValidationException : Exception
    {
        /// <summary>
        /// The type of the command that failed validation
        /// </summary>
        public Type CommandType { get; }

        /// <summary>
        /// The name of the property that caused the validation failure (if applicable)
        /// </summary>
        public string PropertyName { get; }

        /// <summary>
        /// Creates a new PositionalArgumentValidationException
        /// </summary>
        /// <param name="commandType">The type of the command that failed validation</param>
        /// <param name="message">The validation error message</param>
        /// <param name="propertyName">The name of the property that caused the validation failure</param>
        public PositionalArgumentValidationException(Type commandType, string message, string propertyName = null)
            : base(FormatMessage(commandType, message, propertyName))
        {
            CommandType = commandType;
            PropertyName = propertyName;
        }

        private static string FormatMessage(Type commandType, string message, string propertyName)
        {
            var typeName = commandType?.Name ?? "Unknown";
            if (!string.IsNullOrEmpty(propertyName))
            {
                return $"Positional argument validation failed for command '{typeName}' on property '{propertyName}': {message}";
            }
            return $"Positional argument validation failed for command '{typeName}': {message}";
        }
    }
}
