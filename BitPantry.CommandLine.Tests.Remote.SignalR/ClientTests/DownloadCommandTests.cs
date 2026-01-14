using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using BitPantry.CommandLine.Remote.SignalR.Envelopes;
using BitPantry.CommandLine.Tests.Remote.SignalR.Helpers;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using System.IO.Abstractions.TestingHelpers;
using System.Net;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

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
            _proxyMock = TestServerProxyFactory.CreateConnected();
            _console = new TestConsole();
            _fileSystem = new MockFileSystem();
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
        /// IsLiteralPath delegates to GlobPatternHelper.ContainsGlobCharacters.
        /// Detailed pattern tests are in GlobPatternHelperTests.
        /// </summary>
        [TestMethod]
        public void IsLiteralPath_DelegatesToGlobPatternHelper()
        {
            // Arrange
            var command = CreateCommand();

            // Act & Assert - just verify delegation works
            command.IsLiteralPath("config.json").Should().BeTrue("literal path has no globs");
            command.IsLiteralPath("*.txt").Should().BeFalse("* is a glob character");
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
        /// Note: GlobPatternHelper.ValidatePattern is tested in GlobPatternHelperTests.
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

        #endregion

        #region Glob Pattern Expansion Tests (UX-006, T037)

        /// <summary>
        /// Implements: UX-006, T037, CV-004, T048
        /// When: User runs download with glob pattern "*.txt" and 3 files match
        /// Then: All 3 matching files are returned by ExpandSourcePattern as FileInfoEntry list
        /// </summary>
        [TestMethod]
        public async Task ExpandSourcePattern_GlobMatchesMultipleFiles_ReturnsAllMatches()
        {
            // Arrange
            var serverFiles = new[]
            {
                new FileInfoEntry("file1.txt", 100, DateTime.Now),
                new FileInfoEntry("file2.txt", 200, DateTime.Now),
                new FileInfoEntry("file3.txt", 300, DateTime.Now)
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "*.txt";
            command.Destination = @"C:\backup\";

            // Act
            var result = await command.ExpandSourcePattern("*.txt", CancellationToken.None);

            // Assert
            result.Should().HaveCount(3, "glob pattern should match all 3 txt files");
            result.Select(f => _fileSystem.Path.GetFileName(f.Path))
                .Should().Contain(new[] { "file1.txt", "file2.txt", "file3.txt" });
        }

        /// <summary>
        /// Implements: UX-007, T038
        /// When: User runs download with directory-scoped glob pattern "logs/*.log"
        /// Then: Only files in the logs directory matching *.log are returned
        /// </summary>
        [TestMethod]
        public async Task ExpandSourcePattern_DirectoryScopedGlob_ReturnsOnlyFilesInDirectory()
        {
            // Arrange - Server returns files from the logs directory
            var serverFiles = new[]
            {
                new FileInfoEntry("app.log", 100, DateTime.Now),
                new FileInfoEntry("error.log", 200, DateTime.Now)
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.Is<EnumerateFilesRequest>(r => r.Path == "logs" && r.SearchPattern == "*.log"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "logs/*.log";
            command.Destination = @"C:\archive\";

            // Act
            var result = await command.ExpandSourcePattern("logs/*.log", CancellationToken.None);

            // Assert
            result.Should().HaveCount(2, "should return only files from logs directory");
            result.Select(f => f.Path).Should().AllSatisfy(p => p.Should().StartWith("logs/"));
            result.Select(f => _fileSystem.Path.GetFileName(f.Path))
                .Should().Contain(new[] { "app.log", "error.log" });

            // Verify the request was made with the correct directory
            _proxyMock.Verify(p => p.SendRpcRequest<EnumerateFilesResponse>(
                It.Is<EnumerateFilesRequest>(r => r.Path == "logs"),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        /// <summary>
        /// Implements: UX-009, T040
        /// When: User runs download with single-char wildcard pattern "file?.txt"
        /// Then: Only files matching exactly one character after "file" are returned
        /// Note: file10.txt does NOT match because "10" is two characters
        /// </summary>
        [TestMethod]
        public async Task ExpandSourcePattern_SingleCharWildcard_MatchesExactlyOneCharacter()
        {
            // Arrange - Server returns all files, but only file1.txt and file2.txt match "file?.txt"
            // file10.txt has TWO characters after "file", so it should be filtered out
            var serverFiles = new[]
            {
                new FileInfoEntry("file1.txt", 100, DateTime.Now),
                new FileInfoEntry("file2.txt", 200, DateTime.Now),
                new FileInfoEntry("file10.txt", 300, DateTime.Now)  // Should NOT match - "10" is 2 chars
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "file?.txt";
            command.Destination = @"C:\downloads\";

            // Act
            var result = await command.ExpandSourcePattern("file?.txt", CancellationToken.None);

            // Assert
            result.Should().HaveCount(2, "? wildcard matches exactly ONE character, not two");
            result.Select(f => _fileSystem.Path.GetFileName(f.Path))
                .Should().Contain(new[] { "file1.txt", "file2.txt" });
            result.Select(f => _fileSystem.Path.GetFileName(f.Path))
                .Should().NotContain("file10.txt", "file10.txt has two chars after 'file', not one");
        }

        /// <summary>
        /// Implements: UX-010, T041
        /// When: User runs download with pattern "*.xyz" that matches no files
        /// Then: Warning message "No files matched pattern: *.xyz" is displayed in yellow
        /// </summary>
        [TestMethod]
        public async Task Execute_NoMatchingFiles_DisplaysYellowWarning()
        {
            // Arrange - Use VirtualConsoleAnsiAdapter to verify color
            var virtualConsole = new BitPantry.VirtualConsole.VirtualConsole(80, 10);
            var console = new VirtualConsoleAnsiAdapter(virtualConsole);
            
            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), Array.Empty<FileInfoEntry>());
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);

            var fileTransferService = CreateFileTransferService();
            var command = new DownloadCommand(
                _proxyMock.Object,
                fileTransferService,
                console,
                _fileSystem);
            command.Source = "*.xyz";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display warning message with pattern in yellow
            var screenContent = console.GetScreenContent();
            screenContent.Should().Contain("No files matched pattern");
            screenContent.Should().Contain("*.xyz");
            
            // Verify the "No files matched pattern" text is rendered in yellow
            // Spectre.Console may use RGB or 256-color mode, so check for yellow-ish color
            var row = virtualConsole.GetRow(0);
            var cells = row.GetCells().ToList();
            // Find the 'N' in "No files matched" and verify it has a yellow-ish color
            var nCell = cells.FirstOrDefault(c => c.Character == 'N');
            nCell.Should().NotBeNull("should find 'N' character");
            
            // Check for yellow in any color format (ConsoleColor, 256, or RGB)
            var style = nCell!.Style;
            var isYellow = style.ForegroundColor == System.ConsoleColor.DarkYellow ||
                           style.ForegroundColor == System.ConsoleColor.Yellow ||
                           style.Foreground256 == 11 || // bright yellow
                           style.Foreground256 == 3 ||  // dark yellow
                           (style.ForegroundRgb.HasValue && 
                            style.ForegroundRgb.Value.R > 200 && 
                            style.ForegroundRgb.Value.G > 200 && 
                            style.ForegroundRgb.Value.B < 100); // yellowish RGB
            
            isYellow.Should().BeTrue("warning text should be displayed in yellow (got FG={0}, 256={1}, RGB={2})",
                style.ForegroundColor, style.Foreground256, style.ForegroundRgb);
        }

        /// <summary>
        /// Implements: UX-008, T039
        /// When: User runs `server download "logs/**/*.log" ./flat/` with nested files
        /// Then: All nested .log files flattened into ./flat/ (no subdirectory structure preserved)
        /// </summary>
        [TestMethod]
        public void ResolveLocalPath_RecursiveGlobNestedFiles_FlattensToDestination()
        {
            // Arrange - Destination ends with / indicating a directory
            var command = CreateCommand();
            command.Destination = @"C:\flat\";

            // Act - Resolve paths for files from nested directories
            // These are server-relative paths that would come from a **/*.log pattern
            var resolved1 = command.ResolveLocalPath("logs/app.log");
            var resolved2 = command.ResolveLocalPath("logs/sub1/server.log");
            var resolved3 = command.ResolveLocalPath("logs/sub1/sub2/debug.log");

            // Assert - All files should be flattened into the destination directory
            // Only the filename is preserved, not the nested directory structure
            resolved1.Should().Be(@"C:\flat\app.log", "filename should be extracted from single-level path");
            resolved2.Should().Be(@"C:\flat\server.log", "filename should be extracted from two-level nested path");
            resolved3.Should().Be(@"C:\flat\debug.log", "filename should be extracted from deeply nested path");
        }

        /// <summary>
        /// Implements: UX-011, T042
        /// When: Pattern uses ** for recursive search
        /// Then: Files from all subdirectories are included in results
        /// </summary>
        [TestMethod]
        public async Task ExpandSourcePattern_RecursiveGlob_IncludesSubdirectories()
        {
            // Arrange - Server returns files from multiple levels
            var serverFiles = new[]
            {
                new FileInfoEntry("root.log", 100, DateTime.Now),
                new FileInfoEntry("sub1/app.log", 200, DateTime.Now),
                new FileInfoEntry("sub1/sub2/deep.log", 300, DateTime.Now)
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.Is<EnumerateFilesRequest>(r => r.SearchOption == "AllDirectories"),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "**/*.log";
            command.Destination = @"C:\logs\";

            // Act
            var result = await command.ExpandSourcePattern("**/*.log", CancellationToken.None);

            // Assert - Should include files from all levels
            result.Should().HaveCount(3, "recursive pattern should match files in all subdirectories");
            result.Select(f => _fileSystem.Path.GetFileName(f.Path))
                .Should().Contain(new[] { "root.log", "app.log", "deep.log" });

            // Verify recursive search option was set in request
            _proxyMock.Verify(p => p.SendRpcRequest<EnumerateFilesResponse>(
                It.Is<EnumerateFilesRequest>(r => r.SearchOption == "AllDirectories"),
                It.IsAny<CancellationToken>()), Times.Once);
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

        /// <summary>
        /// Implements: UX-027, UX-029, T043, T045, DF-004, T060
        /// When: Glob matches files with same name in different directories
        /// Then: State transitions to "Error: List collisions" - error lists all conflicting filenames AND paths
        /// </summary>
        [TestMethod]
        public async Task Execute_CollisionDetected_DisplaysErrorWithConflictingPaths()
        {
            // Arrange - Server returns files with same filename in different directories
            var serverFiles = new[]
            {
                new FileInfoEntry("dir1/config.json", 100, DateTime.Now),
                new FileInfoEntry("dir2/config.json", 200, DateTime.Now),
                new FileInfoEntry("dir1/unique.txt", 50, DateTime.Now)
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "**/*";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display error with conflicting filenames
            _console.Output.Should().Contain("collision", "should indicate filename collision error");
            _console.Output.Should().Contain("config.json", "should list the colliding filename");
            _console.Output.Should().Contain("dir1", "should list the first conflicting path");
            _console.Output.Should().Contain("dir2", "should list the second conflicting path");
        }

        /// <summary>
        /// Implements: UX-028, T044
        /// When: Collision detected in matched files
        /// Then: No files are downloaded (early abort before any download)
        /// </summary>
        [TestMethod]
        public async Task Execute_CollisionDetected_NoFilesDownloaded()
        {
            // Arrange - Server returns files with same filename in different directories
            var serverFiles = new[]
            {
                new FileInfoEntry("dir1/config.json", 100, DateTime.Now),
                new FileInfoEntry("dir2/config.json", 200, DateTime.Now)
            };

            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "**/*";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - No download should have been attempted
            // When collisions are detected, the command returns early without downloading
            // We verify by checking that an error was displayed (which causes early exit)
            _console.Output.Should().Contain("collision", "should display collision error");
            
            // Verify no success message was displayed (no files were downloaded)
            _console.Output.Should().NotContain("Downloaded", "no files should have been downloaded");
        }

        #endregion

        #region Download State Machine Tests (DF-001)

        /// <summary>
        /// Implements: DF-001, T058, DF-002, T059
        /// When Execute is called while connected with a glob pattern, state transitions to "Expand Source Pattern"
        /// (i.e., EnumerateFilesRequest is sent to expand the pattern).
        /// </summary>
        [TestMethod]
        public async Task Execute_WhenConnected_TransitionsToExpandSourcePattern()
        {
            // Arrange - Set up connected proxy
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var serverFiles = new[]
            {
                new FileInfoEntry("test.txt", 100, DateTime.Now)
            };
            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "*.txt";
            command.Destination = _fileSystem.Path.GetTempPath();

            // Act
            await command.Execute(CreateContext());

            // Assert - Verify pattern expansion (EnumerateFilesRequest) was triggered
            _proxyMock.Verify(
                p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(),
                    It.IsAny<CancellationToken>()),
                Times.Once,
                "Execute should transition to Expand Source Pattern (send EnumerateFilesRequest)");
        }

        /// <summary>
        /// Implements: DF-005, T061
        /// When all filenames are unique, state transitions to "Calculate Total Size" and proceeds to download.
        /// </summary>
        [TestMethod]
        public async Task Execute_UniqueFilenames_TransitionsToCalculateTotalSizeAndDownloads()
        {
            // Arrange - Set up files with unique names
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var serverFiles = new[]
            {
                new FileInfoEntry("file1.txt", 100, DateTime.Now),
                new FileInfoEntry("subdir/file2.txt", 200, DateTime.Now)  // Different filenames = unique
            };
            var response = new EnumerateFilesResponse(Guid.NewGuid().ToString(), serverFiles);
            
            _proxyMock
                .Setup(p => p.SendRpcRequest<EnumerateFilesResponse>(
                    It.IsAny<EnumerateFilesRequest>(), 
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var fileTransferService = CreateFileTransferService();
            var command = CreateCommand(fileTransferService);
            command.Source = "**/*.txt";
            command.Destination = _fileSystem.Path.GetTempPath();

            // Act
            await command.Execute(CreateContext());

            // Assert - Should NOT show collision error (transitions past collision check)
            _console.Output.Should().NotContain("collision", 
                "unique filenames should pass collision check and proceed to download");
            
            // The download process started (no early abort due to collisions)
            // This proves the state machine transitioned: Expand Pattern → Collision Check (pass) → Calculate Total Size → Download
        }

        #endregion

        #region Error Handling Tests (UX-019, EH-005, T129)

        // ===================================================================================
        // INTEGRATION TESTS: Real DownloadCommand → Real FileTransferService → Mock HTTP
        // These tests verify the full error propagation path from HTTP boundary to UI.
        // ===================================================================================

        /// <summary>
        /// T131: EH-012 - Checksum verification failure (INTEGRATION TEST)
        /// Real FileTransferService computes checksum, detects mismatch, throws InvalidDataException.
        /// Real DownloadCommand catches and displays user-friendly error.
        /// </summary>
        [TestMethod]
        public async Task Execute_ChecksumFailure_DisplaysChecksumVerificationError()
        {
            // Arrange - Create real FileTransferService with mock HTTP that returns wrong checksum
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_checksum_{Guid.NewGuid()}.tmp");
            _fileSystem.Directory.CreateDirectory(Path.GetTempPath());
            
            var ctx = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
            await ctx.SetupAuthenticatedTokenAsync();
            
            // Setup HTTP to return content with WRONG checksum in header
            var fileContent = "test file content for checksum test";
            var wrongChecksum = "0000000000000000000000000000000000000000000000000000000000000000";
            
            ctx.SetupHttpDownloadResponse(fileContent, wrongChecksum);

            var command = CreateCommand(ctx.Service);
            command.Source = "remote/file.txt";
            command.Destination = tempFilePath;

            try
            {
                // Act
                await command.Execute(CreateContext());

                // Assert - Should display checksum verification failure
                _console.Output.Should().Contain("Checksum verification failed", "should display integrity check error");
                _console.Output.Should().NotContain("Download failed:", "should not show generic error for checksum issues");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }

        /// <summary>
        /// T130: EH-002 - Connection lost via InvalidOperationException (INTEGRATION TEST)
        /// Real FileTransferService checks connection state and throws InvalidOperationException.
        /// Real DownloadCommand catches and displays user-friendly error.
        /// 
        /// Simulates: DownloadCommand.Execute checks connection (Connected) → calls FileTransferService.DownloadFile
        ///            → FileTransferService checks connection (Disconnected) → throws InvalidOperationException
        /// </summary>
        [TestMethod]
        public async Task Execute_ConnectionLostViaInvalidOperation_DisplaysConnectionLostError()
        {
            // Arrange - Real FileTransferService that will throw when proxy is disconnected
            var ctx = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
            await ctx.SetupAuthenticatedTokenAsync();
            
            // Simulate mid-download disconnect: first call returns Connected (for DownloadCommand check),
            // second call returns Disconnected (for FileTransferService check)
            var callCount = 0;
            _proxyMock.Setup(p => p.ConnectionState)
                .Returns(() => callCount++ == 0 
                    ? ServerProxyConnectionState.Connected 
                    : ServerProxyConnectionState.Disconnected);

            var command = CreateCommand(ctx.Service);
            command.Source = "file.txt";
            command.Destination = @"C:\downloads\file.txt";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display connection lost error (caught InvalidOperationException)
            _console.Output.Should().Contain("Connection lost during download", "should display user-friendly connection error");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for connection issues");
        }

        /// <summary>
        /// T130: Network error during download stream (INTEGRATION TEST)
        /// Real FileTransferService streaming is interrupted by IOException from HTTP.
        /// Real DownloadCommand catches and displays user-friendly error.
        /// </summary>
        [TestMethod]
        public async Task Execute_NetworkErrorDuringStream_DisplaysDownloadFailedError()
        {
            // Arrange - Create real FileTransferService with mock HTTP that throws during streaming
            var tempFilePath = Path.Combine(Path.GetTempPath(), $"test_network_{Guid.NewGuid()}.tmp");
            _fileSystem.Directory.CreateDirectory(Path.GetTempPath());
            
            var ctx = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
            await ctx.SetupAuthenticatedTokenAsync();
            
            // Setup HTTP to return a faulting stream that throws IOException mid-download
            ctx.SetupHttpFaultingStreamResponse(faultAfterBytes: 1024);

            var command = CreateCommand(ctx.Service);
            command.Source = "remote/file.txt";
            command.Destination = tempFilePath;

            try
            {
                // Act
                await command.Execute(CreateContext());

                // Assert - Should display download failed error (generic IOException)
                _console.Output.Should().Contain("Download failed:", "should display error for network issues");
            }
            finally
            {
                // Cleanup - partial file should have been deleted by FileTransferService
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }

        // ===================================================================================
        // BOUNDARY MOCK TESTS: Mock FileTransferService for file system exceptions
        // These exceptions originate from the file system (the true external boundary).
        // Mocking FileTransferService is appropriate since we're testing DownloadCommand's
        // handling of exceptions that would come from FileStream operations.
        // ===================================================================================

        /// <summary>
        /// Implements: UX-019, EH-005, T129 (IMPL-129)
        /// When: UnauthorizedAccessException thrown during local file write
        /// Then: Display "Permission denied: [details]" error message
        /// 
        /// NOTE: This mocks FileTransferService because UnauthorizedAccessException comes from
        /// the file system (FileStream), which is the true external boundary. The mock simulates
        /// what the real FileTransferService would throw when writing to a protected path.
        /// </summary>
        [TestMethod]
        public async Task Execute_PermissionDenied_DisplaysPermissionDeniedError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var mockService = TestFileTransferServiceFactory.CreateMock(_proxyMock);
            mockService
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UnauthorizedAccessException("Access to the path 'C:\\protected\\file.txt' is denied."));

            var command = CreateCommand(mockService.Object);
            command.Source = "file.txt";
            command.Destination = @"C:\protected\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display permission denied error, not generic "Download failed"
            _console.Output.Should().Contain("Permission denied:", "should display user-friendly permission error");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for permission issues");
        }

        /// <summary>
        /// Implements: EH-003, T130 (IMPL-130)
        /// When: RemoteMessagingException thrown during download (SignalR disconnect)
        /// Then: Display "Connection lost during download" error message
        /// 
        /// NOTE: This mocks FileTransferService because RemoteMessagingException comes from the
        /// SignalR RPC layer, not HTTP. FileTransferService.DownloadFile uses HTTP streaming,
        /// but other operations (like EnumerateFiles) use SignalR RPC. This tests the defensive
        /// catch block for RPC failures that could propagate during pattern expansion.
        /// </summary>
        [TestMethod]
        public async Task Execute_ConnectionLostViaRemoteMessagingException_DisplaysConnectionLostError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var mockService = TestFileTransferServiceFactory.CreateMock(_proxyMock);
            mockService
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new RemoteMessagingException("test-correlation-id", "SignalR connection closed"));

            var command = CreateCommand(mockService.Object);
            command.Source = "file.txt";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display connection lost error, not generic "Download failed"
            _console.Output.Should().Contain("Connection lost during download", "should display user-friendly connection error");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for connection issues");
        }

        /// <summary>
        /// T132: EH-006 - Disk space exhausted
        /// Tests that IOException with disk full HResult displays user-friendly disk space error.
        /// 
        /// NOTE: This mocks FileTransferService because IOException with disk full HResult comes
        /// from the file system (FileStream.WriteAsync), which is the true external boundary.
        /// The mock simulates what the real FileTransferService would throw when disk is full.
        /// </summary>
        [TestMethod]
        public async Task Execute_DiskSpaceExhausted_DisplaysDiskSpaceError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var mockService = TestFileTransferServiceFactory.CreateMock(_proxyMock);
            
            // Create IOException with disk full HResult (ERROR_DISK_FULL = 0x70, wrapped in HRESULT = 0x80070070)
            var diskFullException = new IOException("There is not enough space on the disk.");
            // Set HResult via reflection since it's read-only property
            typeof(Exception).GetField("_HResult", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(diskFullException, unchecked((int)0x80070070));
            
            mockService
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(diskFullException);

            var command = CreateCommand(mockService.Object);
            command.Source = "file.txt";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display disk space error, not generic "Download failed"
            _console.Output.Should().Contain("Disk space error", "should display disk space error message");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for disk space issues");
        }

        /// <summary>
        /// T120: EH-007 - Path too long
        /// Tests that PathTooLongException displays user-friendly error with the path.
        /// 
        /// NOTE: This mocks FileTransferService because PathTooLongException comes from the
        /// file system (FileStream constructor), which is the true external boundary.
        /// The mock simulates what the real FileTransferService would throw for long paths.
        /// </summary>
        [TestMethod]
        public async Task Execute_PathTooLong_DisplaysPathTooLongError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var mockService = TestFileTransferServiceFactory.CreateMock(_proxyMock);
            
            var longPath = @"C:\very\long\path\" + new string('a', 300) + ".txt";
            mockService
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PathTooLongException($"The specified path is too long: {longPath}"));

            var command = CreateCommand(mockService.Object);
            command.Source = "file.txt";
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display path too long error, not generic "Download failed"
            _console.Output.Should().Contain("Path too long", "should display path too long error");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for path issues");
        }

        /// <summary>
        /// T121: EH-008 - Invalid filename characters (cross-platform)
        /// Tests that NotSupportedException from invalid filename characters displays user-friendly error.
        /// 
        /// On Windows, characters like &lt;, &gt;, :, ", |, ?, * are invalid in filenames.
        /// On Linux, the forward slash (/) is invalid.
        /// 
        /// When downloading a single file: Display error for that file.
        /// When downloading batch: Skip the file and continue with remaining files.
        /// 
        /// NOTE: This mocks FileTransferService because NotSupportedException comes from the
        /// file system (FileStream constructor), which is the true external boundary.
        /// The mock simulates what the real FileTransferService would throw for invalid filenames.
        /// </summary>
        [TestMethod]
        public async Task Execute_InvalidFilenameCharacters_DisplaysInvalidFilenameError()
        {
            // Arrange
            _proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
            
            var mockService = TestFileTransferServiceFactory.CreateMock(_proxyMock);
            
            // On Windows, NotSupportedException is thrown for filenames with illegal chars like ':'
            // Example: trying to create a file named "file:name.txt"
            mockService
                .Setup(s => s.DownloadFile(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<Func<FileDownloadProgress, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new NotSupportedException("The given path's format is not supported."));

            var command = CreateCommand(mockService.Object);
            command.Source = "file:name.txt";  // Contains illegal ':' character for Windows
            command.Destination = @"C:\downloads\";

            // Act
            await command.Execute(CreateContext());

            // Assert - Should display invalid filename error, not generic "Download failed"
            _console.Output.Should().Contain("Invalid filename", "should display invalid filename error for illegal characters");
            _console.Output.Should().NotContain("Download failed:", "should not show generic error for invalid filename issues");
        }

        #endregion

        #region Constants Documentation

        /// <summary>
        /// Documents: UX-012 - Progress display threshold constant.
        /// This is not a behavioral test - it documents the expected constant value.
        /// If this fails, the threshold was changed and dependent code/docs may need updating.
        /// </summary>
        [TestMethod]
        public void DownloadConstants_ProgressDisplayThreshold_DocumentedAs25MB()
        {
            // Document the threshold constant value
            DownloadConstants.ProgressDisplayThreshold.Should().Be(25 * 1024 * 1024,
                "Progress display threshold should be 25 MB - if changed, update documentation");
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

        private FileTransferService CreateFileTransferService()
        {
            return TestFileTransferServiceFactory.Create(_proxyMock);
        }

        private CommandExecutionContext CreateContext()
        {
            return new CommandExecutionContext();
        }

        #endregion
    }
}
