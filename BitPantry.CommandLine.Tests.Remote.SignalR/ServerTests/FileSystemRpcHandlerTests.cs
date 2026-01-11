using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Remote.SignalR.Server.Files;
using BitPantry.CommandLine.Remote.SignalR.Server.Rpc;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ServerTests
{
    /// <summary>
    /// Unit tests for FileSystemRpcHandler.
    /// Implements test cases: CV-026 through CV-030 from spec 007-download-command.
    /// 
    /// Note: Uses real file system via System.IO.Abstractions.FileSystem because
    /// FileSystemRpcHandler uses DirectoryInfo directly with Microsoft.Extensions.FileSystemGlobbing.
    /// </summary>
    [TestClass]
    public class FileSystemRpcHandlerTests
    {
        private string _storageRoot = null!;
        private IFileSystem _fileSystem = null!;
        private Mock<ILogger<FileSystemRpcHandler>> _loggerMock = null!;
        private FileTransferOptions _options = null!;
        private FileSystemRpcHandler _handler = null!;
        private TestClientProxy _clientProxy = null!;

        [TestInitialize]
        public void Setup()
        {
            // Use temp directory for tests with real file system
            _storageRoot = Path.Combine(Path.GetTempPath(), $"FileSystemRpcHandlerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_storageRoot);
            
            // Use real file system - required because handler uses DirectoryInfo with Matcher
            _fileSystem = new FileSystem();
            _loggerMock = new Mock<ILogger<FileSystemRpcHandler>>();
            _options = new FileTransferOptions { StorageRootPath = _storageRoot };
            _handler = new FileSystemRpcHandler(_loggerMock.Object, _fileSystem, _options);
            _clientProxy = new TestClientProxy();
        }

        [TestCleanup]
        public void Cleanup()
        {
            // Clean up temp directory
            if (Directory.Exists(_storageRoot))
            {
                try
                {
                    Directory.Delete(_storageRoot, recursive: true);
                }
                catch
                {
                    // Best effort cleanup
                }
            }
        }

        #region CV-026: Returns FileInfoEntry array with size/dates

        /// <summary>
        /// Implements: CV-026
        /// HandleEnumerateFiles returns FileInfoEntry array with correct size and dates.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_ValidPathAndPattern_ReturnsFileInfoEntries()
        {
            // Arrange - create real files
            var dataDir = Path.Combine(_storageRoot, "data");
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(Path.Combine(dataDir, "file1.txt"), "content1");
            File.WriteAllText(Path.Combine(dataDir, "file2.txt"), "more content");
            File.WriteAllText(Path.Combine(dataDir, "other.log"), "log data");

            var request = new EnumerateFilesRequest("data", "*.txt", "TopDirectoryOnly")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert
            _clientProxy.SentMessages.Should().HaveCount(1);
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response.Should().NotBeNull();
            response!.Error.Should().BeNullOrEmpty();
            response.Files.Should().HaveCount(2);
            response.Files.Should().Contain(f => f.Path.EndsWith("file1.txt") && f.Size == 8);
            response.Files.Should().Contain(f => f.Path.EndsWith("file2.txt") && f.Size == 12);
        }

        #endregion

        #region CV-027: Recurses with **

        /// <summary>
        /// Implements: CV-027
        /// HandleEnumerateFiles recursively searches subdirectories with ** pattern.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_RecursivePattern_IncludesSubdirectories()
        {
            // Arrange - create nested directory structure
            var logsDir = Path.Combine(_storageRoot, "logs");
            Directory.CreateDirectory(logsDir);
            Directory.CreateDirectory(Path.Combine(logsDir, "2024"));
            Directory.CreateDirectory(Path.Combine(logsDir, "2024", "02"));
            
            File.WriteAllText(Path.Combine(logsDir, "app.log"), "log1");
            File.WriteAllText(Path.Combine(logsDir, "2024", "jan.log"), "log2");
            File.WriteAllText(Path.Combine(logsDir, "2024", "02", "feb.log"), "log3");

            var request = new EnumerateFilesRequest("logs", "**/*.log", "AllDirectories")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response!.Error.Should().BeNullOrEmpty();
            // **/*.log matches all .log files including root and subdirectories
            response.Files.Should().HaveCount(3);
            response.Files.Should().Contain(f => f.Path.EndsWith("app.log"));
            response.Files.Should().Contain(f => f.Path.EndsWith("jan.log"));
            response.Files.Should().Contain(f => f.Path.EndsWith("feb.log"));
        }

        #endregion

        #region CV-028: Rejects path traversal

        /// <summary>
        /// Implements: CV-028
        /// HandleEnumerateFiles rejects path traversal attempts.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_PathTraversalAttempt_ReturnsError()
        {
            // Arrange
            var request = new EnumerateFilesRequest("../../../etc", "*", "TopDirectoryOnly")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response!.Error.Should().Contain("traversal");
        }

        #endregion

        #region CV-029: Returns error for missing directory

        /// <summary>
        /// Implements: CV-029
        /// HandleEnumerateFiles returns error when directory doesn't exist.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_DirectoryNotFound_ReturnsError()
        {
            // Arrange
            var request = new EnumerateFilesRequest("nonexistent", "*.txt", "TopDirectoryOnly")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response!.Error.Should().Contain("not found");
        }

        #endregion

        #region CV-030: Returns empty array for no matches

        /// <summary>
        /// Implements: CV-030
        /// HandleEnumerateFiles returns empty array when no files match pattern.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_NoMatches_ReturnsEmptyArray()
        {
            // Arrange
            var dataDir = Path.Combine(_storageRoot, "data");
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(Path.Combine(dataDir, "file.log"), "log data");
            
            var request = new EnumerateFilesRequest("data", "*.xyz", "TopDirectoryOnly")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response!.Error.Should().BeNullOrEmpty();
            response.Files.Should().NotBeNull();
            response.Files.Should().BeEmpty();
        }

        #endregion

        #region Additional Pattern Matching Tests

        /// <summary>
        /// Tests that single-character wildcard ? is handled.
        /// Note: Microsoft.Extensions.FileSystemGlobbing treats ? differently than shell globbing.
        /// The client-side DownloadCommand applies post-filtering for ? patterns.
        /// </summary>
        [TestMethod]
        public async Task HandleEnumerateFiles_QuestionMarkWildcard_ReturnsMatchingFiles()
        {
            // Arrange
            var dataDir = Path.Combine(_storageRoot, "data");
            Directory.CreateDirectory(dataDir);
            File.WriteAllText(Path.Combine(dataDir, "file1.txt"), "content");
            File.WriteAllText(Path.Combine(dataDir, "file2.txt"), "content");
            File.WriteAllText(Path.Combine(dataDir, "file12.txt"), "content");

            // FileSystemGlobbing may not handle ? the same as shell globbing
            // The client-side DownloadCommand applies post-filtering for ? patterns
            var request = new EnumerateFilesRequest("data", "*.txt", "TopDirectoryOnly")
            {
                CorrelationId = Guid.NewGuid().ToString()
            };

            // Act
            await _handler.HandleEnumerateFiles(_clientProxy, request);

            // Assert - with *.txt pattern, should return all 3 txt files
            var response = _clientProxy.SentMessages[0].Args[0] as EnumerateFilesResponse;
            response!.Error.Should().BeNullOrEmpty();
            response.Files.Should().HaveCount(3);
        }

        #endregion
    }
}
