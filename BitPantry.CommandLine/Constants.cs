namespace BitPantry.CommandLine
{
    public class CommandInputParsingConstants
    {
        public static readonly string[] ElementPrefixes = new string[]
        {
            ElementPrefixArgument,
            ElementPrefixAlias
        };

        public const string ElementPrefixArgument = "--";
        public const string ElementPrefixAlias = "-";
    }
}
