using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for DownloadCommand.
    /// Uses TestEnvironment infrastructure for real client-server communication.
    /// Implements test cases: IT-001, IT-002, IT-006, IT-007, IT-008, IT-011, IT-012
    /// </summary>
    [TestClass]
    public class IntegrationTests_DownloadCommand
    {
        private const string StorageRoot = "./cli-storage";

        #region IT-001: Single File Download E2E

        /// <summary>
        /// Implements: IT-001, T031
        /// End-to-end single file download via command.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_SingleFile_DownloadsSuccessfully()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverFileName = $"download-test-{uniqueId}.txt";
            var serverFilePath = Path.Combine(StorageRoot, serverFileName);
            var localDownloadPath = Path.Combine(Path.GetTempPath(), $"downloaded-{uniqueId}.txt");
            var content = "Content for single file download test";

            try
            {
                // Setup: Create file on server
                Directory.CreateDirectory(StorageRoot);
                File.WriteAllText(serverFilePath, content);

                // Execute download command
                var result = await env.Cli.Run($"server download \"{serverFileName}\" \"{localDownloadPath}\"");

                // Verify
                result.ResultCode.Should().Be(0);
                File.Exists(localDownloadPath).Should().BeTrue("downloaded file should exist locally");
                File.ReadAllText(localDownloadPath).Should().Be(content, "content should match");
                
                var consoleOutput = string.Concat(env.Console.Lines);
                consoleOutput.Should().Contain("Downloaded", "success message should be displayed");
            }
            finally
            {
                if (File.Exists(serverFilePath)) File.Delete(serverFilePath);
                if (File.Exists(localDownloadPath)) File.Delete(localDownloadPath);
            }
        }

        /// <summary>
        /// Implements: IT-001 variant
        /// Download to directory (destination ends with /) appends filename.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_ToDirectory_AppendsFilename()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverFileName = $"file-{uniqueId}.txt";
            var serverFilePath = Path.Combine(StorageRoot, serverFileName);
            var localDir = Path.Combine(Path.GetTempPath(), $"download-dir-{uniqueId}");
            var content = "Download to directory test";

            try
            {
                // Setup
                Directory.CreateDirectory(StorageRoot);
                Directory.CreateDirectory(localDir);
                File.WriteAllText(serverFilePath, content);

                // Execute download command with trailing slash
                var result = await env.Cli.Run($"server download \"{serverFileName}\" \"{localDir}/\"");

                // Verify - filename should be appended
                var expectedLocalPath = Path.Combine(localDir, serverFileName);
                result.ResultCode.Should().Be(0);
                File.Exists(expectedLocalPath).Should().BeTrue("file should be downloaded with original filename");
                File.ReadAllText(expectedLocalPath).Should().Be(content);
            }
            finally
            {
                if (File.Exists(serverFilePath)) File.Delete(serverFilePath);
                if (Directory.Exists(localDir)) Directory.Delete(localDir, true);
            }
        }

        #endregion

        #region IT-002: Glob Pattern Download E2E

        /// <summary>
        /// Implements: IT-002, T067
        /// End-to-end glob pattern download via command.
        /// Uses upload first to ensure files exist on server.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_GlobPattern_DownloadsAllMatching()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPrefix = $"glob-test-{uniqueId}";
            var localUploadDir = Path.Combine(Path.GetTempPath(), $"upload-{uniqueId}");
            var localDownloadDir = Path.Combine(Path.GetTempPath(), $"download-{uniqueId}");

            try
            {
                // Setup: Create local files to upload
                Directory.CreateDirectory(localUploadDir);
                Directory.CreateDirectory(localDownloadDir);
                File.WriteAllText(Path.Combine(localUploadDir, "file1.txt"), "content1");
                File.WriteAllText(Path.Combine(localUploadDir, "file2.txt"), "content2");
                File.WriteAllText(Path.Combine(localUploadDir, "file3.txt"), "content3");
                File.WriteAllText(Path.Combine(localUploadDir, "other.log"), "log content");

                // Upload files to server using FileTransferService
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "file1.txt"), $"{serverPrefix}/file1.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "file2.txt"), $"{serverPrefix}/file2.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "file3.txt"), $"{serverPrefix}/file3.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "other.log"), $"{serverPrefix}/other.log", null, CancellationToken.None);

                // First verify EnumerateFiles works
                var enumFiles = await fileTransferService.EnumerateFiles(serverPrefix, "*.txt", false, CancellationToken.None);
                enumFiles.Should().HaveCount(3, $"EnumerateFiles should find 3 .txt files in {serverPrefix}. Files found: {string.Join(", ", enumFiles.Select(f => f.Path))}");

                // Execute download command with glob pattern
                var result = await env.Cli.Run($"server download \"{serverPrefix}/*.txt\" \"{localDownloadDir}/\"");

                // Debug output - show what we got
                var consoleOutput = string.Concat(env.Console.Lines);
                var localFiles = Directory.Exists(localDownloadDir) ? Directory.GetFiles(localDownloadDir) : Array.Empty<string>();

                // Verify - only .txt files downloaded
                result.ResultCode.Should().Be(0, $"download should succeed. Console output: {consoleOutput}. Local files: {string.Join(", ", localFiles)}");
                File.Exists(Path.Combine(localDownloadDir, "file1.txt")).Should().BeTrue($"file1.txt should be downloaded. Console: {consoleOutput}");
                File.Exists(Path.Combine(localDownloadDir, "file2.txt")).Should().BeTrue("file2.txt should be downloaded");
                File.Exists(Path.Combine(localDownloadDir, "file3.txt")).Should().BeTrue("file3.txt should be downloaded");
                File.Exists(Path.Combine(localDownloadDir, "other.log")).Should().BeFalse("log file should not be downloaded");

                consoleOutput.Should().Contain("3", "summary should show 3 files downloaded");
            }
            finally
            {
                if (Directory.Exists(localUploadDir)) Directory.Delete(localUploadDir, true);
                if (Directory.Exists(localDownloadDir)) Directory.Delete(localDownloadDir, true);
                // Clean up server files
                var serverDir = Path.Combine(StorageRoot, serverPrefix);
                if (Directory.Exists(serverDir)) Directory.Delete(serverDir, true);
            }
        }

        #endregion

        #region IT-006: EnumerateFiles E2E

        /// <summary>
        /// Implements: IT-006, T069
        /// EnumerateFiles returns correct file info via FileTransferService.
        /// Uses upload first to ensure files exist on server.
        /// </summary>
        [TestMethod]
        public async Task FileTransferService_EnumerateFiles_ReturnsFileInfo()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPrefix = $"enum-test-{uniqueId}";
            var localTempDir = Path.Combine(Path.GetTempPath(), $"enum-upload-{uniqueId}");

            try
            {
                // Setup: Create local files with known sizes
                Directory.CreateDirectory(localTempDir);
                File.WriteAllText(Path.Combine(localTempDir, "small.txt"), "12345"); // 5 bytes
                File.WriteAllText(Path.Combine(localTempDir, "medium.txt"), new string('x', 100)); // 100 bytes

                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                
                // Upload files to server
                await fileTransferService.UploadFile(Path.Combine(localTempDir, "small.txt"), $"{serverPrefix}/small.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localTempDir, "medium.txt"), $"{serverPrefix}/medium.txt", null, CancellationToken.None);

                // Call EnumerateFiles
                var files = await fileTransferService.EnumerateFiles(
                    serverPrefix,
                    "*.txt",
                    recursive: false,
                    CancellationToken.None);

                // Verify
                files.Should().HaveCount(2);
                files.Should().Contain(f => f.Path.Contains("small.txt") && f.Size == 5);
                files.Should().Contain(f => f.Path.Contains("medium.txt") && f.Size == 100);
            }
            finally
            {
                if (Directory.Exists(localTempDir)) Directory.Delete(localTempDir, true);
                // Clean up server files
                var serverDir = Path.Combine(StorageRoot, serverPrefix);
                if (Directory.Exists(serverDir)) Directory.Delete(serverDir, true);
            }
        }

        #endregion

        #region IT-007: Recursive Glob with Flattening E2E

        /// <summary>
        /// Implements: IT-007, T068
        /// Recursive glob downloads files from subdirectories and flattens to destination.
        /// Uses upload first to ensure files exist on server.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_RecursiveGlob_FlattensToDestination()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPrefix = $"recursive-{uniqueId}";
            var localUploadDir = Path.Combine(Path.GetTempPath(), $"upload-recursive-{uniqueId}");
            var localDownloadDir = Path.Combine(Path.GetTempPath(), $"download-recursive-{uniqueId}");

            try
            {
                // Setup: Create nested files locally
                Directory.CreateDirectory(Path.Combine(localUploadDir, "sub1"));
                Directory.CreateDirectory(Path.Combine(localUploadDir, "sub2"));
                Directory.CreateDirectory(localDownloadDir);
                File.WriteAllText(Path.Combine(localUploadDir, "root.txt"), "root content");
                File.WriteAllText(Path.Combine(localUploadDir, "sub1", "nested1.txt"), "nested1 content");
                File.WriteAllText(Path.Combine(localUploadDir, "sub2", "nested2.txt"), "nested2 content");

                // Upload files to server using FileTransferService
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "root.txt"), $"{serverPrefix}/root.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "sub1", "nested1.txt"), $"{serverPrefix}/sub1/nested1.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "sub2", "nested2.txt"), $"{serverPrefix}/sub2/nested2.txt", null, CancellationToken.None);

                // Execute download command with recursive glob
                var result = await env.Cli.Run($"server download \"{serverPrefix}/**/*.txt\" \"{localDownloadDir}/\"");

                // Debug output
                var consoleOutput = string.Concat(env.Console.Lines);

                // Verify - all files flattened to destination directory
                result.ResultCode.Should().Be(0, $"download should succeed. Console output: {consoleOutput}");
                
                // Files should be flattened (no subdirectory structure)
                File.Exists(Path.Combine(localDownloadDir, "root.txt")).Should().BeTrue("root.txt should be downloaded");
                File.Exists(Path.Combine(localDownloadDir, "nested1.txt")).Should().BeTrue("nested1.txt should be flattened");
                File.Exists(Path.Combine(localDownloadDir, "nested2.txt")).Should().BeTrue("nested2.txt should be flattened");
                
                // Subdirectories should NOT be created
                Directory.Exists(Path.Combine(localDownloadDir, "sub1")).Should().BeFalse("subdirectories should not be created");
                Directory.Exists(Path.Combine(localDownloadDir, "sub2")).Should().BeFalse("subdirectories should not be created");
            }
            finally
            {
                if (Directory.Exists(localUploadDir)) Directory.Delete(localUploadDir, true);
                if (Directory.Exists(localDownloadDir)) Directory.Delete(localDownloadDir, true);
                // Clean up server files
                var serverDir = Path.Combine(StorageRoot, serverPrefix);
                if (Directory.Exists(serverDir)) Directory.Delete(serverDir, true);
            }
        }

        #endregion

        #region IT-008: 404 Handling E2E

        /// <summary>
        /// Implements: IT-008, T032
        /// Download of nonexistent file shows error message.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_FileNotFound_ShowsError()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var localPath = Path.Combine(Path.GetTempPath(), "should-not-exist.txt");

            try
            {
                // Execute download command for nonexistent file
                var result = await env.Cli.Run($"server download \"nonexistent-file-12345.txt\" \"{localPath}\"");

                // Verify - error message displayed
                var consoleOutput = string.Concat(env.Console.Lines);
                consoleOutput.Should().Contain("not found", "error message should indicate file not found");
                File.Exists(localPath).Should().BeFalse("no file should be created for 404");
            }
            finally
            {
                if (File.Exists(localPath)) File.Delete(localPath);
            }
        }

        #endregion

        #region IT-011: Path Separator Normalization

        /// <summary>
        /// Implements: IT-011, T154
        /// Path separators are normalized correctly between client and server.
        /// Uses upload first to ensure file exists on server.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_PathSeparators_NormalizedCorrectly()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPath = $"path-sep-{uniqueId}/subdir/file.txt";
            var localUploadDir = Path.Combine(Path.GetTempPath(), $"path-sep-upload-{uniqueId}");
            var localDownloadDir = Path.Combine(Path.GetTempPath(), $"path-sep-download-{uniqueId}");

            try
            {
                // Setup: Create local file to upload
                Directory.CreateDirectory(localUploadDir);
                Directory.CreateDirectory(localDownloadDir);
                File.WriteAllText(Path.Combine(localUploadDir, "file.txt"), "content");

                // Upload file to server with forward slashes in path
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "file.txt"), serverPath, null, CancellationToken.None);

                // Execute download with forward slashes (works on both Windows and Unix)
                var result = await env.Cli.Run($"server download \"{serverPath}\" \"{localDownloadDir}/\"");

                // Verify - download succeeds regardless of platform
                var consoleOutput = string.Concat(env.Console.Lines);
                result.ResultCode.Should().Be(0, $"download should succeed. Console: {consoleOutput}");
                File.Exists(Path.Combine(localDownloadDir, "file.txt")).Should().BeTrue("file should be downloaded");
            }
            finally
            {
                if (Directory.Exists(localUploadDir)) Directory.Delete(localUploadDir, true);
                if (Directory.Exists(localDownloadDir)) Directory.Delete(localDownloadDir, true);
                // Clean up server files
                var parentDir = Path.Combine(StorageRoot, $"path-sep-{uniqueId}");
                if (Directory.Exists(parentDir)) Directory.Delete(parentDir, true);
            }
        }

        #endregion

        #region IT-012: Case Collision Detection (EH-010)

        /// <summary>
        /// Implements: IT-012, EH-010, T065, T155
        /// Collision detection catches files with same name in different directories.
        /// When: Multiple files resolve to same local name
        /// Then: Display error listing all conflicts, no downloads
        /// Uses upload first to ensure files exist on server.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_FilenameCollision_ShowsError()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPrefix = $"collision-{uniqueId}";
            var localUploadDir = Path.Combine(Path.GetTempPath(), $"collision-upload-{uniqueId}");
            var localDownloadDir = Path.Combine(Path.GetTempPath(), $"collision-download-{uniqueId}");

            try
            {
                // Setup: Create local files with same name in different directories
                Directory.CreateDirectory(Path.Combine(localUploadDir, "dir1"));
                Directory.CreateDirectory(Path.Combine(localUploadDir, "dir2"));
                Directory.CreateDirectory(localDownloadDir);
                File.WriteAllText(Path.Combine(localUploadDir, "dir1", "same.txt"), "content1");
                File.WriteAllText(Path.Combine(localUploadDir, "dir2", "same.txt"), "content2");

                // Upload files with collision names to server
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "dir1", "same.txt"), $"{serverPrefix}/dir1/same.txt", null, CancellationToken.None);
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "dir2", "same.txt"), $"{serverPrefix}/dir2/same.txt", null, CancellationToken.None);

                // Execute download with recursive glob - should detect collision
                var result = await env.Cli.Run($"server download \"{serverPrefix}/**/*.txt\" \"{localDownloadDir}/\"");

                // Verify - collision error displayed, no files downloaded
                var consoleOutput = string.Concat(env.Console.Lines);
                consoleOutput.Should().Contain("collision", "collision error should be displayed");
                
                // No files should be downloaded when collision is detected
                Directory.GetFiles(localDownloadDir).Should().BeEmpty("no files should be downloaded on collision");
            }
            finally
            {
                if (Directory.Exists(localUploadDir)) Directory.Delete(localUploadDir, true);
                if (Directory.Exists(localDownloadDir)) Directory.Delete(localDownloadDir, true);
                // Clean up server files
                var serverDir = Path.Combine(StorageRoot, serverPrefix);
                if (Directory.Exists(serverDir)) Directory.Delete(serverDir, true);
            }
        }

        #endregion

        #region Disconnected State

        /// <summary>
        /// Implements: UX-004
        /// Download when disconnected returns error without HTTP call.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_WhenDisconnected_ReturnsError()
        {
            using var env = new TestEnvironment();
            // Don't connect

            var result = await env.Cli.Run("server download \"file.txt\" \"./local/\"");

            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("Not connected");
        }

        #endregion

        #region No Matches Warning (EH-009)

        /// <summary>
        /// Implements: UX-010, EH-009, T064
        /// Glob pattern with no matches shows warning.
        /// When: EnumerateFiles returns empty
        /// Then: Display warning "No files matched pattern: [pattern]"
        /// Uses upload first to create a file that won't match the pattern.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_NoMatches_ShowsWarning()
        {
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N");
            var serverPrefix = $"no-match-{uniqueId}";
            var localUploadDir = Path.Combine(Path.GetTempPath(), $"no-match-upload-{uniqueId}");
            var localDownloadDir = Path.Combine(Path.GetTempPath(), $"no-match-download-{uniqueId}");

            try
            {
                // Setup: Create local file to upload that won't match pattern
                Directory.CreateDirectory(localUploadDir);
                Directory.CreateDirectory(localDownloadDir);
                File.WriteAllText(Path.Combine(localUploadDir, "file.log"), "log content");

                // Upload file to server
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();
                await fileTransferService.UploadFile(Path.Combine(localUploadDir, "file.log"), $"{serverPrefix}/file.log", null, CancellationToken.None);

                // Execute download with pattern that matches nothing (.xyz pattern)
                var result = await env.Cli.Run($"server download \"{serverPrefix}/*.xyz\" \"{localDownloadDir}/\"");

                // Verify - warning displayed
                var consoleOutput = string.Concat(env.Console.Lines);
                consoleOutput.Should().Contain("No files matched", "no matches warning should be displayed");
            }
            finally
            {
                if (Directory.Exists(localUploadDir)) Directory.Delete(localUploadDir, true);
                if (Directory.Exists(localDownloadDir)) Directory.Delete(localDownloadDir, true);
                // Clean up server files
                var serverDir = Path.Combine(StorageRoot, serverPrefix);
                if (Directory.Exists(serverDir)) Directory.Delete(serverDir, true);
            }
        }

        #endregion
    }
}
