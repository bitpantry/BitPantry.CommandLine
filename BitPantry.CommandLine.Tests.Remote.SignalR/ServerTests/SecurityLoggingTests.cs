using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests verifying that security events are properly logged.
    /// </summary>
    [TestClass]
    public class SecurityLoggingTests
    {
        private MockFileSystem _fileSystem;
        private FileTransferOptions _options;
        private Mock<ILogger<FileTransferEndpointService>> _loggerMock;
        private Mock<IHubContext<CommandLineHub>> _hubContextMock;
        private FileTransferEndpointService _service;
        private const string StorageRoot = @"C:\storage";

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new MockFileSystem();
            _fileSystem.Directory.CreateDirectory(StorageRoot);
            
            _options = new FileTransferOptions
            {
                StorageRootPath = StorageRoot,
                MaxFileSizeBytes = 1024, // 1KB limit for testing
                AllowedExtensions = new[] { ".txt", ".bin" }
            };

            _loggerMock = new Mock<ILogger<FileTransferEndpointService>>();
            _hubContextMock = new Mock<IHubContext<CommandLineHub>>();
            var hubClientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<ISingleClientProxy>();
            
            _hubContextMock.Setup(x => x.Clients).Returns(hubClientsMock.Object);
            hubClientsMock.Setup(x => x.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);

            _service = new FileTransferEndpointService(
                _loggerMock.Object,
                _hubContextMock.Object,
                _fileSystem,
                _options);
        }

        [TestMethod]
        public async Task PathTraversalAttempt_LogsSecurityEvent()
        {
            // Arrange
            var maliciousPath = "../outside/secret.txt";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("malicious"));

            // Act
            await _service.UploadFile(stream, maliciousPath, "conn-123", "corr-456", stream.Length, null);

            // Assert - Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Path traversal")),
                    It.IsAny<UnauthorizedAccessException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task ExtensionRejection_LogsSecurityEvent()
        {
            // Arrange - .exe is not in allowed list
            var badExtension = "malware.exe";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes("bad content"));

            // Act
            await _service.UploadFile(stream, badExtension, "conn-123", "corr-456", stream.Length, null);

            // Assert - Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("extension")),
                    It.IsAny<FileExtensionNotAllowedException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task SizeLimitExceeded_LogsSecurityEvent()
        {
            // Arrange - Create content larger than 1KB limit
            var largeContent = new byte[2048]; // 2KB > 1KB limit
            var stream = new MemoryStream(largeContent);

            // Act
            await _service.UploadFile(stream, "large.txt", "conn-123", "corr-456", stream.Length, null);

            // Assert - Verify warning was logged for size limit
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("size") || v.ToString()!.Contains("limit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [TestMethod]
        public async Task ChecksumMismatch_LogsSecurityEvent()
        {
            // Arrange
            var content = Encoding.UTF8.GetBytes("content");
            var stream = new MemoryStream(content);
            var wrongChecksum = "0000000000000000000000000000000000000000000000000000000000000000";

            // Act
            await _service.UploadFile(stream, "checksum-test.txt", "conn-123", "corr-456", stream.Length, wrongChecksum);

            // Assert - Verify warning was logged for checksum mismatch
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Checksum")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [TestMethod]
        public async Task DownloadPathTraversal_LogsSecurityEvent()
        {
            // Arrange
            var httpContextMock = new Mock<HttpContext>();
            var responseMock = new Mock<HttpResponse>();
            responseMock.Setup(r => r.Headers).Returns(new HeaderDictionary());
            httpContextMock.Setup(c => c.Response).Returns(responseMock.Object);

            // Act
            await _service.DownloadFile("../outside/secret.txt", httpContextMock.Object);

            // Assert - Verify warning was logged
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Path traversal")),
                    It.IsAny<UnauthorizedAccessException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
