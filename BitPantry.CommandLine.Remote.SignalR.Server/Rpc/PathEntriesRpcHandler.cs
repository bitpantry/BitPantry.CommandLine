using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.AutoComplete;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Rpc
{
    /// <summary>
    /// Handles path entry enumeration RPC requests from clients.
    /// Lists directories (and optionally files) within a given directory
    /// on the server's sandboxed file system.
    /// </summary>
    public class PathEntriesRpcHandler
    {
        private readonly ILogger<PathEntriesRpcHandler> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly PathValidator _pathValidator;

        public PathEntriesRpcHandler(
            ILogger<PathEntriesRpcHandler> logger,
            IFileSystem fileSystem,
            FileTransferOptions options)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _pathValidator = new PathValidator(options.StorageRootPath);
        }

        /// <summary>
        /// Handles an EnumeratePathEntries request.
        /// </summary>
        public async Task HandleEnumeratePathEntries(IClientProxy client, EnumeratePathEntriesRequest request)
        {
            _logger.LogDebug("EnumeratePathEntries request :: directoryPath={DirectoryPath}, includeFiles={IncludeFiles}",
                request.DirectoryPath, request.IncludeFiles);

            try
            {
                // Resolve directory path — empty means storage root
                string validatedPath;
                try
                {
                    var dirPath = string.IsNullOrEmpty(request.DirectoryPath) 
                        ? _pathValidator.StorageRoot 
                        : request.DirectoryPath;
                    validatedPath = _pathValidator.ValidatePath(dirPath);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Path traversal attempt in EnumeratePathEntries: {DirectoryPath}", request.DirectoryPath);
                    await SendResponse(client, request.CorrelationId, null, "Path traversal is not allowed");
                    return;
                }

                // Check directory exists
                if (!_fileSystem.Directory.Exists(validatedPath))
                {
                    _logger.LogWarning("Directory not found in EnumeratePathEntries: {DirectoryPath}", validatedPath);
                    await SendResponse(client, request.CorrelationId, null, $"Directory not found: {request.DirectoryPath}");
                    return;
                }

                var entries = new List<PathEntry>();

                // Always enumerate directories
                var dirs = _fileSystem.Directory.GetDirectories(validatedPath);
                foreach (var dir in dirs)
                {
                    entries.Add(new PathEntry(_fileSystem.Path.GetFileName(dir), IsDirectory: true));
                }

                // Enumerate files only when requested
                if (request.IncludeFiles)
                {
                    var files = _fileSystem.Directory.GetFiles(validatedPath);
                    foreach (var file in files)
                    {
                        entries.Add(new PathEntry(_fileSystem.Path.GetFileName(file), IsDirectory: false));
                    }
                }

                _logger.LogDebug("EnumeratePathEntries found {Count} entries", entries.Count);
                await SendResponse(client, request.CorrelationId, entries.ToArray(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnumeratePathEntries for path {DirectoryPath}", request.DirectoryPath);
                await SendResponse(client, request.CorrelationId, null, $"Error enumerating path entries: {ex.Message}");
            }
        }

        private async Task SendResponse(IClientProxy client, string correlationId, PathEntry[] entries, string error)
        {
            var response = new EnumeratePathEntriesResponse(correlationId, entries, error);
            await client.SendAsync(SignalRMethodNames.ReceiveResponse, response);
        }
    }
}
