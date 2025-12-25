using BitPantry.CommandLine.Processing.Parsing;

namespace BitPantry.CommandLine.Processing.Resolution
{
    /// <summary>
    /// Types of command resolution errors
    /// </summary>
    public enum CommandResolutionErrorType
    {
        /// <summary>
        /// For the input, a command could not be found
        /// </summary>
        CommandNotFound,

        /// <summary>
        /// A defined argument could not be found on the command
        /// </summary>
        ArgumentNotFound,

        /// <summary>
        /// A defined element has an unexpected value associated to it - e.g., an option argument cannot have an associated value
        /// </summary>
        UnexpectedValue,

        /// <summary>
        /// An argument has been referenced by name and by alias
        /// </summary>
        DuplicateArgument,

        /// <summary>
        /// A required positional argument was not provided
        /// </summary>
        MissingRequiredPositional,

        /// <summary>
        /// More positional values were provided than the command accepts (and no IsRest argument exists)
        /// </summary>
        ExcessPositionalValues,

        /// <summary>
        /// A scalar (non-collection) argument was specified multiple times (--opt a --opt b on non-collection property)
        /// </summary>
        DuplicateScalarArgument
    }

    public class ResolveCommandError
    {
        public CommandResolutionErrorType Type { get; internal set; }
        public ParsedCommandElement Element { get; internal set; }
        public string Message { get; internal set; }
    }
}
