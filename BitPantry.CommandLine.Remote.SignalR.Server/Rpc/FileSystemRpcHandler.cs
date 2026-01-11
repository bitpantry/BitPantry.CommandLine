using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Configuration;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.FileSystemGlobbing;
using GlobbingDirectoryInfoWrapper = Microsoft.Extensions.FileSystemGlobbing.Abstractions.DirectoryInfoWrapper;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Remote.SignalR.Server.Rpc
{
    /// <summary>
    /// Handles file system RPC operations from clients.
    /// </summary>
    public class FileSystemRpcHandler
    {
        private readonly ILogger<FileSystemRpcHandler> _logger;
        private readonly IFileSystem _fileSystem;
        private readonly FileTransferOptions _options;
        private readonly Files.PathValidator _pathValidator;

        public FileSystemRpcHandler(
            ILogger<FileSystemRpcHandler> logger,
            IFileSystem fileSystem,
            FileTransferOptions options)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _options = options;
            _pathValidator = new Files.PathValidator(options.StorageRootPath);
        }

        /// <summary>
        /// Handles EnumerateFiles requests, returning file information including size and dates.
        /// </summary>
        public async Task HandleEnumerateFiles(IClientProxy client, EnumerateFilesRequest request)
        {
            _logger.LogDebug("EnumerateFiles request :: path={Path}, pattern={Pattern}, option={Option}",
                request.Path, request.SearchPattern, request.SearchOption);

            try
            {
                // Validate path to prevent traversal attacks
                string validatedPath;
                try
                {
                    validatedPath = _pathValidator.ValidatePath(request.Path ?? "");
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogWarning("Path traversal attempt in EnumerateFiles: {Path}", request.Path);
                    await SendResponse(client, request.CorrelationId, null, "Path traversal is not allowed");
                    return;
                }

                // Check directory exists
                if (!_fileSystem.Directory.Exists(validatedPath))
                {
                    _logger.LogWarning("Directory not found in EnumerateFiles: {Path}", validatedPath);
                    await SendResponse(client, request.CorrelationId, null, $"Directory not found: {request.Path}");
                    return;
                }

                // Parse search option
                var searchOption = request.SearchOption == "AllDirectories"
                    ? SearchOption.AllDirectories
                    : SearchOption.TopDirectoryOnly;

                // Use Microsoft.Extensions.FileSystemGlobbing for pattern matching
                var matcher = new Matcher();
                
                // Handle ** patterns for recursive search
                var pattern = request.SearchPattern ?? "*";
                if (pattern.Contains("**"))
                {
                    searchOption = SearchOption.AllDirectories;
                }
                
                matcher.AddInclude(pattern);

                var directoryInfo = new DirectoryInfo(validatedPath);
                var matchResult = matcher.Execute(new GlobbingDirectoryInfoWrapper(directoryInfo));

                var files = new List<FileInfoEntry>();
                
                foreach (var match in matchResult.Files)
                {
                    var fullPath = _fileSystem.Path.Combine(validatedPath, match.Path);
                    
                    // Validate each matched path to prevent traversal via patterns
                    try
                    {
                        _pathValidator.ValidatePath(fullPath);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        continue; // Skip files that would escape sandbox
                    }

                    if (_fileSystem.File.Exists(fullPath))
                    {
                        var fileInfo = _fileSystem.FileInfo.New(fullPath);
                        files.Add(new FileInfoEntry(
                            match.Path, // Use relative path from search root
                            fileInfo.Length,
                            fileInfo.LastWriteTimeUtc));
                    }
                }

                _logger.LogDebug("EnumerateFiles found {Count} files matching pattern", files.Count);
                await SendResponse(client, request.CorrelationId, files.ToArray(), null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EnumerateFiles for path {Path}", request.Path);
                await SendResponse(client, request.CorrelationId, null, $"Error enumerating files: {ex.Message}");
            }
        }

        private async Task SendResponse(IClientProxy client, string correlationId, FileInfoEntry[] files, string error)
        {
            var response = new EnumerateFilesResponse(correlationId, files, error);
            await client.SendAsync(SignalRMethodNames.ReceiveResponse, response);
        }
    }
}
