namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Built-in handler for autocompleting directory paths (excludes files).
/// Uses <see cref="IPathEntryProvider"/> for transparent local/remote file system access.
/// Bound via [DirectoryPathAutoComplete] attribute.
/// </summary>
public class DirectoryPathAutoCompleteHandler : PathAutoCompleteHandlerBase
{
    /// <summary>
    /// Creates a new DirectoryPathAutoCompleteHandler.
    /// </summary>
    /// <param name="provider">The path entry provider for directory enumeration.</param>
    /// <param name="theme">The theme providing directory styling.</param>
    public DirectoryPathAutoCompleteHandler(IPathEntryProvider provider, Theme theme)
        : base(provider, theme, includeFiles: false)
    {
    }
}
