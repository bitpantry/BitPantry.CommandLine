namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Attribute that binds a command property to file path autocomplete.
/// Syntactic sugar for [AutoComplete&lt;FilePathAutoCompleteHandler&gt;].
/// </summary>
public class FilePathAutoCompleteAttribute : AutoCompleteAttribute<FilePathAutoCompleteHandler>
{
}
