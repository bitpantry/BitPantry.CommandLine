using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.AutoComplete;

/// <summary>
/// Provides path entry enumeration for autocomplete handlers.
/// Abstracts whether the entries come from a local file system
/// or from a remote peer via RPC.
/// </summary>
public interface IPathEntryProvider
{
    /// <summary>
    /// Gets the current working directory for the target file system.
    /// Used when the user provides an empty/relative query.
    /// </summary>
    string GetCurrentDirectory();

    /// <summary>
    /// Enumerates the immediate children of the given directory.
    /// </summary>
    /// <param name="directoryPath">Absolute path to enumerate.</param>
    /// <param name="includeFiles">Whether to include files in addition to directories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of path entries, or an empty list on error/unavailable.</returns>
    Task<IReadOnlyList<PathEntry>> EnumerateAsync(
        string directoryPath,
        bool includeFiles,
        CancellationToken cancellationToken = default);
}
