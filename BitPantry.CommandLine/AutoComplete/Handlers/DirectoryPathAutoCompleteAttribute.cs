namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Attribute that binds a command property to directory-only path autocomplete.
/// Syntactic sugar for [AutoComplete&lt;DirectoryPathAutoCompleteHandler&gt;].
/// </summary>
public class DirectoryPathAutoCompleteAttribute : AutoCompleteAttribute<DirectoryPathAutoCompleteHandler>
{
}
