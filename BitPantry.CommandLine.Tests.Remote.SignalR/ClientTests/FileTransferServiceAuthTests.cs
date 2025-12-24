using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Tests to verify that FileTransferService sends tokens in Authorization headers,
    /// not in URL query strings.
    /// </summary>
    [TestClass]
    public class FileTransferServiceAuthTests
    {
        private Mock<ILogger<FileTransferService>> _loggerMock;
        private Mock<IServerProxy> _proxyMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private AccessTokenManager _accessTokenManager;
        private Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>> _registryLoggerMock;
        private FileUploadProgressUpdateFunctionRegistry _registry;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private List<HttpRequestMessage> _capturedRequests;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<FileTransferService>>();
            _proxyMock = new Mock<IServerProxy>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _registryLoggerMock = new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>();
            _registry = new FileUploadProgressUpdateFunctionRegistry(_registryLoggerMock.Object);
            _capturedRequests = new List<HttpRequestMessage>();

            // Setup proxy mock
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _proxyMock.Setup(p => p.ConnectionUri).Returns(new Uri("https://localhost:5000"));
            _proxyMock.Setup(p => p.ConnectionId).Returns("test-connection-id");

            // Create access token manager with a valid test token
            _accessTokenManager = TestAccessTokenManager.Create(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            // Setup HTTP handler mock to capture requests
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    // Clone the request since the original may be disposed
                    _capturedRequests.Add(new HttpRequestMessage(request.Method, request.RequestUri)
                    {
                        Headers = { Authorization = request.Headers.Authorization }
                    });
                    return new HttpResponseMessage(HttpStatusCode.OK);
                });

            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(_httpClient);
        }

        [TestMethod]
        public async Task UploadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");

            // Set a test token first
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _registry);

            try
            {
                // Act
                // Note: This will be cancelled after 1 second because we don't have
                // a real server to respond with progress updates. We just need to capture the request.
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    await service.UploadFile(tempFile, "test.txt", null, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected - test is designed to cancel after capturing the request
                }
            }
            finally
            {
                File.Delete(tempFile);
            }

            // Assert
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            
            request.Headers.Authorization.Should().NotBeNull("Authorization header should be present");
            request.Headers.Authorization.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be(testToken.Token);
        }

        [TestMethod]
        public async Task UploadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, "test content");

            // Set a test token first
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _registry);

            try
            {
                // Act
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
                try
                {
                    await service.UploadFile(tempFile, "test.txt", null, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    // Expected - test is designed to cancel after capturing the request
                }
            }
            finally
            {
                File.Delete(tempFile);
            }

            // Assert
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            
            // Query string should not contain token
            var queryString = request.RequestUri.Query;
            queryString.Should().NotContain("access_token", "Token should not be in query string");
            queryString.Should().NotContain(testToken.Token, "Token value should not appear in URL");
        }

        [TestMethod]
        public async Task DownloadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            // Reset captured requests
            _capturedRequests.Clear();

            // Setup handler to return a valid response for download
            var contentBytes = System.Text.Encoding.UTF8.GetBytes("test content");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    _capturedRequests.Add(new HttpRequestMessage(request.Method, request.RequestUri)
                    {
                        Headers = { Authorization = request.Headers.Authorization }
                    });
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(contentBytes)
                    };
                    response.Headers.Add("X-File-Checksum", checksum);
                    return response;
                });

            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _registry);

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act
                await service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert
                _capturedRequests.Should().HaveCount(1);
                var request = _capturedRequests[0];
                request.Headers.Authorization.Should().NotBeNull();
                request.Headers.Authorization.Scheme.Should().Be("Bearer");
                request.Headers.Authorization.Parameter.Should().Be(testToken.Token);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        [TestMethod]
        public async Task DownloadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            // Reset captured requests
            _capturedRequests.Clear();

            // Setup handler to return a valid response for download
            var contentBytes = System.Text.Encoding.UTF8.GetBytes("test content");
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    _capturedRequests.Add(new HttpRequestMessage(request.Method, request.RequestUri));
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(contentBytes)
                    };
                    response.Headers.Add("X-File-Checksum", checksum);
                    return response;
                });

            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _registry);

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act
                await service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert - token should NOT be in query string
                _capturedRequests.Should().HaveCount(1);
                var request = _capturedRequests[0];
                var queryString = request.RequestUri.Query;
                queryString.Should().NotContain("token");
                queryString.Should().NotContain(testToken.Token, "Token value should not appear in URL");
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }
    }
}
