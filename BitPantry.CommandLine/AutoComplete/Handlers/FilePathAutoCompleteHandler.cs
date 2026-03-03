namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Built-in handler for autocompleting file system paths (directories and files).
/// Uses <see cref="IPathEntryProvider"/> for transparent local/remote file system access.
/// Bound via [FilePathAutoComplete] attribute — not a type handler since string is too broad.
/// </summary>
public class FilePathAutoCompleteHandler : PathAutoCompleteHandlerBase
{
    /// <summary>
    /// Creates a new FilePathAutoCompleteHandler.
    /// </summary>
    /// <param name="provider">The path entry provider for directory/file enumeration.</param>
    /// <param name="theme">The theme providing directory styling.</param>
    public FilePathAutoCompleteHandler(IPathEntryProvider provider, Theme theme)
        : base(provider, theme, includeFiles: true)
    {
    }
}

