using System;

namespace BitPantry.CommandLine.Processing.Description
{
    /// <summary>
    /// An exception that has occured during a command reflection operation
    /// </summary>
    public class CommandDescriptionException : Exception
    {
        /// <summary>
        /// The type that was being processed when the exception occured
        /// </summary>
        public Type CommandType { get; private set; }

        /// <summary>
        /// Creates a new CommandDescriptionException
        /// </summary>
        /// <param name="commandType">The type being processed when the exception occured</param>
        /// <param name="message">The exception message</param>
        public CommandDescriptionException(Type commandType, string message) : base(message)
        {
            CommandType = commandType;
        }

        /// <summary>
        /// Creates a new CommandDescriptionException
        /// </summary>
        /// <param name="commandType">The type being processed when the exception occured</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException">The inner exception</param>
        public CommandDescriptionException(Type commandType, string message, Exception innerException) : base(message, innerException)
        {
            CommandType = commandType;
        }
    }
}
