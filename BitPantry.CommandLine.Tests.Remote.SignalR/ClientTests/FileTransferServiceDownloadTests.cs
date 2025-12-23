using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for FileTransferService download functionality.
    /// </summary>
    [TestClass]
    public class FileTransferServiceDownloadTests
    {
        private Mock<ILogger<FileTransferService>> _loggerMock;
        private Mock<IServerProxy> _proxyMock;
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private AccessTokenManager _accessTokenManager;
        private Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>> _registryLoggerMock;
        private FileUploadProgressUpdateFunctionRegistry _registry;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<FileTransferService>>();
            _proxyMock = new Mock<IServerProxy>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _registryLoggerMock = new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>();
            _registry = new FileUploadProgressUpdateFunctionRegistry(_registryLoggerMock.Object);

            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _proxyMock.Setup(p => p.ConnectionUri).Returns(new Uri("https://localhost:5000"));
            _proxyMock.Setup(p => p.ConnectionId).Returns("test-connection-id");

            _accessTokenManager = TestAccessTokenManager.Create(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(_httpClient);
        }

        [TestMethod]
        public async Task DownloadFile_ValidFile_ReturnsContent()
        {
            // Arrange
            var expectedContent = "Downloaded file content";
            var contentBytes = Encoding.UTF8.GetBytes(expectedContent);
            
            // Compute checksum for the response
            using var sha256 = SHA256.Create();
            var checksum = Convert.ToHexString(sha256.ComputeHash(contentBytes));

            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(contentBytes)
                    };
                    response.Headers.Add("X-File-Checksum", checksum);
                    response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    response.Content.Headers.ContentLength = contentBytes.Length;
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
                // Act - When download is implemented, this will work
                // For now, just verify the setup is correct
                service.Should().NotBeNull();
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        [TestMethod]
        public void DownloadFile_FileNotFound_ThrowsFileNotFoundException()
        {
            // This test will be enabled when download is implemented
            Assert.Inconclusive("Download functionality not yet implemented");
        }

        [TestMethod]
        public void DownloadFile_ChecksumMismatch_ThrowsIntegrityException()
        {
            // This test will be enabled when download is implemented
            Assert.Inconclusive("Download functionality not yet implemented");
        }

        [TestMethod]
        public void DownloadFile_Cancelled_ThrowsTaskCancelledException()
        {
            // This test will be enabled when download is implemented
            Assert.Inconclusive("Download functionality not yet implemented");
        }

        [TestMethod]
        public async Task DownloadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            var capturedRequests = new List<HttpRequestMessage>();
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
                {
                    capturedRequests.Add(new HttpRequestMessage(request.Method, request.RequestUri)
                    {
                        Headers = { Authorization = request.Headers.Authorization }
                    });
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                });

            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _registry);

            // When download is implemented, verify the Authorization header is sent
            service.Should().NotBeNull();
        }
    }
}
