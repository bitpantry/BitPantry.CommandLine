namespace BitPantry.CommandLine.Processing.Parsing
{
    /// <summary>
    /// Input validation error types
    /// </summary>
    public enum ParsedInputValidationErrorType
    {
        InvalidAlias,
        NoCommandElement
    }

    public class ParsedInputValidationError
    {
        public ParsedInputValidationErrorType Type { get; internal set; }
        public ParsedInputElement Element { get; internal set; }
        public string Message { get; internal set; }
    }
}
