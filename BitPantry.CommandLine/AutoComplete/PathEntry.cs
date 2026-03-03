namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Represents a single entry (file or directory) returned from path enumeration.
/// Used by <see cref="IPathEntryProvider"/> implementations to communicate results
/// without coupling to any specific file system abstraction.
/// </summary>
/// <param name="Name">The entry name (not full path).</param>
/// <param name="IsDirectory">True if the entry is a directory; false if a file.</param>
public record PathEntry(string Name, bool IsDirectory);
