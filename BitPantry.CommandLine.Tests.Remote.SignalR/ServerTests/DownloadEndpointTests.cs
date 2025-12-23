using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for the file download endpoint functionality.
    /// </summary>
    [TestClass]
    public class DownloadEndpointTests
    {
        private MockFileSystem _fileSystem;
        private FileTransferOptions _options;
        private Mock<ILogger<FileTransferEndpointService>> _loggerMock;
        private Mock<IHubContext<CommandLineHub>> _hubContextMock;
        private FileTransferEndpointService _service;
        private Mock<HttpContext> _httpContextMock;
        private Mock<HttpResponse> _httpResponseMock;
        private HeaderDictionary _responseHeaders;
        private const string StorageRoot = @"C:\storage";

        [TestInitialize]
        public void Setup()
        {
            _fileSystem = new MockFileSystem();
            _fileSystem.Directory.CreateDirectory(StorageRoot);
            
            _options = new FileTransferOptions
            {
                StorageRootPath = StorageRoot,
                MaxFileSizeBytes = 100 * 1024 * 1024 // 100MB
            };

            _loggerMock = new Mock<ILogger<FileTransferEndpointService>>();
            _hubContextMock = new Mock<IHubContext<CommandLineHub>>();
            var hubClientsMock = new Mock<IHubClients>();
            var clientProxyMock = new Mock<ISingleClientProxy>();
            
            _hubContextMock.Setup(x => x.Clients).Returns(hubClientsMock.Object);
            hubClientsMock.Setup(x => x.Client(It.IsAny<string>())).Returns(clientProxyMock.Object);

            // Setup HttpContext mock
            _httpContextMock = new Mock<HttpContext>();
            _httpResponseMock = new Mock<HttpResponse>();
            _responseHeaders = new HeaderDictionary();
            _httpResponseMock.Setup(r => r.Headers).Returns(_responseHeaders);
            _httpContextMock.Setup(c => c.Response).Returns(_httpResponseMock.Object);

            _service = new FileTransferEndpointService(
                _loggerMock.Object,
                _hubContextMock.Object,
                _fileSystem,
                _options);
        }

        [TestMethod]
        public async Task Download_FileExists_ReturnsFileStream()
        {
            // Arrange
            var content = "Test file content for download";
            var filePath = Path.Combine(StorageRoot, "download-test.txt");
            _fileSystem.File.WriteAllText(filePath, content);

            // Act
            var result = await _service.DownloadFile("download-test.txt", _httpContextMock.Object);

            // Assert
            result.Should().NotBeNull();
            // Result should be a file result with the correct content
        }

        [TestMethod]
        public async Task Download_FileNotExists_Returns404()
        {
            // Act
            var result = await _service.DownloadFile("nonexistent.txt", _httpContextMock.Object);

            // Assert
            result.Should().NotBeNull();
            // The result should be a 404 Not Found
        }

        [TestMethod]
        public async Task Download_PathTraversal_Returns403()
        {
            // Arrange - Create file outside storage root
            _fileSystem.Directory.CreateDirectory(@"C:\secret");
            _fileSystem.File.WriteAllText(@"C:\secret\password.txt", "secret data");

            // Act
            var result = await _service.DownloadFile("../secret/password.txt", _httpContextMock.Object);

            // Assert
            result.Should().NotBeNull();
            // The result should be a 403 Forbidden
        }

        [TestMethod]
        public async Task Download_IncludesChecksumHeader()
        {
            // Arrange
            var content = "Content for checksum test";
            var filePath = Path.Combine(StorageRoot, "checksum-download.txt");
            _fileSystem.File.WriteAllText(filePath, content);

            // Act
            var result = await _service.DownloadFile("checksum-download.txt", _httpContextMock.Object);

            // Assert - Result should include X-File-Checksum header
            result.Should().NotBeNull();
            _responseHeaders.Should().ContainKey("X-File-Checksum");
            _responseHeaders["X-File-Checksum"].ToString().Should().HaveLength(64, 
                "SHA256 hash should be 64 hex characters");
        }
    }
}
