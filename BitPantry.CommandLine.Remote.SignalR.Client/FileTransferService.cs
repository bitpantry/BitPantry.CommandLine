using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class FileTransferService
    {
        private readonly ILogger<FileTransferService> _logger;
        private readonly IServerProxy _proxy;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AccessTokenManager _accessTokenMgr;
        private readonly FileUploadProgressUpdateFunctionRegistry _uploadReg;

        public FileTransferService(
            ILogger<FileTransferService> logger, 
            IServerProxy proxy, 
            IHttpClientFactory httpClientFactory, 
            AccessTokenManager accessTokenMgr, 
            FileUploadProgressUpdateFunctionRegistry uploadReg)
        {
            _logger = logger;
            _proxy = proxy;
            _httpClientFactory = httpClientFactory;
            _accessTokenMgr = accessTokenMgr;
            _uploadReg = uploadReg;
        }

        /// <summary>
        /// Computes SHA256 hash of a file incrementally.
        /// </summary>
        private static string ComputeFileChecksum(string filePath)
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            var buffer = new byte[81920];
            int bytesRead;
            
            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
            {
                hasher.AppendData(buffer, 0, bytesRead);
            }
            
            return Convert.ToHexString(hasher.GetHashAndReset());
        }

        public virtual async Task<FileUploadResponse> UploadFile(string filePath, string toFilePath, Func<FileUploadProgress, Task> updateProgressFunc = null, CancellationToken token = default, bool skipIfExists = false)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
                throw new InvalidOperationException("The client is disconnected");

            var correlationId = Guid.NewGuid().ToString();

            // get the file size

            var fileSize = new FileInfo(filePath).Length;

            // log the file upload details

            _logger.LogInformation("Uploading file {FilePath} ({fileSize} bytes) to {ToFilePath} with correlationId {CorrelationId}", filePath, fileSize, toFilePath, correlationId);

            // setup the async progress wrapper

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            
            // Register cancellation to complete the task
            using var ctr = token.Register(() => tcs.TrySetCanceled(token));
            
            async Task UpdateProgressWrapper(FileUploadProgress progress)
            {
                // log the progress update

                if (!string.IsNullOrEmpty(progress.Error))
                {
                    tcs.TrySetException(new Exception("File upload failed with error: " + progress.Error));
                }
                else
                {
                    if(updateProgressFunc != null)
                        await updateProgressFunc(progress);
                 
                    if (progress.TotalRead == fileSize)
                        tcs.TrySetResult();
                }
            }

            // upload the file

            try
            {
                // ensure file exists locally

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found", filePath);

                // Compute SHA256 checksum of file for integrity verification
                var checksum = ComputeFileChecksum(filePath);
                _logger.LogDebug("Computed checksum for {FilePath}: {Checksum}", filePath, checksum);

                // register the update func

                correlationId = await _uploadReg.Register(UpdateProgressWrapper);

                // upload file

                using var client = _httpClientFactory.CreateClient();

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Build URL without token in query string
                var postUrl = $"{_proxy.Server.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FileUpload}" +
                    $"?toFilePath={Uri.EscapeDataString(toFilePath)}&connectionId={_proxy.Server.ConnectionId}&correlationId={correlationId}" +
                    (skipIfExists ? "&skipIfExists=true" : "");

                // Create request with Authorization header
                using var request = new HttpRequestMessage(HttpMethod.Post, postUrl);
                request.Content = fileContent;
                
                // Add Authorization Bearer header instead of query string
                if (_accessTokenMgr.CurrentToken?.Token != null)
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenMgr.CurrentToken.Token);
                }

                // Add X-File-Checksum header for integrity verification
                request.Headers.Add("X-File-Checksum", checksum);

                var response = await client.SendAsync(request, token);

                // handle response

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"File upload failed with status code {response.StatusCode}", null, response.StatusCode);

                // Try to read FileUploadResponse from response body
                FileUploadResponse uploadResponse = null;
                try
                {
                    uploadResponse = await response.Content.ReadFromJsonAsync<FileUploadResponse>(token);
                }
                catch
                {
                    // Legacy server response without JSON body
                }

                // For skipped files, we don't need to wait for progress completion
                if (uploadResponse?.Status == "skipped")
                {
                    return uploadResponse;
                }

                await tcs.Task;
                return uploadResponse ?? new FileUploadResponse("uploaded", null, fileSize);
            }
            finally
            {
                await _uploadReg.Unregister(correlationId);
            }
        }

        /// <summary>
        /// Downloads a file from the remote server.
        /// </summary>
        /// <param name="remoteFilePath">The path of the file on the server (relative to storage root)</param>
        /// <param name="localFilePath">The local path where the file should be saved</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="InvalidOperationException">Thrown if the client is disconnected</exception>
        /// <exception cref="FileNotFoundException">Thrown if the remote file does not exist</exception>
        /// <exception cref="InvalidDataException">Thrown if the checksum verification fails</exception>
        public Task DownloadFile(string remoteFilePath, string localFilePath, CancellationToken token = default)
        {
            return DownloadFile(remoteFilePath, localFilePath, null, token);
        }

        /// <summary>
        /// Downloads a file from the remote server with progress reporting.
        /// </summary>
        /// <param name="remoteFilePath">The path of the file on the server (relative to storage root)</param>
        /// <param name="localFilePath">The local path where the file should be saved</param>
        /// <param name="progressCallback">Optional callback for progress updates</param>
        /// <param name="token">Cancellation token</param>
        /// <exception cref="InvalidOperationException">Thrown if the client is disconnected</exception>
        /// <exception cref="FileNotFoundException">Thrown if the remote file does not exist</exception>
        /// <exception cref="InvalidDataException">Thrown if the checksum verification fails</exception>
        public virtual async Task DownloadFile(string remoteFilePath, string localFilePath, Func<FileDownloadProgress, Task> progressCallback, CancellationToken token = default)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
                throw new InvalidOperationException("The client is disconnected");

            _logger.LogInformation("Downloading file {RemoteFilePath} to {LocalFilePath}", remoteFilePath, localFilePath);

            using var client = _httpClientFactory.CreateClient();

            // Build URL for download
            var downloadUrl = $"{_proxy.Server.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FileDownload}" +
                $"?filePath={Uri.EscapeDataString(remoteFilePath)}";

            // Create request with Authorization header
            using var request = new HttpRequestMessage(HttpMethod.Get, downloadUrl);

            // Add Authorization Bearer header
            if (_accessTokenMgr.CurrentToken?.Token != null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenMgr.CurrentToken.Token);
            }

            // Use ResponseHeadersRead to start streaming immediately
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, token);

            // Handle 404 - file not found
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                throw new FileNotFoundException($"Remote file not found: {remoteFilePath}", remoteFilePath);
            }

            // Handle other errors
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"File download failed with status code {response.StatusCode}", null, response.StatusCode);
            }

            // Get expected checksum for verification
            string expectedChecksum = null;
            if (response.Headers.TryGetValues("X-File-Checksum", out var checksumValues))
            {
                expectedChecksum = checksumValues.First();
            }

            // Get content length for progress tracking
            var totalSize = response.Content.Headers.ContentLength ?? 0;

            // Ensure directory exists
            var directory = Path.GetDirectoryName(localFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Track completion for partial file cleanup
            bool downloadComplete = false;
            
            // Stream content to file with progress reporting
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            using var contentStream = await response.Content.ReadAsStreamAsync(token);
            
            try
            {
                using var fileStream = new FileStream(localFilePath, FileMode.Create, FileAccess.Write);
            
                var buffer = new byte[DownloadConstants.ChunkSize];
                long totalRead = 0;
                var lastProgressUpdate = DateTime.UtcNow;
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead, token);
                    hasher.AppendData(buffer, 0, bytesRead);
                    totalRead += bytesRead;

                    // Report progress at throttled intervals
                    if (progressCallback != null)
                    {
                        var now = DateTime.UtcNow;
                        if ((now - lastProgressUpdate).TotalMilliseconds >= DownloadConstants.ProgressThrottleMs || totalRead == totalSize)
                        {
                            await progressCallback(new FileDownloadProgress(totalRead, totalSize));
                            lastProgressUpdate = now;
                        }
                    }
                }

                // Verify checksum if provided
                if (expectedChecksum != null)
                {
                    var actualChecksum = Convert.ToHexString(hasher.GetHashAndReset());

                    if (!string.Equals(expectedChecksum, actualChecksum, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogError("Checksum mismatch for {RemoteFilePath}. Expected: {Expected}, Actual: {Actual}", 
                            remoteFilePath, expectedChecksum, actualChecksum);
                        throw new InvalidDataException($"Checksum verification failed for file: {remoteFilePath}");
                    }

                    _logger.LogDebug("Checksum verified for {RemoteFilePath}: {Checksum}", remoteFilePath, actualChecksum);
                }

                _logger.LogInformation("Downloaded {ByteCount} bytes to {LocalFilePath}", totalRead, localFilePath);
                downloadComplete = true;
            }
            finally
            {
                // Cleanup partial file if download did not complete successfully
                if (!downloadComplete && File.Exists(localFilePath))
                {
                    try { File.Delete(localFilePath); } 
                    catch { /* ignore cleanup errors */ }
                }
            }
        }

        /// <summary>
        /// Checks if files exist on the remote server.
        /// </summary>
        /// <param name="directory">Remote directory path to check files in.</param>
        /// <param name="filenames">Array of filenames to check for existence.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary mapping filename to existence status.</returns>
        public async Task<Dictionary<string, bool>> CheckFilesExist(
            string directory, 
            IEnumerable<string> filenames, 
            CancellationToken token = default)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
                throw new InvalidOperationException("The client is disconnected");

            var allFiles = filenames.ToArray();
            var result = new Dictionary<string, bool>();

            using var client = _httpClientFactory.CreateClient();

            // Chunk files if more than BATCH_EXISTS_CHUNK_SIZE
            const int BATCH_EXISTS_CHUNK_SIZE = 100;
            
            foreach (var chunk in allFiles.Chunk(BATCH_EXISTS_CHUNK_SIZE))
            {
                var requestUrl = $"{_proxy.Server.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FilesExist}";
                var request = new FilesExistRequest(directory, chunk);

                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, requestUrl);
                httpRequest.Content = System.Net.Http.Json.JsonContent.Create(request);

                // Add Authorization Bearer header
                if (_accessTokenMgr.CurrentToken?.Token != null)
                {
                    httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenMgr.CurrentToken.Token);
                }

                var response = await client.SendAsync(httpRequest, token);

                if (!response.IsSuccessStatusCode)
                {
                    throw new HttpRequestException($"Files exist check failed with status code {response.StatusCode}", null, response.StatusCode);
                }

                var chunkResult = await response.Content.ReadFromJsonAsync<FilesExistResponse>(token);
                
                if (chunkResult?.Exists != null)
                {
                    foreach (var kvp in chunkResult.Exists)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Enumerates files on the remote server matching a pattern.
        /// </summary>
        /// <param name="path">The base directory path on the server.</param>
        /// <param name="searchPattern">Glob pattern to match files (e.g., "*.txt", "**/*.cs").</param>
        /// <param name="recursive">Whether to search recursively in subdirectories.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of file info entries matching the pattern.</returns>
        /// <exception cref="InvalidOperationException">Thrown if the client is disconnected.</exception>
        public async Task<IReadOnlyList<FileInfoEntry>> EnumerateFiles(
            string path, 
            string searchPattern, 
            bool recursive = false, 
            CancellationToken token = default)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
                throw new InvalidOperationException("The client is disconnected");

            _logger.LogDebug("Enumerating files at {Path} with pattern {Pattern}, recursive={Recursive}", 
                path, searchPattern, recursive);

            // Create the RPC request
            var searchOption = recursive ? "AllDirectories" : "TopDirectoryOnly";
            var request = new EnumerateFilesRequest(path, searchPattern, searchOption);

            // Send RPC request via SignalR through the proxy
            var response = await _proxy.SendRpcRequest<EnumerateFilesResponse>(request, token);

            // Check for errors
            if (!string.IsNullOrEmpty(response.Error))
            {
                _logger.LogError("EnumerateFiles failed: {Error}", response.Error);
                throw new InvalidOperationException($"Failed to enumerate files: {response.Error}");
            }

            var files = response.Files;
            _logger.LogDebug("Found {Count} files matching pattern", files.Length);

            return files;
        }

    }

}
