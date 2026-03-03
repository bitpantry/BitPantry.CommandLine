using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Provides remote file system browsing over RPC (SignalR).
/// Implemented differently on client (sends ServerRequest) and server (sends ClientRequest).
/// </summary>
public interface IRemoteFileSystemBrowser
{
    /// <summary>
    /// Enumerates path entries in a directory on the remote peer's file system.
    /// </summary>
    /// <param name="directoryPath">Directory path to enumerate (empty = current directory on remote).</param>
    /// <param name="includeFiles">Whether to include files in addition to directories.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of path entries from the remote peer.</returns>
    Task<IReadOnlyList<PathEntry>> EnumeratePathEntriesAsync(
        string directoryPath,
        bool includeFiles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current working directory from the remote peer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The remote current working directory path.</returns>
    Task<string> GetCurrentDirectoryAsync(CancellationToken cancellationToken = default);
}
