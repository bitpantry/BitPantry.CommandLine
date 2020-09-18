using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Processing.Resolution;

namespace BitPantry.CommandLine.Processing.Activation
{
    /// <summary>
    /// The results of a command activation
    /// </summary>
    public class ActivationResult
    {
        /// <summary>
        /// The command object that was activated
        /// </summary>
        public CommandBase Command { get; private set; }

        /// <summary>
        /// The ResolvedCommand which was used to activate the command object
        /// </summary>
        public ResolvedCommand ResolvedCommand { get; private set; }

        internal ActivationResult(
            CommandBase command,
            ResolvedCommand resolvedCommand)
        {
            Command = command;
            ResolvedCommand = resolvedCommand;
        }
    }
}
