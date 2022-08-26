using BitPantry.CommandLine.Processing.Parsing;
using System;

namespace BitPantry.CommandLine.Processing.Resolution
{
    public class InputResolutionException : Exception
    {
        /// <summary>
        /// The input being resolved when the exception occured
        /// </summary>
        public ParsedInput ParsedInput { get; }

        /// <summary>
        /// Creates a new InputResolutionException
        /// </summary>
        /// <param name="parsedInput">The input being resolved when the exception occured</param>
        /// <param name="message">The exception message</param>
        public InputResolutionException(ParsedInput parsedInput, string message) : base(message)
        {
            ParsedInput = parsedInput;
        }

        /// <summary>
        /// Creates a new InputResolutionException
        /// </summary>
        /// <param name="parsedInput">The input being resolved when the exception occured</param>
        /// <param name="message">The exception message</param>
        /// <param name="innerException"
        public InputResolutionException(ParsedInput parsedInput, string message, Exception innerException) : base(message, innerException)
        {
            ParsedInput = parsedInput;
        }

    }
}
