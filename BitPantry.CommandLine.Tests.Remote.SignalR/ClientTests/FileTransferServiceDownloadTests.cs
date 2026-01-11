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
        private Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>> _uploadRegistryLoggerMock;
        private FileUploadProgressUpdateFunctionRegistry _uploadRegistry;
        private Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>> _downloadRegistryLoggerMock;
        private FileDownloadProgressUpdateFunctionRegistry _downloadRegistry;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClient;
        private FileTransferService _service;

        [TestInitialize]
        public void Setup()
        {
            _loggerMock = new Mock<ILogger<FileTransferService>>();
            _proxyMock = new Mock<IServerProxy>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _uploadRegistryLoggerMock = new Mock<ILogger<FileUploadProgressUpdateFunctionRegistry>>();
            _uploadRegistry = new FileUploadProgressUpdateFunctionRegistry(_uploadRegistryLoggerMock.Object);
            _downloadRegistryLoggerMock = new Mock<ILogger<FileDownloadProgressUpdateFunctionRegistry>>();
            _downloadRegistry = new FileDownloadProgressUpdateFunctionRegistry(_downloadRegistryLoggerMock.Object);

            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _proxyMock.Setup(p => p.Server).Returns(new ServerCapabilities(
                new Uri("https://localhost:5000"),
                "test-connection-id",
                new List<CommandInfo>(),
                100 * 1024 * 1024));

            _accessTokenManager = TestAccessTokenManager.Create(new HttpResponseMessage(HttpStatusCode.Unauthorized));

            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(f => f.CreateClient()).Returns(_httpClient);

            _service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _uploadRegistry,
                _downloadRegistry);
        }

        /// <summary>
        /// Implements: CV-010
        /// When remote file exists, file content written to local path.
        /// </summary>
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

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act
                await _service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert
                File.Exists(outputPath).Should().BeTrue();
                File.ReadAllText(outputPath).Should().Be(expectedContent);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: CV-012
        /// When server returns 404, throws FileNotFoundException.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act & Assert
                await _service.Invoking(s => s.DownloadFile("nonexistent.txt", outputPath, CancellationToken.None))
                    .Should().ThrowAsync<FileNotFoundException>()
                    .WithMessage("*nonexistent.txt*");
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: CV-013
        /// When checksum mismatch, throws InvalidDataException and deletes partial file.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_ChecksumMismatch_ThrowsInvalidDataException()
        {
            // Arrange
            var contentBytes = Encoding.UTF8.GetBytes("File content");
            var wrongChecksum = "WRONGCHECKSUMVALUE123456789";

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
                    response.Headers.Add("X-File-Checksum", wrongChecksum);
                    return response;
                });

            var outputPath = Path.GetTempFileName();
            // Delete the temp file so we can verify partial file cleanup behavior
            File.Delete(outputPath);

            try
            {
                // Act & Assert
                await _service.Invoking(s => s.DownloadFile("test-file.txt", outputPath, CancellationToken.None))
                    .Should().ThrowAsync<InvalidDataException>()
                    .WithMessage("*Checksum*");

                // Verify partial file is deleted after checksum failure (CV-013 requirement)
                File.Exists(outputPath).Should().BeFalse("partial file should be deleted on checksum failure");
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: EH-017
        /// When user cancels download, throws TaskCanceledException.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_Cancelled_ThrowsTaskCancelledException()
        {
            // Arrange
            var testToken = TestJwtTokenService.GenerateAccessToken();
            await _accessTokenManager.SetAccessToken(testToken, "https://localhost:5000");

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act & Assert
                await _service.Invoking(s => s.DownloadFile("test-file.txt", outputPath, cts.Token))
                    .Should().ThrowAsync<TaskCanceledException>();
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: DF-013
        /// When DownloadFile called, request sent with Authorization Bearer header.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_SendsAuthorizationBearerHeader()
        {
            // Arrange
            HttpRequestMessage capturedRequest = null;
            var contentBytes = Encoding.UTF8.GetBytes("content");
            
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
                    capturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
                    if (request.Headers.Authorization != null)
                    {
                        capturedRequest.Headers.Authorization = new AuthenticationHeaderValue(
                            request.Headers.Authorization.Scheme,
                            request.Headers.Authorization.Parameter);
                    }
                    
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(contentBytes)
                    };
                    response.Headers.Add("X-File-Checksum", checksum);
                    return response;
                });

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act
                await _service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert
                capturedRequest.Should().NotBeNull();
                capturedRequest.Headers.Authorization.Should().NotBeNull();
                capturedRequest.Headers.Authorization.Scheme.Should().Be("Bearer");
                capturedRequest.Headers.Authorization.Parameter.Should().Be(testToken.Token);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: DF-013 (negative case)
        /// When DownloadFile called, token is not exposed in URL query string.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            HttpRequestMessage capturedRequest = null;
            var contentBytes = Encoding.UTF8.GetBytes("content");
            
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
                    capturedRequest = new HttpRequestMessage(request.Method, request.RequestUri);
                    
                    var response = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new ByteArrayContent(contentBytes)
                    };
                    response.Headers.Add("X-File-Checksum", checksum);
                    return response;
                });

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act
                await _service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert - token should NOT be in query string
                capturedRequest.Should().NotBeNull();
                capturedRequest.RequestUri.Query.Should().NotContain("token");
                capturedRequest.RequestUri.Query.Should().NotContain(testToken.Token);
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: CV-009
        /// When client is disconnected, throws InvalidOperationException.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_WhenDisconnected_ThrowsInvalidOperationException()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);

            // Create a new service with the disconnected proxy
            var service = new FileTransferService(
                _loggerMock.Object,
                _proxyMock.Object,
                _httpClientFactoryMock.Object,
                _accessTokenManager,
                _uploadRegistry,
                _downloadRegistry);

            var outputPath = Path.GetTempFileName();

            try
            {
                // Act & Assert
                await service.Invoking(s => s.DownloadFile("test-file.txt", outputPath, CancellationToken.None))
                    .Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*disconnected*");
            }
            finally
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
            }
        }

        /// <summary>
        /// Implements: CV-014
        /// When local directory doesn't exist, creates parent directories before write.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_CreatesParentDirectories()
        {
            // Arrange
            var expectedContent = "File content for directory test";
            var contentBytes = Encoding.UTF8.GetBytes(expectedContent);

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

            // Create a path with non-existent parent directories
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "subdir1", "subdir2");
            var outputPath = Path.Combine(tempDir, "downloaded-file.txt");

            try
            {
                // Verify parent directory doesn't exist before download
                Directory.Exists(tempDir).Should().BeFalse("test precondition: parent directory should not exist");

                // Act
                await _service.DownloadFile("test-file.txt", outputPath, CancellationToken.None);

                // Assert
                Directory.Exists(tempDir).Should().BeTrue("parent directories should be created");
                File.Exists(outputPath).Should().BeTrue("file should be downloaded");
                File.ReadAllText(outputPath).Should().Be(expectedContent);
            }
            finally
            {
                // Clean up the created directory tree
                var rootDir = Path.Combine(Path.GetTempPath(), Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(tempDir))!));
                if (Directory.Exists(rootDir))
                    Directory.Delete(rootDir, recursive: true);
            }
        }
    }
}
