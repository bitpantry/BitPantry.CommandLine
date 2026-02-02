using System;
using Spectre.Console;

namespace BitPantry.CommandLine.API
{
    public abstract class CommandBase
    {
        protected IAnsiConsole Console { get; private set; }

        internal void SetConsole(IAnsiConsole console)
        {
            Console = console;
        }

        /// <summary>
        /// Throws a user-facing exception with the specified message.
        /// The message will be displayed to the user (including over remote connections).
        /// </summary>
        /// <param name="message">The user-friendly error message to display.</param>
        protected void Fail(string message)
            => throw new UserFacingException(message);

        /// <summary>
        /// Throws a user-facing exception with the specified message and inner exception.
        /// The message will be displayed to the user (including over remote connections).
        /// </summary>
        /// <param name="message">The user-friendly error message to display.</param>
        /// <param name="innerException">The underlying exception that caused this failure.</param>
        protected void Fail(string message, Exception innerException)
            => throw new UserFacingException(message, innerException);
    }
}
