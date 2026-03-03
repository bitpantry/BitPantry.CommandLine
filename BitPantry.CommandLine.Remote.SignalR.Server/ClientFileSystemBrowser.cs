using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Server
{
    /// <summary>
    /// Browses the client's file system by sending <see cref="ClientEnumeratePathEntriesRequest"/>
    /// RPC requests through the connected client.
    /// Used on the server when Client* autocomplete attributes need to reach
    /// the connected client's local file system.
    /// </summary>
    /// <remarks>
    /// Reads the per-request <c>IClientProxy</c> and <c>RpcMessageRegistry</c> from
    /// <see cref="ClientConnectionContext"/> (set by the hub before handler activation).
    /// Register as <strong>singleton</strong>; safe because all mutable state lives in the
    /// ambient <c>AsyncLocal</c> context, not on this instance.
    /// </remarks>
    public class ClientFileSystemBrowser : IRemoteFileSystemBrowser
    {
        private readonly HubInvocationContext _invocationContext;
        private readonly ILogger<ClientFileSystemBrowser> _logger;

        public ClientFileSystemBrowser(
            HubInvocationContext invocationContext,
            ILogger<ClientFileSystemBrowser> logger)
        {
            _invocationContext = invocationContext ?? throw new ArgumentNullException(nameof(invocationContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<IReadOnlyList<PathEntry>> EnumeratePathEntriesAsync(
            string directoryPath,
            bool includeFiles,
            CancellationToken cancellationToken = default)
        {
            var ctx = _invocationContext.Current
                ?? throw new InvalidOperationException("No hub invocation context available — cannot enumerate client path entries");

            try
            {
                _logger.LogDebug(
                    "Requesting client path entries at {DirectoryPath}, includeFiles={IncludeFiles}",
                    directoryPath, includeFiles);

                var request = new ClientEnumeratePathEntriesRequest(directoryPath, includeFiles);
                var response = await ctx.ClientProxy.Rpc<EnumeratePathEntriesResponse>(
                    ctx.RpcMessageRegistry, request, cancellationToken);

                if (!string.IsNullOrEmpty(response.Error))
                {
                    _logger.LogWarning("Client returned error for EnumeratePathEntries: {Error}", response.Error);
                    return [];
                }

                _logger.LogDebug("Client returned {Count} path entries", response.Entries.Length);
                return response.Entries;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to enumerate client path entries at {DirectoryPath}", directoryPath);
                return [];
            }
        }

        /// <inheritdoc/>
        public Task<string> GetCurrentDirectoryAsync(CancellationToken cancellationToken = default)
        {
            // The client resolves empty DirectoryPath to its actual current directory.
            // From the server's perspective, return empty string and let the client resolve it.
            return Task.FromResult(string.Empty);
        }
    }
}
