using BitPantry.CommandLine.Processing.Resolution;
using System;

namespace BitPantry.CommandLine.Processing.Activation
{
    public class CommandActivationException : Exception
    {
        /// <summary>
        /// The resolved command being activated when the error occured
        /// </summary>
        public ResolvedCommand Command { get; private set; }
        
        public CommandActivationException(ResolvedCommand command, string message) : base(message)
        {
            Command = command;
        }

        public CommandActivationException(ResolvedCommand command, string message, Exception innerException) : base(message, innerException)
        {
            Command = command;
        }

    }
}
