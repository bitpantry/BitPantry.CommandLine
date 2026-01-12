using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Net.Http;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.Helpers
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
            Mock<IHttpClientFactory>? httpClientFactory = null,
            AccessTokenManager? accessTokenManager = null)
        {
            var loggerMock = new Mock<ILogger<FileTransferService>>();
            
            httpClientFactory ??= new Mock<IHttpClientFactory>();
            
            accessTokenManager ??= TestAccessTokenManager.Create(
                new HttpResponseMessage(HttpStatusCode.Unauthorized));
            
            var uploadRegistry = new FileUploadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>().Object);
            
            var downloadRegistry = new FileDownloadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>>().Object);

            return new FileTransferService(
                loggerMock.Object,
                proxyMock.Object,
                httpClientFactory.Object,
                accessTokenManager,
                uploadRegistry,
                downloadRegistry);
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
            AccessTokenManager? accessTokenManager = null)
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
            
            var downloadRegistry = new FileDownloadProgressUpdateFunctionRegistry(
                new Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>>().Object);

            var service = new FileTransferService(
                loggerMock.Object,
                proxyMock.Object,
                httpClientFactoryMock.Object,
                accessTokenManager,
                uploadRegistry,
                downloadRegistry);

            return new FileTransferServiceTestContext(
                service,
                loggerMock,
                httpClientFactoryMock,
                httpMessageHandlerMock,
                accessTokenManager,
                uploadRegistry,
                downloadRegistry);
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
        public FileDownloadProgressUpdateFunctionRegistry DownloadRegistry { get; }

        public FileTransferServiceTestContext(
            FileTransferService service,
            Mock<ILogger<FileTransferService>> loggerMock,
            Mock<IHttpClientFactory> httpClientFactoryMock,
            Mock<HttpMessageHandler> httpMessageHandlerMock,
            AccessTokenManager accessTokenManager,
            FileUploadProgressUpdateFunctionRegistry uploadRegistry,
            FileDownloadProgressUpdateFunctionRegistry downloadRegistry)
        {
            Service = service;
            LoggerMock = loggerMock;
            HttpClientFactoryMock = httpClientFactoryMock;
            HttpMessageHandlerMock = httpMessageHandlerMock;
            AccessTokenManager = accessTokenManager;
            UploadRegistry = uploadRegistry;
            DownloadRegistry = downloadRegistry;
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
    }
}
