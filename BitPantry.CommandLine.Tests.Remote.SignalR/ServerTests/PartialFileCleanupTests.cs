using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for partial file cleanup functionality.
    /// These tests verify that failed, cancelled, or corrupted uploads don't leave orphaned files.
    /// </summary>
    [TestClass]
    public class PartialFileCleanupTests
    {
        private MockFileSystem _fileSystem;
        private FileTransferOptions _options;
        private Mock<ILogger<FileTransferEndpointService>> _loggerMock;
        private Mock<IHubContext<CommandLineHub>> _hubContextMock;
        private Mock<IHubClients> _hubClientsMock;
        private Mock<ISingleClientProxy> _clientProxyMock;
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
                MaxFileSizeBytes = 100 * 1024 * 1024 // 100MB
            };

            _loggerMock = new Mock<ILogger<FileTransferEndpointService>>();
            _hubContextMock = new Mock<IHubContext<CommandLineHub>>();
            _hubClientsMock = new Mock<IHubClients>();
            _clientProxyMock = new Mock<ISingleClientProxy>();

            _hubContextMock.Setup(x => x.Clients).Returns(_hubClientsMock.Object);
            _hubClientsMock.Setup(x => x.Client(It.IsAny<string>())).Returns(_clientProxyMock.Object);

            _service = new FileTransferEndpointService(
                _loggerMock.Object,
                _hubContextMock.Object,
                _fileSystem,
                _options);
        }

        [TestMethod]
        public async Task Upload_ExceptionDuringWrite_PartialFileDeleted()
        {
            // Arrange - Create a stream that throws after some data
            var faultyStream = new FaultyStream(throwAfterBytes: 1000);
            var targetPath = "exception-test.txt";

            // Act
            var result = await _service.UploadFile(
                faultyStream, 
                targetPath, 
                "conn-id", 
                "corr-id", 
                5000);

            // Assert - The result should be an error
            result.Should().NotBeNull();
            
            // Partial file should be cleaned up
            var fullPath = Path.Combine(StorageRoot, targetPath);
            _fileSystem.File.Exists(fullPath).Should().BeFalse(
                "partial file should be deleted after exception");
        }

        [TestMethod]
        public async Task Upload_ChecksumMismatch_PartialFileDeleted()
        {
            // Arrange
            var content = "Test content for checksum mismatch";
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var targetPath = "checksum-mismatch.txt";
            var wrongChecksum = "0000000000000000000000000000000000000000000000000000000000000000";

            // Act
            var result = await _service.UploadFile(
                stream,
                targetPath,
                "conn-id",
                "corr-id",
                content.Length,
                wrongChecksum);

            // Assert - The result should be a 400 error
            result.Should().NotBeNull();
            
            // File should be deleted after checksum mismatch
            var fullPath = Path.Combine(StorageRoot, targetPath);
            _fileSystem.File.Exists(fullPath).Should().BeFalse(
                "file should be deleted when checksum verification fails");
        }

        [TestMethod]
        public async Task Upload_SizeLimitExceeded_PartialFileDeleted()
        {
            // Arrange - Configure small limit
            _options.MaxFileSizeBytes = 100; // 100 bytes
            
            // Create content larger than limit
            var largeContent = new string('X', 200);
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
            var targetPath = "size-exceeded.txt";

            // Act
            var result = await _service.UploadFile(
                stream,
                targetPath,
                "conn-id",
                "corr-id",
                200);

            // Assert - Partial file should be cleaned up
            var fullPath = Path.Combine(StorageRoot, targetPath);
            _fileSystem.File.Exists(fullPath).Should().BeFalse(
                "partial file should be deleted when size limit is exceeded");
        }

        /// <summary>
        /// A stream that throws an IOException after reading a specified number of bytes.
        /// </summary>
        private class FaultyStream : Stream
        {
            private readonly int _throwAfterBytes;
            private int _bytesRead = 0;

            public FaultyStream(int throwAfterBytes)
            {
                _throwAfterBytes = throwAfterBytes;
            }

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position 
            { 
                get => _bytesRead; 
                set => throw new NotSupportedException(); 
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                if (_bytesRead >= _throwAfterBytes)
                {
                    throw new IOException("Simulated disk failure");
                }

                var toRead = Math.Min(count, _throwAfterBytes - _bytesRead);
                for (int i = 0; i < toRead; i++)
                {
                    buffer[offset + i] = (byte)'X';
                }
                _bytesRead += toRead;
                return toRead;
            }

            public override void Flush() { }
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }
    }
}
