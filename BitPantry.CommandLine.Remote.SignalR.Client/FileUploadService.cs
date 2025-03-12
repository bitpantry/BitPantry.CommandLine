using BitPantry.CommandLine.Client;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Remote.SignalR.Client
{
    public class FileUploadService
    {
        private ILogger<FileUploadService> _logger;
        private IServerProxy _proxy;
        private IHttpClientFactory _httpClientFactory;
        private AccessTokenManager _accessTokenMgr;
        private FileUploadProgressUpdateFunctionRegistry _reg;

        public FileUploadService(
            ILogger<FileUploadService> logger, 
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
            async Task UpdateProgressWrapper(FileUploadProgress progress)
            {
                // log the progress update

                if (!string.IsNullOrEmpty(progress.Error))
                {
                    tcs.SetException(new Exception("File upload failed with error: " + progress.Error));
                }
                else
                {
                    if(updateProgressFunc != null)
                        await updateProgressFunc(progress);
                 
                    if (progress.TotalRead == fileSize)
                        tcs.SetResult();
                }
            }

            // upload the file

            try
            {
                // ensure file exists locally

                if (!File.Exists(filePath))
                    throw new FileNotFoundException("File not found", filePath);

                // register the update func

                correlationId = await _reg.Register(UpdateProgressWrapper);

                // upload file

                using var client = _httpClientFactory.CreateClient();

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                using var fileContent = new StreamContent(fileStream);

                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                var postUrl = $"{_proxy.ConnectionUri.AbsoluteUri.TrimEnd('/')}/{ServiceEndpointNames.FileUpload}" +
                    $"?toFilePath={toFilePath}&connectionId={_proxy.ConnectionId}&correlationId={correlationId}&access_token={_accessTokenMgr.CurrentToken?.Token}";

                var response = await client.PostAsync(postUrl, fileContent, token);

                // handle response

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"File upload failed with status code {response.StatusCode}");

                await tcs.Task;
            }
            finally
            {
                await _reg.Unregister(correlationId);
            }
        }

    }

}
