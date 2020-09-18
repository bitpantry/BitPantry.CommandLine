using BitPantry.CommandLine.Processing.Parsing;
using System;

namespace BitPantry.CommandLine.Processing.Resolution
{
    public class CommandResolutionException : Exception
    {
        /// <summary>
        /// The input being resolved when the exception occured
        /// </summary>
        public ParsedInput Input { get; }

        /// <summary>
        /// Creates a new CommandResolutionException
        /// </summary>
        /// <param name="input">The input being resolved when the exception occured</param>
        /// <param name="message">The exception message</param>
        public CommandResolutionException(ParsedInput input, string message) : base(message)
        {
            Input = input;
        }

        /// <summary>
        /// Creates a new CommandResolutionException
        /// </summary>
        /// <param name="input">The input being resolved when the exception occured</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException"
        public CommandResolutionException(ParsedInput input, string message, Exception innerException) : base(message, innerException)
        {
            Input = input;
        }

    }
}
