using BitPantry.CommandLine.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class FileTransferService
    {
        private ILogger<FileTransferService> _logger;
        private IServerProxy _proxy;
        private IHttpClientFactory _httpClientFactory;
        private AccessTokenManager _accessTokenMgr;
        private FileUploadProgressUpdateFunctionRegistry _reg;

        public FileTransferService(
            ILogger<FileTransferService> logger, 
            IServerProxy proxy, 
            IHttpClientFactory httpClientFactory, 
            AccessTokenManager accessTokenMgr, 
            FileUploadProgressUpdateFunctionRegistry reg)
        {
            _logger = logger;
            _proxy = proxy;
            _httpClientFactory = httpClientFactory;
            _accessTokenMgr = accessTokenMgr;
            _reg = reg;
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

        public async Task UploadFile(string filePath, string toFilePath, Func<FileUploadProgress, Task> updateProgressFunc = null, CancellationToken token = default)
        {
            if (_proxy.ConnectionState != ServerProxyConnectionState.Connected)
                throw new InvalidOperationException("The client is disconnected");

            var correlationId = Guid.NewGuid().ToString();

            // get the file size

            var fileSize = new FileInfo(filePath).Length;

            // log the file upload details

            _logger.LogInformation("Uploading file {FilePath} ({fileSize} bytes) to {ToFilePath} with correlationId {CorrelationId}", filePath, fileSize, toFilePath, correlationId);

            // setup the async progress wrapper

            var tcs = new TaskCompletionSource();
            
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

                correlationId = await _reg.Register(UpdateProgressWrapper);

                // upload file

                using var client = _httpClientFactory.CreateClient();

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Build URL without token in query string
                var postUrl = $"{_proxy.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FileUpload}" +
                    $"?toFilePath={Uri.EscapeDataString(toFilePath)}&connectionId={_proxy.ConnectionId}&correlationId={correlationId}";

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

                await tcs.Task;
            }
            finally
            {
                await _reg.Unregister(correlationId);
            }
        }

    }

}
