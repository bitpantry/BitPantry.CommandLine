using System.Threading;

namespace BitPantry.CommandLine.API
{
    /// <summary>
    /// Contains information representing the context of the currently executing command process
    /// </summary>
    public class CommandExecutionContext
    {
        /// <summary>
        /// The cancellation token scoped to the currently executing command process
        /// </summary>
        public CancellationToken CancellationToken { get; internal set; }

        /// <summary>
        /// The command registry for the command line application
        /// </summary>
        public CommandRegistry CommandRegistry { get; internal set; }
        
    }

    /// <summary>
    /// Contains information representing the context of the currently executing command process
    /// </summary>
    /// <typeparam name="T">The data type of the input data that can be passed into this command</typeparam>
    public class CommandExecutionContext<T> : CommandExecutionContext
    {

        /// <summary>
        /// The input passed into this command. If no input is available from the pipeline, default(T) is used
        /// </summary>
        public T Input { get; internal set; }

        public CommandExecutionContext(object input)
        {
            Input = input == null
                ? default(T)
                : (T)input;
        }
    }
}
