using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using FluentAssertions;
using Moq;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests
{
    /// <summary>
    /// Unit tests for DownloadCommand.
    /// Implements test cases from spec 007-download-command: CV-001 through CV-030, UX-001 through UX-032, DF-001 through DF-019
    /// </summary>
    [TestClass]
    public class DownloadCommandTests
    {
        private Mock<IServerProxy> _proxyMock = null!;
        private TestConsole _console = null!;
        private MockFileSystem _fileSystem = null!;

        [TestInitialize]
        public void Setup()
        {
            _proxyMock = new Mock<IServerProxy>();
            _console = new TestConsole();
            _fileSystem = new MockFileSystem();

            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            _proxyMock.Setup(p => p.Server).Returns(new ServerCapabilities(
                new Uri("https://localhost:5000"),
                "test-connection-id",
                new List<CommandInfo>(),
                100 * 1024 * 1024)); // 100MB default
        }

        #region Connection Verification Tests (CV-001, UX-004, EH-001)

        /// <summary>
        /// Implements: CV-001, UX-004, EH-001
        /// When not connected, returns error without attempting download.
        /// </summary>
        [TestMethod]
        public async Task Execute_WhenDisconnected_ReturnsErrorWithoutDownload()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            var command = CreateCommand();
            command.Source = "remote.txt";
            command.Destination = @"C:\local\";

            // Act
            await command.Execute(CreateContext());

            // Assert
            _console.Output.Should().Contain("Not connected to server");
        }

        /// <summary>
        /// Implements: CV-001
        /// When connection state is Connecting, returns error.
        /// </summary>
        [TestMethod]
        public async Task Execute_WhenConnecting_ReturnsErrorWithoutDownload()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connecting);
            var command = CreateCommand();
            command.Source = "remote.txt";
            command.Destination = @"C:\local\";

            // Act
            await command.Execute(CreateContext());

            // Assert
            _console.Output.Should().Contain("Not connected to server");
        }

        #endregion

        #region ResolveLocalPath Tests (CV-007, CV-008, T022, T023)

        /// <summary>
        /// Implements: CV-007, T022
        /// ResolveLocalPath appends filename when destination ends with /
        /// </summary>
        [TestMethod]
        public void ResolveLocalPath_DestinationEndsWithSlash_AppendsFilename()
        {
            // Arrange
            var command = CreateCommand();
            command.Destination = @"C:\downloads\";

            // Act
            var resolved = command.ResolveLocalPath("remote/path/myfile.txt");

            // Assert
            resolved.Should().Be(@"C:\downloads\myfile.txt");
        }

        /// <summary>
        /// Implements: CV-007, T022
        /// ResolveLocalPath appends filename when destination ends with \ (Windows style)
        /// </summary>
        [TestMethod]
        public void ResolveLocalPath_DestinationEndsWithBackslash_AppendsFilename()
        {
            // Arrange
            var command = CreateCommand();
            command.Destination = @"C:\downloads\subdir\";

            // Act
            var resolved = command.ResolveLocalPath("data.json");

            // Assert
            resolved.Should().Be(@"C:\downloads\subdir\data.json");
        }

        /// <summary>
        /// Implements: CV-008, T023
        /// ResolveLocalPath uses destination as-is for specific filename
        /// </summary>
        [TestMethod]
        public void ResolveLocalPath_DestinationIsFilename_UsesAsIs()
        {
            // Arrange
            var command = CreateCommand();
            command.Destination = @"C:\downloads\myfile.txt";

            // Act
            var resolved = command.ResolveLocalPath("remote.txt");

            // Assert
            resolved.Should().Be(@"C:\downloads\myfile.txt");
        }

        #endregion

        #region Literal Path Detection Tests (CV-003, T021, T028)

        /// <summary>
        /// Implements: CV-003, T021
        /// Literal path (no glob characters) triggers direct lookup
        /// </summary>
        [TestMethod]
        public void IsLiteralPath_NoGlobCharacters_ReturnsTrue()
        {
            // Arrange
            var command = CreateCommand();

            // Act & Assert
            command.IsLiteralPath("config.json").Should().BeTrue();
            command.IsLiteralPath("folder/file.txt").Should().BeTrue();
            command.IsLiteralPath("/absolute/path.log").Should().BeTrue();
        }

        /// <summary>
        /// Implements: CV-002
        /// Path with glob characters detected for pattern expansion
        /// </summary>
        [TestMethod]
        public void IsLiteralPath_WithGlobCharacters_ReturnsFalse()
        {
            // Arrange
            var command = CreateCommand();

            // Act & Assert
            command.IsLiteralPath("*.txt").Should().BeFalse();
            command.IsLiteralPath("data?.json").Should().BeFalse();
            command.IsLiteralPath("logs/**/*.log").Should().BeFalse();
            // Note: [abc].txt character class syntax is not currently supported by ContainsGlobCharacters
        }

        #endregion

        #region Not Connected Error Display Tests (UX-004, CV-001, EH-001)

        /// <summary>
        /// Implements: UX-004, CV-001, EH-001
        /// When not connected, displays friendly error message
        /// </summary>
        [TestMethod]
        public async Task Execute_NotConnected_DisplaysFriendlyError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
            var command = CreateCommand();
            command.Source = "file.txt";
            command.Destination = @"C:\local\";

            // Act
            await command.Execute(CreateContext());

            // Assert
            _console.Output.Should().Contain("Not connected to server");
            _console.Output.Should().NotContain("Exception");
            _console.Output.Should().NotContain("Stack trace");
        }

        #endregion

        #region Invalid Pattern Validation Tests (EH-011, T066)

        /// <summary>
        /// Implements: EH-011, T066
        /// When: Empty source pattern provided
        /// Then: Display error with helpful message suggesting valid format
        /// </summary>
        [TestMethod]
        public async Task Execute_EmptySource_DisplaysHelpfulError()
        {
            // Arrange
            var command = CreateCommand();
            command.Source = "";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - should display helpful error message
            _console.Output.Should().Contain("Invalid", "should indicate pattern is invalid");
            _console.Output.Should().ContainAny(new[] { "pattern", "source", "path" }, "should mention what's wrong");
        }

        /// <summary>
        /// Implements: EH-011, T066
        /// When: Whitespace-only source pattern provided
        /// Then: Display error with helpful message
        /// </summary>
        [TestMethod]
        public async Task Execute_WhitespaceSource_DisplaysHelpfulError()
        {
            // Arrange
            var command = CreateCommand();
            command.Source = "   ";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - should display helpful error message
            _console.Output.Should().Contain("Invalid", "should indicate pattern is invalid");
        }

        /// <summary>
        /// Implements: EH-011
        /// Validates that GlobPatternHelper.ValidatePattern rejects empty patterns.
        /// </summary>
        [TestMethod]
        public void ValidatePattern_EmptyString_ReturnsError()
        {
            // Act
            var result = GlobPatternHelper.ValidatePattern("");

            // Assert
            result.IsValid.Should().BeFalse("empty pattern should be invalid");
            result.ErrorMessage.Should().NotBeNullOrEmpty("should provide error message");
            result.SuggestedFormat.Should().NotBeNullOrEmpty("should suggest valid format");
        }

        /// <summary>
        /// Implements: EH-011
        /// Validates that GlobPatternHelper.ValidatePattern rejects whitespace-only patterns.
        /// </summary>
        [TestMethod]
        public void ValidatePattern_WhitespaceOnly_ReturnsError()
        {
            // Act
            var result = GlobPatternHelper.ValidatePattern("   ");

            // Assert
            result.IsValid.Should().BeFalse("whitespace-only pattern should be invalid");
            result.ErrorMessage.Should().Contain("empty", "should indicate pattern is empty/whitespace");
        }

        /// <summary>
        /// Implements: EH-011
        /// Valid patterns pass validation.
        /// </summary>
        [TestMethod]
        public void ValidatePattern_ValidPatterns_ReturnsSuccess()
        {
            // Arrange
            var validPatterns = new[]
            {
                "file.txt",
                "*.txt",
                "folder/*.log",
                "**/*.json",
                "data?.csv",
                "logs/2024/*.log"
            };

            foreach (var pattern in validPatterns)
            {
                // Act
                var result = GlobPatternHelper.ValidatePattern(pattern);

                // Assert
                result.IsValid.Should().BeTrue($"pattern '{pattern}' should be valid");
            }
        }

        #endregion

        #region Collision Detection Tests (CV-005, CV-006, T049, T050)

        /// <summary>
        /// Implements: CV-005, T049
        /// DetectCollisions returns CollisionGroup for duplicate filenames (case-insensitive)
        /// </summary>
        [TestMethod]
        public void DetectCollisions_DuplicateFilenames_ReturnsCollisionGroups()
        {
            // Arrange
            var command = CreateCommand();
            var files = new[]
            {
                new FileInfoEntry("/dir1/file.txt", 100, DateTime.Now),
                new FileInfoEntry("/dir2/file.txt", 200, DateTime.Now),
                new FileInfoEntry("/dir1/other.txt", 50, DateTime.Now)
            };

            // Act
            var collisions = command.DetectCollisions(files);

            // Assert
            collisions.Should().HaveCount(1);
            collisions[0].FileName.Should().Be("file.txt");
            collisions[0].Paths.Should().HaveCount(2);
            collisions[0].Paths.Should().Contain("/dir1/file.txt");
            collisions[0].Paths.Should().Contain("/dir2/file.txt");
        }

        /// <summary>
        /// Implements: CV-005
        /// DetectCollisions is case-insensitive for cross-platform safety
        /// </summary>
        [TestMethod]
        public void DetectCollisions_CaseInsensitive_DetectsCollision()
        {
            // Arrange
            var command = CreateCommand();
            var files = new[]
            {
                new FileInfoEntry("/dir1/FILE.txt", 100, DateTime.Now),
                new FileInfoEntry("/dir2/file.txt", 200, DateTime.Now)
            };

            // Act
            var collisions = command.DetectCollisions(files);

            // Assert
            collisions.Should().HaveCount(1);
        }

        /// <summary>
        /// Implements: CV-006, T050
        /// DetectCollisions returns empty for unique filenames
        /// </summary>
        [TestMethod]
        public void DetectCollisions_UniqueFilenames_ReturnsEmpty()
        {
            // Arrange
            var command = CreateCommand();
            var files = new[]
            {
                new FileInfoEntry("/dir1/file1.txt", 100, DateTime.Now),
                new FileInfoEntry("/dir2/file2.txt", 200, DateTime.Now),
                new FileInfoEntry("/dir1/file3.txt", 50, DateTime.Now)
            };

            // Act
            var collisions = command.DetectCollisions(files);

            // Assert
            collisions.Should().BeEmpty();
        }

        /// <summary>
        /// Implements: CV-005
        /// DetectCollisions handles multiple collision groups
        /// </summary>
        [TestMethod]
        public void DetectCollisions_MultipleCollisionGroups_ReturnsAll()
        {
            // Arrange
            var command = CreateCommand();
            var files = new[]
            {
                new FileInfoEntry("/dir1/file.txt", 100, DateTime.Now),
                new FileInfoEntry("/dir2/file.txt", 200, DateTime.Now),
                new FileInfoEntry("/dir3/file.txt", 300, DateTime.Now),
                new FileInfoEntry("/dir1/data.json", 50, DateTime.Now),
                new FileInfoEntry("/dir2/data.json", 60, DateTime.Now)
            };

            // Act
            var collisions = command.DetectCollisions(files);

            // Assert
            collisions.Should().HaveCount(2);
            collisions.Should().Contain(c => c.FileName == "file.txt" && c.Paths.Count == 3);
            collisions.Should().Contain(c => c.FileName == "data.json" && c.Paths.Count == 2);
        }

        #endregion

        #region Helper Methods

        private DownloadCommand CreateCommand(FileTransferService? fileTransferService = null)
        {
            return new DownloadCommand(
                _proxyMock.Object,
                fileTransferService!,
                _console,
                _fileSystem);
        }

        private CommandExecutionContext CreateContext()
        {
            return new CommandExecutionContext();
        }

        #endregion
    }
}
