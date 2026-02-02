using System;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// A convenience exception class that implements <see cref="IUserFacingException"/>.
    /// Use this when you want to throw an exception with a message intended for end-user display.
    /// For remote commands, this exception's details will be fully serialized and rendered
    /// on the client using Spectre.Console's exception formatting.
    /// </summary>
    public class UserFacingException : Exception, IUserFacingException
    {
        /// <summary>
        /// Creates a new UserFacingException with the specified message.
        /// </summary>
        /// <param name="message">A user-appropriate error message.</param>
        public UserFacingException(string message) : base(message)
        {
        }

        /// <summary>
        /// Creates a new UserFacingException with the specified message and inner exception.
        /// </summary>
        /// <param name="message">A user-appropriate error message.</param>
        /// <param name="innerException">The inner exception that caused this exception.</param>
        public UserFacingException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
