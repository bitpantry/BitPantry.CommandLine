using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Commands.Server;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.CommandLine.Tests.Infrastructure.Helpers;
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
        #region IT-001: Single File Download E2E

        /// <summary>
        /// Implements: IT-001, T031
        /// End-to-end single file download via command.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_SingleFile_DownloadsSuccessfully()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var content = "Content for single file download test";
            var serverPath = env.RemoteFileSystem.CreateServerFile("download-test.txt", content);

            // Execute download command
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify
            result.ResultCode.Should().Be(0);
            var downloadedFile = env.RemoteFileSystem.LocalPath("download-test.txt");
            File.Exists(downloadedFile).Should().BeTrue("downloaded file should exist locally");
            File.ReadAllText(downloadedFile).Should().Be(content, "content should match");
            
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("Downloaded", "success message should be displayed");
        }

        /// <summary>
        /// Implements: IT-001 variant
        /// Download to directory (destination ends with /) appends filename.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_ToDirectory_AppendsFilename()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var content = "Download to directory test";
            var serverPath = env.RemoteFileSystem.CreateServerFile("file-to-dir.txt", content);

            // Execute download command with trailing slash
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - filename should be appended
            var expectedLocalPath = env.RemoteFileSystem.LocalPath("file-to-dir.txt");
            result.ResultCode.Should().Be(0);
            File.Exists(expectedLocalPath).Should().BeTrue("file should be downloaded with original filename");
            File.ReadAllText(expectedLocalPath).Should().Be(content);
        }

        #endregion

        #region IT-002: Glob Pattern Download E2E

        /// <summary>
        /// Implements: IT-002, T067
        /// End-to-end glob pattern download via command.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_GlobPattern_DownloadsAllMatching()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create files on server (3 txt + 1 log)
            env.RemoteFileSystem.CreateServerFile("file1.txt", "content1");
            env.RemoteFileSystem.CreateServerFile("file2.txt", "content2");
            env.RemoteFileSystem.CreateServerFile("file3.txt", "content3");
            env.RemoteFileSystem.CreateServerFile("other.log", "log content");

            // Execute download command with glob pattern
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.txt";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - only .txt files downloaded
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed. Console output: {consoleOutput}");
            File.Exists(env.RemoteFileSystem.LocalPath("file1.txt")).Should().BeTrue($"file1.txt should be downloaded. Console: {consoleOutput}");
            File.Exists(env.RemoteFileSystem.LocalPath("file2.txt")).Should().BeTrue("file2.txt should be downloaded");
            File.Exists(env.RemoteFileSystem.LocalPath("file3.txt")).Should().BeTrue("file3.txt should be downloaded");
            File.Exists(env.RemoteFileSystem.LocalPath("other.log")).Should().BeFalse("log file should not be downloaded");

            consoleOutput.Should().Contain("3", "summary should show 3 files downloaded");
        }

        #endregion

        #region IT-006: EnumerateFiles E2E

        /// <summary>
        /// Implements: IT-006, T069
        /// EnumerateFiles returns correct file info via FileTransferService.
        /// </summary>
        [TestMethod]
        public async Task FileTransferService_EnumerateFiles_ReturnsFileInfo()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create files with known sizes on server
            env.RemoteFileSystem.CreateServerFile("small.txt", "12345"); // 5 bytes
            env.RemoteFileSystem.CreateServerFile("medium.txt", new string('x', 100)); // 100 bytes

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Call EnumerateFiles
            var files = await fileTransferService.EnumerateFiles(
                env.RemoteFileSystem.ServerTestFolderPrefix,
                "*.txt",
                recursive: false,
                CancellationToken.None);

            // Verify
            files.Should().HaveCount(2);
            files.Should().Contain(f => f.Path.Contains("small.txt") && f.Size == 5);
            files.Should().Contain(f => f.Path.Contains("medium.txt") && f.Size == 100);
        }

        #endregion

        #region IT-007: Recursive Glob with Flattening E2E

        /// <summary>
        /// Implements: IT-007, T068
        /// Recursive glob downloads files from subdirectories and flattens to destination.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_RecursiveGlob_FlattensToDestination()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create nested files on server
            env.RemoteFileSystem.CreateServerFile("root.txt", "root content");
            env.RemoteFileSystem.CreateServerFile("sub1/nested1.txt", "nested1 content");
            env.RemoteFileSystem.CreateServerFile("sub2/nested2.txt", "nested2 content");

            // Execute download command with recursive glob
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/**/*.txt";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - all files flattened to destination directory
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed. Console output: {consoleOutput}");
            
            // Files should be flattened (no subdirectory structure)
            File.Exists(env.RemoteFileSystem.LocalPath("root.txt")).Should().BeTrue("root.txt should be downloaded");
            File.Exists(env.RemoteFileSystem.LocalPath("nested1.txt")).Should().BeTrue("nested1.txt should be flattened");
            File.Exists(env.RemoteFileSystem.LocalPath("nested2.txt")).Should().BeTrue("nested2.txt should be flattened");
            
            // Subdirectories should NOT be created
            Directory.Exists(Path.Combine(env.RemoteFileSystem.LocalTestDir, "sub1")).Should().BeFalse("subdirectories should not be created");
            Directory.Exists(Path.Combine(env.RemoteFileSystem.LocalTestDir, "sub2")).Should().BeFalse("subdirectories should not be created");
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
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Execute download command for nonexistent file
            var result = await env.Cli.Run($"server download \"nonexistent-file-12345.txt\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - error message displayed
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("not found", "error message should indicate file not found");
            File.Exists(env.RemoteFileSystem.LocalPath("nonexistent-file-12345.txt")).Should().BeFalse("no file should be created for 404");
        }

        #endregion

        #region IT-011: Path Separator Normalization

        /// <summary>
        /// Implements: IT-011, T154
        /// Path separators are normalized correctly between client and server.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_PathSeparators_NormalizedCorrectly()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create file in nested path on server (uses forward slashes)
            var serverPath = env.RemoteFileSystem.CreateServerFile("subdir/file.txt", "content");

            // Execute download with forward slashes (works on both Windows and Unix)
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - download succeeds regardless of platform
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed. Console: {consoleOutput}");
            File.Exists(env.RemoteFileSystem.LocalPath("file.txt")).Should().BeTrue("file should be downloaded");
        }

        #endregion

        #region IT-012: Case Collision Detection (EH-010)

        /// <summary>
        /// Implements: IT-012, EH-010, T065, T155
        /// Collision detection catches files with same name in different directories.
        /// When: Multiple files resolve to same local name
        /// Then: Display error listing all conflicts, no downloads
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_FilenameCollision_ShowsError()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create files with same name in different directories on server
            env.RemoteFileSystem.CreateServerFile("dir1/same.txt", "content1");
            env.RemoteFileSystem.CreateServerFile("dir2/same.txt", "content2");

            // Execute download with recursive glob - should detect collision
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/**/*.txt";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - collision error displayed, no files downloaded
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("collision", "collision error should be displayed");
            
            // No files should be downloaded when collision is detected
            Directory.GetFiles(env.RemoteFileSystem.LocalTestDir).Should().BeEmpty("no files should be downloaded on collision");
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
            using var env = TestEnvironment.WithServer();
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
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_NoMatches_ShowsWarning()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create file on server that won't match pattern
            env.RemoteFileSystem.CreateServerFile("file.log", "log content");

            // Execute download with pattern that matches nothing (.xyz pattern)
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.xyz";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - warning displayed
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("No files matched", "no matches warning should be displayed");
        }

        #endregion

        #region UX-031: Multi-File Success Message Format

        /// <summary>
        /// Implements: UX-031, T046
        /// When: Multiple files are downloaded successfully
        /// Then: Message "Downloaded [N] file(s) to [destination]" is displayed
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_MultipleFilesSuccess_DisplaysCorrectSummaryMessage()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Setup: Create files on server
            env.RemoteFileSystem.CreateServerFile("report1.txt", "Report 1 content");
            env.RemoteFileSystem.CreateServerFile("report2.txt", "Report 2 content");
            env.RemoteFileSystem.CreateServerFile("report3.txt", "Report 3 content");

            // Execute download command with glob pattern
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.txt";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify - success message format matches UX-031 spec
            var consoleOutput = string.Concat(env.Console.Lines);
            
            result.ResultCode.Should().Be(0, $"download should succeed. Console: {consoleOutput}");
            
            // UX-031: Message "Downloaded [N] file(s) to [destination]" displayed
            consoleOutput.Should().Contain("Downloaded 3 file(s) to", 
                $"should display multi-file success message with count. Actual output: {consoleOutput}");
            
            // Verify destination path is shown
            consoleOutput.Should().Contain(env.RemoteFileSystem.LocalTestDir.TrimEnd('/', '\\'),
                "success message should include destination path");
        }

        #endregion

        #region UX-012: Progress Display for Large Downloads

        /// <summary>
        /// Implements: UX-012, T076
        /// When: User downloads file >= DownloadConstants.ProgressDisplayThreshold (25MB)
        /// Then: Progress bar is displayed during download
        /// 
        /// This test uses WriteLog to capture transient progress output that would
        /// otherwise be cleared by AutoClear(true) before we can inspect it.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_LargeFile_DisplaysProgressBar()
        {
            using var env = TestEnvironment.WithServer();
            
            // Enable write logging to capture transient progress bar output
            env.Console.WriteLogEnabled = true;
            
            await env.ConnectToServerAsync();

            // Create a large file at exactly the threshold (25MB) to trigger progress display
            var fileSize = DownloadConstants.ProgressDisplayThreshold;
            var serverPath = env.RemoteFileSystem.CreateServerFile("large-file.bin", size: fileSize);

            // Execute download command
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify download succeeded
            result.ResultCode.Should().Be(0, "download should succeed");
            var downloadedFile = env.RemoteFileSystem.LocalPath("large-file.bin");
            File.Exists(downloadedFile).Should().BeTrue("downloaded file should exist locally");
            new FileInfo(downloadedFile).Length.Should().Be(fileSize, "file size should match");

            // Verify progress bar was displayed by checking the write log
            // The progress bar uses Unicode block characters like ━ (U+2501) or █ (U+2588)
            // and percentage indicators
            
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeTrue(
                $"progress bar should be displayed for files >= 25MB. WriteLog length: {env.Console.WriteLog.Contents.Length} chars. " +
                $"Sample (first 500 chars): {env.Console.WriteLog.Contents.Substring(0, Math.Min(500, env.Console.WriteLog.Contents.Length))}");
        }

        #endregion

        #region UX-013: No Progress for Small Downloads

        /// <summary>
        /// Implements: UX-013, T077
        /// When: User downloads small files via pattern (aggregate < 25MB threshold)
        /// Then: No progress bar is displayed, clean output only
        /// 
        /// NOTE: Single file downloads always show progress bar because the file size
        /// is not known upfront without an additional RPC call. UX-013 threshold check
        /// applies to DownloadWithPattern (multiple files) where sizes are known.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_SmallFilesViaPattern_NoProgressBar()
        {
            using var env = TestEnvironment.WithServer();
            
            // Enable write logging to check for absence of progress bar
            env.Console.WriteLogEnabled = true;
            
            await env.ConnectToServerAsync();

            // Create small files that together are below threshold (< 25MB aggregate)
            env.RemoteFileSystem.CreateServerFile("small1.log", "Small content 1");
            env.RemoteFileSystem.CreateServerFile("small2.log", "Small content 2");

            // Execute download with pattern - needs full path with server folder prefix
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.log";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify download succeeded
            result.ResultCode.Should().Be(0, "download should succeed");
            File.Exists(env.RemoteFileSystem.LocalPath("small1.log")).Should().BeTrue("small1.log should exist locally");
            File.Exists(env.RemoteFileSystem.LocalPath("small2.log")).Should().BeTrue("small2.log should exist locally");

            // Verify NO progress bar was displayed (aggregate size < 25MB)
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeFalse(
                "progress bar should NOT be displayed when aggregate size < 25MB threshold (UX-013)");
            
            // Verify success message is shown
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("Downloaded", "success message should be displayed");
        }

        /// <summary>
        /// Documents current behavior: Single file downloads always show progress bar.
        /// The DownloadSingleFile method cannot check threshold because file size is
        /// unknown without an extra RPC call. Progress starts indeterminate, then updates
        /// when Content-Length is received from the transfer.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_SingleFile_AlwaysShowsProgress()
        {
            using var env = TestEnvironment.WithServer();
            
            env.Console.WriteLogEnabled = true;
            
            await env.ConnectToServerAsync();

            // Create any single file - even small files trigger progress display
            var serverPath = env.RemoteFileSystem.CreateServerFile("single-file.txt", "File content");

            // Execute single file download (uses DownloadSingleFile path)
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            result.ResultCode.Should().Be(0, "download should succeed");
            
            // Single file downloads always show progress (current behavior)
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeTrue(
                "single file downloads always show progress bar (size unknown upfront)");
        }

        #endregion

        #region UX-014: Aggregate Progress for Multiple Files

        /// <summary>
        /// Implements: UX-014, T078
        /// When: User downloads multiple files with aggregate size >= DownloadConstants.ProgressDisplayThreshold
        /// Then: Aggregate progress bar is displayed
        /// 
        /// Each individual file is below threshold, but combined they exceed it.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_MultipleFiles_AggregateAboveThreshold_DisplaysProgressBar()
        {
            using var env = TestEnvironment.WithServer();
            
            // Enable write logging to capture transient progress bar output
            env.Console.WriteLogEnabled = true;
            
            await env.ConnectToServerAsync();

            // Create 3 files at 10MB each = 30MB total (above 25MB threshold)
            // Each individual file is below the threshold
            var fileSizeEach = 10L * 1024 * 1024; // 10 MB
            env.RemoteFileSystem.CreateServerFile("chunk1.bin", size: fileSizeEach);
            env.RemoteFileSystem.CreateServerFile("chunk2.bin", size: fileSizeEach);
            env.RemoteFileSystem.CreateServerFile("chunk3.bin", size: fileSizeEach);

            // Execute download command with glob pattern
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.bin";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify download succeeded
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed. Console: {consoleOutput}");
            File.Exists(env.RemoteFileSystem.LocalPath("chunk1.bin")).Should().BeTrue("chunk1.bin should be downloaded");
            File.Exists(env.RemoteFileSystem.LocalPath("chunk2.bin")).Should().BeTrue("chunk2.bin should be downloaded");
            File.Exists(env.RemoteFileSystem.LocalPath("chunk3.bin")).Should().BeTrue("chunk3.bin should be downloaded");

            // Verify progress bar was displayed due to aggregate size
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeTrue(
                $"aggregate progress bar should be displayed when total size >= 25MB (3 x 10MB = 30MB). " +
                $"WriteLog length: {env.Console.WriteLog.Contents.Length} chars");
        }

        /// <summary>
        /// Implements: UX-014 (negative case)
        /// When: User downloads multiple files with aggregate size < DownloadConstants.ProgressDisplayThreshold
        /// Then: No progress bar is displayed
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_MultipleFiles_AggregateBelowThreshold_NoProgressBar()
        {
            using var env = TestEnvironment.WithServer();
            
            // Enable write logging to check for absence of progress bar
            env.Console.WriteLogEnabled = true;
            
            await env.ConnectToServerAsync();

            // Create 3 small files at 5MB each = 15MB total (below 25MB threshold)
            var fileSizeEach = 5L * 1024 * 1024; // 5 MB
            env.RemoteFileSystem.CreateServerFile("small1.bin", size: fileSizeEach);
            env.RemoteFileSystem.CreateServerFile("small2.bin", size: fileSizeEach);
            env.RemoteFileSystem.CreateServerFile("small3.bin", size: fileSizeEach);

            // Execute download command with glob pattern
            var globPattern = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/*.bin";
            var result = await env.Cli.Run($"server download \"{globPattern}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify download succeeded
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed. Console: {consoleOutput}");

            // Verify NO progress bar was displayed (aggregate 15MB < 25MB threshold)
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeFalse(
                "no progress bar should be displayed when aggregate size < 25MB (3 x 5MB = 15MB)");
            
            // Verify success message is shown
            consoleOutput.Should().Contain("Downloaded 3 file(s)", "success message should show file count");
        }

        #endregion

        #region IT-005: Checksum Verification E2E

        /// <summary>
        /// Implements: IT-005, T128
        /// End-to-end checksum verification: server sends X-File-Checksum header,
        /// client computes checksum during download, download succeeds when checksums match.
        /// </summary>
        [TestMethod]
        public async Task DownloadCommand_ChecksumVerification_FileIntegrityPreserved()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create file with known content for checksum verification
            var knownContent = "This is test content for checksum verification E2E - IT-005";
            var serverPath = env.RemoteFileSystem.CreateServerFile("checksum-test.txt", knownContent);

            // Compute expected checksum
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var expectedChecksum = Convert.ToHexString(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(knownContent)));

            // Execute download command
            var result = await env.Cli.Run($"server download \"{serverPath}\" \"{env.RemoteFileSystem.LocalDestination}\"");

            // Verify download succeeded (checksum verification passed internally)
            var consoleOutput = string.Concat(env.Console.Lines);
            result.ResultCode.Should().Be(0, $"download should succeed when checksum matches. Console: {consoleOutput}");

            // Verify downloaded file exists and content matches
            var downloadedFile = env.RemoteFileSystem.LocalPath("checksum-test.txt");
            File.Exists(downloadedFile).Should().BeTrue("downloaded file should exist");

            var downloadedContent = File.ReadAllText(downloadedFile);
            downloadedContent.Should().Be(knownContent, "downloaded content should match original exactly");

            // Verify downloaded file checksum matches expected
            using var sha256Verify = System.Security.Cryptography.SHA256.Create();
            var actualChecksum = Convert.ToHexString(sha256Verify.ComputeHash(System.Text.Encoding.UTF8.GetBytes(downloadedContent)));
            actualChecksum.Should().Be(expectedChecksum, "downloaded file checksum should match server's computed checksum");
        }

        #endregion
    }
}

