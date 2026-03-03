using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    /// <summary>
    /// Browses the server's file system by sending <see cref="EnumeratePathEntriesRequest"/>
    /// RPC requests through the connected <see cref="IServerProxy"/>.
    /// Used by <see cref="RemotePathEntryProvider"/> when Server* autocomplete
    /// attributes are active on the client side.
    /// </summary>
    public class ServerFileSystemBrowser : IRemoteFileSystemBrowser
    {
        private readonly IServerProxy _proxy;
        private readonly ILogger<ServerFileSystemBrowser> _logger;

        public ServerFileSystemBrowser(
            IServerProxy proxy,
            ILogger<ServerFileSystemBrowser> logger)
        {
            _proxy = proxy;
            _logger = logger;
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PathEntry>> EnumeratePathEntriesAsync(
            string directoryPath,
            bool includeFiles,
            CancellationToken cancellationToken = default)
        {
            _logger.LogDebug(
                "Enumerating server path entries at {DirectoryPath}, includeFiles={IncludeFiles}",
                directoryPath, includeFiles);

            var request = new EnumeratePathEntriesRequest(directoryPath, includeFiles);
            var response = await _proxy.SendRpcRequest<EnumeratePathEntriesResponse>(request, cancellationToken);

            if (!string.IsNullOrEmpty(response.Error))
            {
                _logger.LogWarning("Server returned error for EnumeratePathEntries: {Error}", response.Error);
                return [];
            }

            _logger.LogDebug("Server returned {Count} path entries", response.Entries.Length);
            return response.Entries;
        }

        /// <inheritdoc/>
        public async Task<string> GetCurrentDirectoryAsync(CancellationToken cancellationToken = default)
        {
            // The server resolves empty DirectoryPath to its storage root,
            // so from the client's perspective, the server's "current directory" is always
            // the root denoted by empty string. Actual resolution happens server-side.
            await Task.CompletedTask;
            return string.Empty;
        }
    }
}
