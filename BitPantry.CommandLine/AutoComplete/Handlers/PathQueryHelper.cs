namespace BitPantry.CommandLine.AutoComplete.Handlers;

/// <summary>
/// Shared utility for splitting path-style autocomplete queries into a directory
/// prefix and a filename/folder fragment. Used by both local (<see cref="PathAutoCompleteHandlerBase"/>)
/// and remote (SemanticPathAutoCompleteHandlerBase) path handlers.
/// </summary>
public static class PathQueryHelper
{
    /// <summary>
    /// Splits a query string into a directory path prefix and a filename fragment.
    /// For "dir/sub/fi" → ("dir/sub/", "fi")
    /// For "fi" → ("", "fi")
    /// For "dir/" → ("dir/", "")
    /// </summary>
    public static (string directoryPath, string fragment) SplitQueryIntoDirectoryAndFragment(string query)
    {
        if (string.IsNullOrEmpty(query))
            return (string.Empty, string.Empty);

        var lastSepIndex = query.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSepIndex < 0)
            return (string.Empty, query);

        var directoryPath = query.Substring(0, lastSepIndex + 1);
        var fragment = query.Substring(lastSepIndex + 1);
        return (directoryPath, fragment);
    }
}
