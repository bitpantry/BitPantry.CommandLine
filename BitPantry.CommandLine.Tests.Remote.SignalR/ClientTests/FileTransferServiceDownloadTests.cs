using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
using FluentAssertions;
using Moq;
using Moq.Protected;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for FileTransferService download functionality.
    /// </summary>
    [TestClass]
    public class FileTransferServiceDownloadTests
    {
        private Mock<IServerProxy> _proxyMock;
        private FileTransferServiceTestContext _context;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private FileTransferService _service;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = TestServerProxyFactory.CreateConnected();

            _context = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
            _httpMessageHandlerMock = _context.HttpMessageHandlerMock;
            _service = _context.Service;
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

            await _context.SetupAuthenticatedTokenAsync();
            _context.SetupHttpDownloadResponse(expectedContent);

            using var tempFile = new TempFileScope();

            // Act
            await _service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert
            tempFile.Exists.Should().BeTrue();
            tempFile.ReadAllText().Should().Be(expectedContent);
        }

        /// <summary>
        /// Implements: CV-012
        /// When server returns 404, throws FileNotFoundException.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_FileNotFound_ThrowsFileNotFoundException()
        {
            // Arrange
            await _context.SetupAuthenticatedTokenAsync();
            _context.SetupHttp404Response();

            using var tempFile = new TempFileScope();

            // Act & Assert
            await _service.Invoking(s => s.DownloadFile("nonexistent.txt", tempFile.Path, CancellationToken.None))
                .Should().ThrowAsync<FileNotFoundException>()
                .WithMessage("*nonexistent.txt*");
        }

        /// <summary>
        /// Implements: CV-013
        /// When checksum mismatch, throws InvalidDataException and deletes partial file.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_ChecksumMismatch_ThrowsInvalidDataException()
        {
            // Arrange
            await _context.SetupAuthenticatedTokenAsync();
            
            // Use the two-arg overload with intentionally wrong checksum
            _context.SetupHttpDownloadResponse("File content", "WRONGCHECKSUMVALUE123456789");

            // Use WithoutFile() to verify partial file cleanup behavior
            using var tempFile = TempFileScope.WithoutFile();

            // Act & Assert
            await _service.Invoking(s => s.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None))
                .Should().ThrowAsync<InvalidDataException>()
                .WithMessage("*Checksum*");

            // Verify partial file is deleted after checksum failure (CV-013 requirement)
            tempFile.Exists.Should().BeFalse("partial file should be deleted on checksum failure");
        }

        /// <summary>
        /// Implements: EH-017
        /// When user cancels download, throws TaskCanceledException.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_Cancelled_ThrowsTaskCancelledException()
        {
            // Arrange
            await _context.SetupAuthenticatedTokenAsync();

            var cts = new CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            _httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            using var tempFile = new TempFileScope();

            // Act & Assert
            await _service.Invoking(s => s.DownloadFile("test-file.txt", tempFile.Path, cts.Token))
                .Should().ThrowAsync<TaskCanceledException>();
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
            
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests, "content");

            using var tempFile = new TempFileScope();

            // Act
            await _service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasBearerAuth.Should().BeTrue("Authorization Bearer header should be present");
            request.AuthParameter.Should().Be(testToken.Token);
        }

        /// <summary>
        /// Implements: DF-013 (negative case)
        /// When DownloadFile called, token is not exposed in URL query string.
        /// </summary>
        [TestMethod]
        public async Task DownloadFile_DoesNotIncludeTokenInQueryString()
        {
            // Arrange
            var testToken = await _context.SetupAuthenticatedTokenAsync();
            
            var capturedRequests = new List<CapturedHttpRequest>();
            _context.SetupHttpWithRequestCapture(capturedRequests, "content");

            using var tempFile = new TempFileScope();

            // Act
            await _service.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None);

            // Assert - token should NOT be in query string
            capturedRequests.Should().HaveCount(1);
            var request = capturedRequests[0];
            request.HasTokenInQueryString(testToken.Token).Should().BeFalse("Token should not appear in URL");
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

            // Create a new service with the disconnected proxy using factory
            var service = TestFileTransferServiceFactory.Create(_proxyMock);

            using var tempFile = new TempFileScope();

            // Act & Assert
            await service.Invoking(s => s.DownloadFile("test-file.txt", tempFile.Path, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*disconnected*");
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

            await _context.SetupAuthenticatedTokenAsync();
            _context.SetupHttpDownloadResponse(expectedContent);

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

