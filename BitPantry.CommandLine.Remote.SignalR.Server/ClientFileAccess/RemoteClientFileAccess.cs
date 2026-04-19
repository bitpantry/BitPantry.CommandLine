using System.IO.Abstractions;
using System.Runtime.CompilerServices;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BitPantry.CommandLine.Remote.SignalR.Server.ClientFileAccess
{
    /// <summary>
    /// Server-side implementation of <see cref="IClientFileAccess"/> that coordinates file
    /// transfers between the server and a connected client via SignalR push messages and
    /// the existing HTTP file transfer endpoints.
    /// </summary>
    public class RemoteClientFileAccess : IClientFileAccess
    {
        private const string StagingDirectory = ".client-file-staging";
        private const int BufferSize = 81920;

        private readonly HubInvocationContext _hubInvocationContext;
        private readonly IFileSystem _fileSystem;
        private readonly FileTransferOptions _fileTransferOptions;
        private readonly ILogger<RemoteClientFileAccess> _logger;

        public RemoteClientFileAccess(
            HubInvocationContext hubInvocationContext,
            IFileSystem fileSystem,
            FileTransferOptions fileTransferOptions,
            ILogger<RemoteClientFileAccess> logger)
        {
            _hubInvocationContext = hubInvocationContext ?? throw new ArgumentNullException(nameof(hubInvocationContext));
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _fileTransferOptions = fileTransferOptions ?? throw new ArgumentNullException(nameof(fileTransferOptions));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<ClientFile> GetFileAsync(string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            var ctx = _hubInvocationContext.Current
                ?? throw new InvalidOperationException("No hub invocation context available — cannot access client files outside of a hub invocation.");

            var tempFileName = $"{Guid.NewGuid():N}";
            var tempPath = _fileSystem.Path.Combine(StagingDirectory, tempFileName);

            // Ensure the staging directory exists
            var stagingDir = _fileSystem.Path.GetDirectoryName(tempPath);
            if (!string.IsNullOrEmpty(stagingDir) && !_fileSystem.Directory.Exists(stagingDir))
                _fileSystem.Directory.CreateDirectory(stagingDir);

            var rpcCtx = ctx.RpcMessageRegistry.Register();

            try
            {
                var msg = new ClientFileUploadRequestMessage(clientPath, tempPath);
                msg.CorrelationId = rpcCtx.CorrelationId;

                _logger.LogDebug("Requesting client file upload: {ClientPath} -> {TempPath}, correlationId={CorrelationId}",
                    clientPath, tempPath, rpcCtx.CorrelationId);

                await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, msg, ct);

                var response = await rpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>().WaitAsync(ct);

                if (!response.Success)
                {
                    CleanupTempFile(tempPath);
                    throw MapError(response.Error, clientPath);
                }

                var fileInfo = _fileSystem.FileInfo.New(tempPath);
                var length = fileInfo.Length;

                // Validate file size after upload
                if (length > _fileTransferOptions.MaxFileSizeBytes)
                {
                    CleanupTempFile(tempPath);
                    throw new InvalidOperationException(
                        $"File '{clientPath}' ({length} bytes) exceeds the maximum allowed size of {_fileTransferOptions.MaxFileSizeBytes} bytes.");
                }

                var stream = _fileSystem.File.OpenRead(tempPath);

                progress?.Report(new FileTransferProgress(length, length));

                return new ClientFile(stream, _fileSystem.Path.GetFileName(clientPath), length,
                    () => { CleanupTempFile(tempPath); return default; });
            }
            catch
            {
                CleanupTempFile(tempPath);
                throw;
            }
        }

        public async IAsyncEnumerable<ClientFile> GetFilesAsync(
            string clientGlobPattern,
            IProgress<FileTransferProgress> progress = null,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            ValidateGlobPattern(clientGlobPattern);

            var ctx = _hubInvocationContext.Current
                ?? throw new InvalidOperationException("No hub invocation context available — cannot access client files outside of a hub invocation.");

            // Send enumerate request to client
            var enumerateRpcCtx = ctx.RpcMessageRegistry.Register();

            var enumerateMsg = new ClientFileEnumerateRequestMessage(clientGlobPattern);
            enumerateMsg.CorrelationId = enumerateRpcCtx.CorrelationId;

            _logger.LogDebug("Requesting client file enumeration: {GlobPattern}, correlationId={CorrelationId}",
                clientGlobPattern, enumerateRpcCtx.CorrelationId);

            await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, enumerateMsg, ct);

            var enumerateResponse = await enumerateRpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>().WaitAsync(ct);

            if (!enumerateResponse.Success)
                throw MapError(enumerateResponse.Error, clientGlobPattern);

            var fileEntries = enumerateResponse.FileInfoEntries;

            // Yield individual files lazily — each triggers a separate upload request
            foreach (var entry in fileEntries)
            {
                ct.ThrowIfCancellationRequested();
                yield return await GetFileAsync(entry.Path, progress, ct);
            }
        }

        public async Task SaveFileAsync(Stream content, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            var ctx = _hubInvocationContext.Current
                ?? throw new InvalidOperationException("No hub invocation context available — cannot access client files outside of a hub invocation.");

            // Write content to staging temp file
            var tempFileName = $"{Guid.NewGuid():N}";
            var tempPath = _fileSystem.Path.Combine(StagingDirectory, tempFileName);

            var stagingDir = _fileSystem.Path.GetDirectoryName(tempPath);
            if (!string.IsNullOrEmpty(stagingDir) && !_fileSystem.Directory.Exists(stagingDir))
                _fileSystem.Directory.CreateDirectory(stagingDir);

            try
            {
                long bytesWritten = 0;
                long? totalBytes = null;
                if (content.CanSeek)
                {
                    totalBytes = content.Length;

                    // Validate file size before writing
                    if (totalBytes > _fileTransferOptions.MaxFileSizeBytes)
                    {
                        throw new InvalidOperationException(
                            $"Content ({totalBytes} bytes) exceeds the maximum allowed size of {_fileTransferOptions.MaxFileSizeBytes} bytes.");
                    }
                }

                using (var fileStream = _fileSystem.FileStream.New(tempPath, FileMode.Create, FileAccess.Write))
                {
                    var buffer = new byte[BufferSize];
                    int bytesRead;

                    while ((bytesRead = await content.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead, ct).ConfigureAwait(false);
                        bytesWritten += bytesRead;

                        // Check file size during write for non-seekable streams
                        if (bytesWritten > _fileTransferOptions.MaxFileSizeBytes)
                        {
                            throw new InvalidOperationException(
                                $"Content exceeds the maximum allowed size of {_fileTransferOptions.MaxFileSizeBytes} bytes.");
                        }

                        progress?.Report(new FileTransferProgress(bytesWritten, totalBytes));
                    }
                }

                var fileInfo = _fileSystem.FileInfo.New(tempPath);
                var fileSize = fileInfo.Length;

                var rpcCtx = ctx.RpcMessageRegistry.Register();

                var msg = new ClientFileDownloadRequestMessage(tempPath, clientPath, fileSize);
                msg.CorrelationId = rpcCtx.CorrelationId;

                _logger.LogDebug("Requesting client file download: {TempPath} -> {ClientPath}, size={FileSize}, correlationId={CorrelationId}",
                    tempPath, clientPath, fileSize, rpcCtx.CorrelationId);

                await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, msg, ct);

                var response = await rpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>().WaitAsync(ct);

                if (!response.Success)
                    throw MapError(response.Error, clientPath);
            }
            finally
            {
                CleanupTempFile(tempPath);
            }
        }

        public async Task SaveFileAsync(string sourcePath, string clientPath, IProgress<FileTransferProgress> progress = null, CancellationToken ct = default)
        {
            var ctx = _hubInvocationContext.Current
                ?? throw new InvalidOperationException("No hub invocation context available — cannot access client files outside of a hub invocation.");

            // Validate source file exists
            if (!_fileSystem.File.Exists(sourcePath))
                throw new FileNotFoundException($"Source file not found: {sourcePath}", sourcePath);

            var fileInfo = _fileSystem.FileInfo.New(sourcePath);
            var fileSize = fileInfo.Length;

            // Validate file size before transfer
            if (fileSize > _fileTransferOptions.MaxFileSizeBytes)
            {
                throw new InvalidOperationException(
                    $"File '{sourcePath}' ({fileSize} bytes) exceeds the maximum allowed size of {_fileTransferOptions.MaxFileSizeBytes} bytes.");
            }

            var rpcCtx = ctx.RpcMessageRegistry.Register();

            var msg = new ClientFileDownloadRequestMessage(sourcePath, clientPath, fileSize);
            msg.CorrelationId = rpcCtx.CorrelationId;

            _logger.LogDebug("Requesting client file download from source: {SourcePath} -> {ClientPath}, size={FileSize}, correlationId={CorrelationId}",
                sourcePath, clientPath, fileSize, rpcCtx.CorrelationId);

            await ctx.ClientProxy.SendAsync(SignalRMethodNames.ReceiveMessage, msg, ct);

            var response = await rpcCtx.WaitForCompletion<ClientFileAccessResponseMessage>().WaitAsync(ct);

            if (!response.Success)
                throw MapError(response.Error, clientPath);

            progress?.Report(new FileTransferProgress(fileSize, fileSize));
        }

        private void CleanupTempFile(string tempPath)
        {
            try
            {
                if (_fileSystem.File.Exists(tempPath))
                    _fileSystem.File.Delete(tempPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up staging temp file: {TempPath}", tempPath);
            }
        }

        private static void ValidateGlobPattern(string pattern)
        {
            var segments = pattern.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(segment => segment == ".."))
                throw new ArgumentException($"Glob pattern must not contain path traversal: '{pattern}'", nameof(pattern));
        }

        private static Exception MapError(string error, string path)
        {
            if (error != null && error.Contains("not found", StringComparison.OrdinalIgnoreCase))
                return new FileNotFoundException(error, path);

            if (error != null && error.Contains("denied", StringComparison.OrdinalIgnoreCase))
                return new FileAccessDeniedException(path, error);

            return new InvalidOperationException(error ?? $"Client file access failed for '{path}'.");
        }
    }
}
