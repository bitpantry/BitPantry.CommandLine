using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Remote.SignalR.AutoComplete;

/// <summary>
/// Provides path entries from a remote peer via <see cref="IRemoteFileSystemBrowser"/>.
/// Used when the target filesystem is on the other side of the connection.
/// Returns empty results when the remote peer is unavailable or on errors.
/// </summary>
public class RemotePathEntryProvider : IPathEntryProvider
{
    private readonly IRemoteFileSystemBrowser _browser;

    public RemotePathEntryProvider(IRemoteFileSystemBrowser browser)
    {
        _browser = browser ?? throw new ArgumentNullException(nameof(browser));
    }

    /// <inheritdoc />
    public string GetCurrentDirectory()
    {
        // For remote, we use empty string to signal "use remote CWD".
        // The remote side resolves this to its storage root / CWD.
        // This avoids a synchronous-over-async RPC call.
        return string.Empty;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PathEntry>> EnumerateAsync(
        string directoryPath,
        bool includeFiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _browser.EnumeratePathEntriesAsync(directoryPath, includeFiles, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return Array.Empty<PathEntry>();
        }
        catch (Exception)
        {
            // Gracefully degrade on RPC errors
            return Array.Empty<PathEntry>();
        }
    }
}
