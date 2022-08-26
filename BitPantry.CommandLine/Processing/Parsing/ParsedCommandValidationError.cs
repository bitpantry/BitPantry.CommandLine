namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Input validation error types
    /// </summary>
    public enum ParsedCommandValidationErrorType
    {
        InvalidAlias,
        NoCommandElement
    }

    public class ParsedCommandValidationError
    {
        public ParsedCommandValidationErrorType Type { get; internal set; }
        public ParsedCommandElement Element { get; internal set; }
        public string Message { get; internal set; }

    }
}
