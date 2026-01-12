using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
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
        private Mock<IServerProxy> _proxyMock;
        private FileTransferServiceTestContext _context;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private AccessTokenManager _accessTokenManager;
        private List<HttpRequestMessage> _capturedRequests;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = TestServerProxyFactory.CreateConnected();
            _capturedRequests = new List<HttpRequestMessage>();

            _context = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
            _httpMessageHandlerMock = _context.HttpMessageHandlerMock;
            _accessTokenManager = _context.AccessTokenManager;

            // Setup HTTP handler mock to capture requests
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
        }

        /// <summary>
        /// Implements: Security requirement from contracts - token in Authorization header, not URL.
        /// When UploadFile called, request includes Authorization Bearer header.
        /// </summary>
        [TestMethod]
        public async Task UploadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            using var tempFile = new TempFileScope("test content");

            // Set a test token first
            var testToken = await _context.SetupAuthenticatedTokenAsync();

            var service = _context.Service;

            // Act
            // Note: This will be cancelled after 1 second because we don't have
            // a real server to respond with progress updates. We just need to capture the request.
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            try
            {
                await service.UploadFile(tempFile.Path, "test.txt", null, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected - test is designed to cancel after capturing the request
            }

            // Assert
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            
            request.Headers.Authorization.Should().NotBeNull("Authorization header should be present");
            request.Headers.Authorization.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be(testToken.Token);
        }

        /// <summary>
        /// Implements: Security requirement from contracts - token never in URL query string.
        /// When UploadFile called, token is not exposed in URL.
        /// </summary>
        [TestMethod]
        public async Task UploadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            using var tempFile = new TempFileScope("test content");

            // Set a test token first
            var testToken = await _context.SetupAuthenticatedTokenAsync();

            var service = _context.Service;

            // Act
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
            try
            {
                await service.UploadFile(tempFile.Path, "test.txt", null, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // Expected - test is designed to cancel after capturing the request
            }

            // Assert
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            
            // Query string should not contain token
            var queryString = request.RequestUri.Query;
            queryString.Should().NotContain("access_token", "Token should not be in query string");
            queryString.Should().NotContain(testToken.Token, "Token value should not appear in URL");
        }

        /// <summary>
        /// Implements: DF-013
        /// When DownloadFile called, request sent with Authorization Bearer header.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            var testToken = await _context.SetupAuthenticatedTokenAsync();

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

            var service = _context.Service;

            using var tempFile = new TempFileScope();

            // Act
            await service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            request.Headers.Authorization.Should().NotBeNull();
            request.Headers.Authorization.Scheme.Should().Be("Bearer");
            request.Headers.Authorization.Parameter.Should().Be(testToken.Token);
        }

        /// <summary>
        /// Implements: DF-013 (negative case), Security requirement from contracts/download-api.md.
        /// When DownloadFile called, token is not exposed in URL query string.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            var testToken = await _context.SetupAuthenticatedTokenAsync();

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

            var service = _context.Service;

            using var tempFile = new TempFileScope();

            // Act
            await service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert - token should NOT be in query string
            _capturedRequests.Should().HaveCount(1);
            var request = _capturedRequests[0];
            var queryString = request.RequestUri.Query;
            queryString.Should().NotContain("token");
            queryString.Should().NotContain(testToken.Token, "Token value should not appear in URL");
        }
    }
}
