using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Infrastructure.Helpers
{
    /// <summary>
    /// Factory for creating FileTransferService instances in tests.
    /// Consolidates common setup patterns to avoid duplication across test classes.
    /// </summary>
    public static class TestFileTransferServiceFactory
    {
        /// <summary>
        /// Creates a FileTransferService with standard test mocks.
        /// Use this for simple tests that don't need HTTP control.
        /// </summary>
        /// <param name="proxyMock">The server proxy mock (required for RPC calls).</param>
        /// <param name="httpClientFactory">Optional custom HTTP client factory. If null, creates a default mock.</param>
        /// <param name="accessTokenManager">Optional custom access token manager. If null, creates via TestAccessTokenManager.</param>
        /// <returns>A configured FileTransferService for testing.</returns>
        public static FileTransferService Create(
            Mock<IServerProxy> proxyMock,
            Mock<IHttpClientFactory> httpClientFactory = null,
            AccessTokenManager accessTokenManager = null)
        {
            var loggerMock = new Mock<ILogger<FileTransferService>>();
            
            httpClientFactory ??= new Mock<IHttpClientFactory>();
            
            accessTokenManager ??= TestAccessTokenManager.Create(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            
            var uploadRegistry = new FileUploadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>().Object);

            return new FileTransferService(
                loggerMock.Object,
                proxyMock.Object,
                httpClientFactory.Object,
                accessTokenManager,
                uploadRegistry);
        }

        /// <summary>
        /// Creates a FileTransferService with pre-configured HTTP infrastructure.
        /// Use this when you need control over HTTP responses via the message handler mock.
        /// </summary>
        /// <param name="proxyMock">The server proxy mock.</param>
        /// <param name="httpMessageHandlerMock">The mock HTTP message handler for controlling HTTP responses.</param>
        /// <param name="httpClientFactoryMock">The mock HTTP client factory.</param>
        /// <param name="accessTokenManager">Optional custom access token manager.</param>
        /// <returns>A configured FileTransferService with HTTP client support.</returns>
        public static FileTransferService CreateWithHttpClient(
            Mock<IServerProxy> proxyMock,
            Mock<HttpMessageHandler> httpMessageHandlerMock,
            Mock<IHttpClientFactory> httpClientFactoryMock,
            AccessTokenManager accessTokenManager = null)
        {
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(httpClient);

            return Create(proxyMock, httpClientFactoryMock, accessTokenManager);
        }

        /// <summary>
        /// Creates standard mocks needed for FileTransferService tests that require HTTP control.
        /// Returns a tuple with all the mocks configured and ready to use.
        /// </summary>
        /// <param name="proxyMock">The server proxy mock.</param>
        /// <returns>Tuple containing (Service, HttpClientFactoryMock, HttpMessageHandlerMock, AccessTokenManager, UploadRegistry, DownloadRegistry).</returns>
        public static FileTransferServiceTestContext CreateWithContext(Mock<IServerProxy> proxyMock)
        {
            var loggerMock = new Mock<ILogger<FileTransferService>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(httpClient);

            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            
            var uploadRegistry = new FileUploadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>().Object);

            var service = new FileTransferService(
                loggerMock.Object,
                proxyMock.Object,
                httpClientFactoryMock.Object,
                accessTokenManager,
                uploadRegistry);

            return new FileTransferServiceTestContext(
                service,
                loggerMock,
                httpClientFactoryMock,
                httpMessageHandlerMock,
                accessTokenManager,
                uploadRegistry);
        }

        /// <summary>
        /// Creates a Mock&lt;FileTransferService&gt; with all dependencies pre-wired.
        /// Use this for boundary mock tests where you need to mock FileTransferService itself
        /// (e.g., simulating file system exceptions that originate outside FileTransferService).
        /// </summary>
        /// <param name="proxyMock">The server proxy mock.</param>
        /// <returns>A Mock&lt;FileTransferService&gt; with CallBase=false, ready for .Setup() calls.</returns>
        public static Mock<FileTransferService> CreateMock(Mock<IServerProxy> proxyMock)
        {
            var loggerMock = new Mock<ILogger<FileTransferService>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var accessTokenManager = TestAccessTokenManager.Create(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            var uploadRegistry = new FileUploadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>().Object);

            return new Mock<FileTransferService>(
                loggerMock.Object,
                proxyMock.Object,
                httpClientFactoryMock.Object,
                accessTokenManager,
                uploadRegistry)
            { CallBase = false };
        }
    }

    /// <summary>
    /// Context object containing all mocks and services for FileTransferService testing.
    /// Useful for tests that need to configure HTTP responses or verify interactions.
    /// </summary>
    public class FileTransferServiceTestContext
    {
        public FileTransferService Service { get; }
        public Mock<ILogger<FileTransferService>> LoggerMock { get; }
        public Mock<IHttpClientFactory> HttpClientFactoryMock { get; }
        public Mock<HttpMessageHandler> HttpMessageHandlerMock { get; }
        public AccessTokenManager AccessTokenManager { get; }
        public FileUploadProgressUpdateFunctionRegistry UploadRegistry { get; }

        public FileTransferServiceTestContext(
            FileTransferService service,
            Mock<ILogger<FileTransferService>> loggerMock,
            Mock<IHttpClientFactory> httpClientFactoryMock,
            Mock<HttpMessageHandler> httpMessageHandlerMock,
            AccessTokenManager accessTokenManager,
            FileUploadProgressUpdateFunctionRegistry uploadRegistry)
        {
            Service = service;
            LoggerMock = loggerMock;
            HttpClientFactoryMock = httpClientFactoryMock;
            HttpMessageHandlerMock = httpMessageHandlerMock;
            AccessTokenManager = accessTokenManager;
            UploadRegistry = uploadRegistry;
        }

        /// <summary>
        /// Sets up an authenticated token for the test context.
        /// Consolidates the common 2-line token setup pattern.
        /// </summary>
        /// <param name="serverUrl">Server URL for the token. Defaults to https://localhost:5000</param>
        /// <returns>The generated access token for verification in tests.</returns>
        public async Task<AccessToken> SetupAuthenticatedTokenAsync(string serverUrl = "https://localhost:5000")
        {
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await AccessTokenManager.SetAccessToken(testToken, serverUrl);
            return testToken;
        }

        /// <summary>
        /// Sets up HTTP mock to return a download response with specified content and checksum.
        /// Used for integration tests that verify real FileTransferService behavior.
        /// </summary>
        /// <param name="content">The file content to return.</param>
        /// <param name="checksum">The checksum header value to return.</param>
        public void SetupHttpDownloadResponse(string content, string checksum)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content)
            };
            response.Content.Headers.ContentLength = content.Length;
            response.Headers.TryAddWithoutValidation("X-File-Checksum", checksum);

            HttpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up HTTP mock to return a response with a stream that throws IOException after reading some bytes.
        /// Used for integration tests that verify partial file cleanup and network error handling.
        /// </summary>
        /// <param name="faultAfterBytes">Number of bytes to successfully return before throwing IOException.</param>
        public void SetupHttpFaultingStreamResponse(int faultAfterBytes)
        {
            var faultingStream = new FaultingStream(faultAfterBytes);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(faultingStream)
            };
            response.Content.Headers.ContentLength = faultAfterBytes * 2; // Claim more than we'll deliver
            response.Headers.TryAddWithoutValidation("X-File-Checksum", "ABC123");

            HttpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up HTTP mock to return a download response with auto-computed SHA256 checksum.
        /// Convenience overload that computes the correct checksum from content.
        /// </summary>
        /// <param name="content">The file content to return.</param>
        public void SetupHttpDownloadResponse(string content)
        {
            var contentBytes = Encoding.UTF8.GetBytes(content);
            using var sha256 = SHA256.Create();
            var checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));
            SetupHttpDownloadResponse(content, checksum);
        }

        /// <summary>
        /// Sets up HTTP mock to return a download response with content as bytes and auto-computed checksum.
        /// </summary>
        /// <param name="contentBytes">The file content bytes to return.</param>
        public void SetupHttpDownloadResponse(byte[] contentBytes)
        {
            using var sha256 = SHA256.Create();
            var checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(contentBytes)
            };
            response.Content.Headers.ContentLength = contentBytes.Length;
            response.Headers.TryAddWithoutValidation("X-File-Checksum", checksum);

            HttpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        /// <summary>
        /// Sets up HTTP mock to return 404 Not Found response.
        /// </summary>
        public void SetupHttp404Response()
        {
            HttpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        /// <summary>
        /// Sets up HTTP mock to capture requests and return successful responses.
        /// Used for verifying Authorization headers and other request properties.
        /// Automatically handles both download (returns content) and upload (returns "skipped" JSON to avoid progress wait).
        /// </summary>
        /// <param name="capturedRequests">List to store captured request info.</param>
        /// <param name="content">Optional content to return for downloads. If null, returns upload-compatible JSON response.</param>
        public void SetupHttpWithRequestCapture(List<CapturedHttpRequest> capturedRequests, string content = null)
        {
            byte[] contentBytes = content != null ? Encoding.UTF8.GetBytes(content) : null;
            string checksum = null;
            
            if (contentBytes != null)
            {
                using var sha256 = SHA256.Create();
                checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));
            }

            HttpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    capturedRequests.Add(new CapturedHttpRequest(
                        request.Method,
                        request.RequestUri!,
                        request.Headers.Authorization?.Scheme,
                        request.Headers.Authorization?.Parameter));

                    var response = new HttpResponseMessage(HttpStatusCode.OK);
                    if (contentBytes != null)
                    {
                        // Download response: return content with checksum header
                        response.Content = new ByteArrayContent(contentBytes);
                        response.Content.Headers.ContentLength = contentBytes.Length;
                        response.Headers.TryAddWithoutValidation("X-File-Checksum", checksum);
                    }
                    else
                    {
                        // Upload response: return "skipped" status to avoid waiting for progress callback
                        // This is safe for auth tests because we only need to verify the request was made correctly
                        var uploadJson = """{"status":"skipped","serverPath":"test.txt","bytesWritten":0}""";
                        response.Content = new StringContent(uploadJson, Encoding.UTF8, "application/json");
                    }
                    return response;
                });
        }
    }

    /// <summary>
    /// Captured HTTP request information for test verification.
    /// </summary>
    public record CapturedHttpRequest(
        HttpMethod Method,
        Uri RequestUri,
        string AuthScheme,
        string AuthParameter)
    {
        /// <summary>
        /// Returns true if the request has a Bearer authorization header.
        /// </summary>
        public bool HasBearerAuth => AuthScheme == "Bearer" && !string.IsNullOrEmpty(AuthParameter);

        /// <summary>
        /// Returns true if the token appears in the query string (security violation).
        /// </summary>
        public bool HasTokenInQueryString(string token) => 
            RequestUri.Query.Contains(token) || RequestUri.Query.Contains("token");
    }

    /// <summary>
    /// Stream that throws IOException after reading a specified number of bytes.
    /// Simulates network errors during download streaming.
    /// </summary>
    public class FaultingStream : Stream
    {
        private readonly int _faultAfterBytes;
        private int _bytesRead;

        public FaultingStream(int faultAfterBytes)
        {
            _faultAfterBytes = faultAfterBytes;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_bytesRead >= _faultAfterBytes)
            {
                throw new IOException("Simulated network error during download");
            }

            var bytesToReturn = Math.Min(count, _faultAfterBytes - _bytesRead);
            for (int i = 0; i < bytesToReturn; i++)
            {
                buffer[offset + i] = (byte)'X';
            }
            _bytesRead += bytesToReturn;
            return bytesToReturn;
        }

        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}

