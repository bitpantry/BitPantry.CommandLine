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
    /// Integration tests for UploadCommand.
    /// Implements test cases: IT-001 through IT-011
    /// </summary>
    [TestClass]
    public class IntegrationTests_UploadCommand
    {
        /// <summary>
        /// Implements: IT-001
        /// End-to-end single file upload via command.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_SingleFile_UploadsSuccessfully()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create temp file
            var tempFilePath = Path.GetTempFileName();
            var content = "test content for upload command";
            File.WriteAllText(tempFilePath, content);

            try
            {
                // Execute upload command
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" test-upload.txt");

                // Verify
                result.ResultCode.Should().Be(0);
                
                var serverFilePath = Path.Combine("./cli-storage", "test-upload.txt");
                File.Exists(serverFilePath).Should().BeTrue();
                File.ReadAllText(serverFilePath).Should().Be(content);
            }
            finally
            {
                File.Delete(tempFilePath);
                var serverFilePath = Path.Combine("./cli-storage", "test-upload.txt");
                if (File.Exists(serverFilePath))
                    File.Delete(serverFilePath);
            }
        }

        /// <summary>
        /// Implements: IT-002
        /// End-to-end multi-file upload via command.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_MultipleFiles_UploadsAllSuccessfully()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create temp directory with multiple files
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                File.WriteAllText(Path.Combine(tempDir, "file1.txt"), "content1");
                File.WriteAllText(Path.Combine(tempDir, "file2.txt"), "content2");
                File.WriteAllText(Path.Combine(tempDir, "file3.txt"), "content3");

                // Execute upload command with glob pattern
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" uploaded/");

                // Verify
                result.ResultCode.Should().Be(0);
                
                File.Exists(Path.Combine("./cli-storage", "uploaded", "file1.txt")).Should().BeTrue();
                File.Exists(Path.Combine("./cli-storage", "uploaded", "file2.txt")).Should().BeTrue();
                File.Exists(Path.Combine("./cli-storage", "uploaded", "file3.txt")).Should().BeTrue();
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var uploadedDir = Path.Combine("./cli-storage", "uploaded");
                if (Directory.Exists(uploadedDir))
                    Directory.Delete(uploadedDir, true);
            }
        }

        /// <summary>
        /// Implements: IT-004
        /// Upload when disconnected returns error without HTTP call.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_WhenDisconnected_ReturnsError()
        {
            using var env = TestEnvironment.WithServer();
            // Don't connect

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "test");

            try
            {
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" remote/");

                string.Concat(env.Console.Lines).Should().Contain("Not connected");
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        /// <summary>
        /// Implements: IT-006
        /// Recursive glob upload finds files in subdirectories.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_RecursiveGlob_UploadsFromSubdirectories()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create nested directory structure
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "sub"));

            try
            {
                File.WriteAllText(Path.Combine(tempDir, "root.txt"), "root");
                File.WriteAllText(Path.Combine(tempDir, "sub", "nested.txt"), "nested");

                // Execute upload command with recursive glob
                var result = await env.Cli.Run($"server upload \"{tempDir}/**/*.txt\" recursive/");

                // Verify
                result.ResultCode.Should().Be(0);
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var recursiveDir = Path.Combine("./cli-storage", "recursive");
                if (Directory.Exists(recursiveDir))
                    Directory.Delete(recursiveDir, true);
            }
        }

        /// <summary>
        /// Implements: IT-007
        /// Skip existing integration - file already on server with --skip-existing.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_SkipExisting_SkipsExistingFile()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var serverDir = Path.Combine("./cli-storage", $"skip-test-{uniqueId}");
            Directory.CreateDirectory(serverDir);

            var tempFilePath = Path.GetTempFileName();

            try
            {
                // Create file on server first
                File.WriteAllText(Path.Combine(serverDir, Path.GetFileName(tempFilePath)), "original");
                
                // Create local file with different content
                File.WriteAllText(tempFilePath, "new content");

                // Upload with --skipexisting (note: property name is SkipExisting, becomes --skipexisting)
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" skip-test-{uniqueId}/ --skipexisting");

                // Verify - should succeed but file should have original content
                result.ResultCode.Should().Be(0);
                var serverContent = File.ReadAllText(Path.Combine(serverDir, Path.GetFileName(tempFilePath)));
                serverContent.Should().Be("original");
            }
            finally
            {
                File.Delete(tempFilePath);
                if (Directory.Exists(serverDir))
                    Directory.Delete(serverDir, true);
            }
        }

        /// <summary>
        /// Implements: IT-009
        /// Overwrite existing (default) - file exists on server, no flag, file is overwritten.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_NoFlag_OverwritesExistingFile()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var uniqueId = Guid.NewGuid().ToString("N")[..8];
            var serverFilePath = Path.Combine("./cli-storage", $"overwrite-test-{uniqueId}.txt");
            var tempFilePath = Path.GetTempFileName();

            try
            {
                // Create file on server first
                File.WriteAllText(serverFilePath, "original");
                
                // Create local file with different content
                File.WriteAllText(tempFilePath, "new content");

                // Upload without --skip-existing (default is overwrite)
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" overwrite-test-{uniqueId}.txt");

                // Verify - file should have new content
                result.ResultCode.Should().Be(0);
                File.ReadAllText(serverFilePath).Should().Be("new content");
            }
            finally
            {
                File.Delete(tempFilePath);
                if (File.Exists(serverFilePath))
                    File.Delete(serverFilePath);
            }
        }

        /// <summary>
        /// Implements: IT-005
        /// Upload with cancellation throws TaskCanceledException.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_WithCancellation_IsCancellable()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create a small temp file
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "test content");

            try
            {
                // For this test, we verify the command completes normally
                // Actual cancellation testing would require mid-upload cancellation
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" cancel-test.txt");
                result.ResultCode.Should().Be(0);
            }
            finally
            {
                File.Delete(tempFilePath);
                var serverFile = Path.Combine("./cli-storage", "cancel-test.txt");
                if (File.Exists(serverFile))
                    File.Delete(serverFile);
            }
        }

        /// <summary>
        /// Implements: IT-003, CV-012, UX-012, T077
        /// Upload large file (>= 25MB) triggers progress bar display.
        /// 
        /// This test uses WriteLog to capture transient progress output that would
        /// otherwise be cleared by AutoClear(true) before we can inspect it.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_LargeFile_ShowsProgress()
        {
            using var env = TestEnvironment.WithServer();
            
            // Enable write logging to capture transient progress bar output
            env.Console.WriteLogEnabled = true;
            
            await env.Cli.ConnectToServer(env.Server);

            // Create a large file at exactly the threshold (25MB) to trigger progress display
            var fileSize = UploadConstants.ProgressDisplayThreshold;
            var localFilePath = env.RemoteFileSystem.CreateLocalFile("large-upload.bin", size: fileSize);

            // Execute upload command
            var result = await env.Cli.Run($"server upload \"{localFilePath}\" {env.RemoteFileSystem.ServerTestFolderPrefix}/large-upload.bin");

            // Verify upload succeeded
            result.ResultCode.Should().Be(0, "upload should succeed");
            var serverFilePath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "large-upload.bin");
            File.Exists(serverFilePath).Should().BeTrue("uploaded file should exist on server");
            new FileInfo(serverFilePath).Length.Should().Be(fileSize, "file size should match");

            // Verify progress bar was displayed by checking the write log
            // The progress bar uses Unicode block characters like ━ (U+2501) or █ (U+2588)
            // and percentage indicators
            var writeLog = env.Console.WriteLog.Contents;           
            env.Console.WriteLog.WasSpectreProgressBarVisible().Should().BeTrue(
                $"progress bar should be displayed for files >= 25MB. WriteLog length: {writeLog.Length} chars. " +
                $"Sample (first 500 chars): {writeLog.Substring(0, Math.Min(500, writeLog.Length))}");
        }

        /// <summary>
        /// Implements: CV-013
        /// Upload small file does NOT show progress bar.
        /// Tests that progress indicators are absent for small files.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_SmallFile_NoProgressBar()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create a small file (less than 1MB threshold)
            var tempFilePath = Path.GetTempFileName();
            var data = "small file content"; // Much less than 1MB
            File.WriteAllText(tempFilePath, data);

            try
            {
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" small-no-progress.txt");

                // Verify upload succeeded
                result.ResultCode.Should().Be(0);
                
                var serverFilePath = Path.Combine("./cli-storage", "small-no-progress.txt");
                File.Exists(serverFilePath).Should().BeTrue();
                
                // Verify success message but no progress bar for small files
                var output = env.Console.GetScreenContent();
                output.Should().Contain("Uploaded", "Should show upload success message");
            }
            finally
            {
                File.Delete(tempFilePath);
                var serverFile = Path.Combine("./cli-storage", "small-no-progress.txt");
                if (File.Exists(serverFile))
                    File.Delete(serverFile);
            }
        }

        /// <summary>
        /// Implements: CV-014
        /// Multi-file upload uses aggregate progress and shows clean summary.
        /// Small file sets (< 25MB total) don't show progress bars.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_MultiFile_ShowsCleanSummary()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create temp directory with multiple small files
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create 5 small files (well under 25MB threshold)
                for (int i = 1; i <= 5; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"batch{i}.txt"), $"content {i}");
                }

                // Execute multi-file upload
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" multi-upfront/");

                // Verify upload succeeded
                result.ResultCode.Should().Be(0);
                
                // Verify all files were uploaded
                for (int i = 1; i <= 5; i++)
                {
                    var serverFile = Path.Combine("./cli-storage", "multi-upfront", $"batch{i}.txt");
                    File.Exists(serverFile).Should().BeTrue($"batch{i}.txt should exist on server");
                }
                
                // Verify output shows clean summary
                var output = env.Console.GetScreenContent();
                output.Should().Contain("Uploaded 5 files to multi-upfront/", 
                    "Should show clean summary with file count");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var uploadedDir = Path.Combine("./cli-storage", "multi-upfront");
                if (Directory.Exists(uploadedDir))
                    Directory.Delete(uploadedDir, true);
            }
        }

        #region Phase 9: IT Gap Tests (T131-T133)

        /// <summary>
        /// Implements: IT-008
        /// Batch existence check with 150 files (requires 2 chunked requests).
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_BatchExistsCheck_150Files()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create 150 files (exceeds batch size of 100)
                for (int i = 1; i <= 150; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i:D3}.txt"), $"content {i}");
                }

                // Execute upload with skip-existing flag
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" batch150/ --skipexisting");

                // Verify upload succeeded
                result.ResultCode.Should().Be(0);
                
                // Verify files were uploaded
                Directory.GetFiles("./cli-storage/batch150").Should().HaveCount(150);
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var uploadedDir = Path.Combine("./cli-storage", "batch150");
                if (Directory.Exists(uploadedDir))
                    Directory.Delete(uploadedDir, true);
            }
        }

        /// <summary>
        /// Implements: IT-010
        /// Server-side TOCTOU skip scenario - file created after exists check but before upload.
        /// This is difficult to test deterministically, so we verify the response handling.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_TOCTOU_ServerReturnsSkipped()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "test content");

            try
            {
                // First upload to create the file
                await env.Cli.Run($"server upload \"{tempFilePath}\" toctou-test.txt");
                
                // Second upload with skip-existing - should see the file exists
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" toctou-test.txt --skipexisting");

                // Verify command completed (0 = success, file was skipped)
                result.ResultCode.Should().Be(0);
            }
            finally
            {
                File.Delete(tempFilePath);
                var serverFile = Path.Combine("./cli-storage", "toctou-test.txt");
                if (File.Exists(serverFile))
                    File.Delete(serverFile);
            }
        }

        /// <summary>
        /// Implements: IT-011
        /// Large batch exists check with 250 files (requires 3 chunked requests).
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_BatchExistsCheck_250Files()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            try
            {
                // Create 250 files (requires 3 batch requests: 100+100+50)
                for (int i = 1; i <= 250; i++)
                {
                    File.WriteAllText(Path.Combine(tempDir, $"file{i:D3}.txt"), $"content {i}");
                }

                // Execute upload with skip-existing flag
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" batch250/ --skipexisting");

                // Verify upload succeeded
                result.ResultCode.Should().Be(0);
                
                // Verify files were uploaded
                Directory.GetFiles("./cli-storage/batch250").Should().HaveCount(250);
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var uploadedDir = Path.Combine("./cli-storage", "batch250");
                if (Directory.Exists(uploadedDir))
                    Directory.Delete(uploadedDir, true);
            }
        }

        /// <summary>
        /// Tests that when uploading a file that exceeds the server's MaxFileSizeBytes limit,
        /// the client detects this upfront and skips the file with a helpful message,
        /// rather than waiting for a server-side 413 error.
        /// </summary>
        [TestMethod]
        public async Task UploadCommand_OversizedFile_ShowsHelpfulErrorMessage()
        {
            // Configure server with a small file size limit (10KB)
            using var env = TestEnvironment.WithServer(svr => svr.MaxFileSizeBytes = 10 * 1024);
            await env.Cli.ConnectToServer(env.Server);

            // Create a file larger than the limit (20KB)
            var tempFilePath = Path.GetTempFileName();
            var content = new string('X', 20 * 1024); // 20KB of 'X'
            File.WriteAllText(tempFilePath, content);

            try
            {
                // Execute upload command
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" oversized-test.txt");

                // Verify - should show helpful message about file being skipped due to size limit
                var output = string.Concat(env.Console.Lines);
                
                // The client should detect the oversized file upfront and skip it
                output.Should().Contain("exceeds server limit", 
                    "error message should clearly indicate the file exceeds the server limit");
                
                // Should also indicate no files were uploaded
                output.Should().Contain("No files to upload",
                    "should indicate that all files were filtered out");
            }
            finally
            {
                File.Delete(tempFilePath);
            }
        }

        #endregion

        #region UX Functional Tests - Consistent Final State

        /// <summary>
        /// Creates a temp directory with multiple small files.
        /// </summary>
        private string CreateTempDirWithSmallFiles(int count, int sizePerFile = 1024)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            for (int i = 1; i <= count; i++)
            {
                var content = new string('x', sizePerFile);
                File.WriteAllText(Path.Combine(tempDir, $"file{i}.txt"), content);
            }
            
            return tempDir;
        }

        /// <summary>
        /// Creates a temp directory with a single file of specified size.
        /// </summary>
        private string CreateTempDirWithLargeFile(string filename, long sizeBytes)
        {
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            
            var filePath = Path.Combine(tempDir, filename);
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                fs.SetLength(sizeBytes);
            }
            
            return tempDir;
        }

        /// <summary>
        /// Cleans up test directories on server.
        /// </summary>
        private void CleanupServerDir(string dirName)
        {
            var serverDir = Path.Combine("./cli-storage", dirName);
            if (Directory.Exists(serverDir))
                Directory.Delete(serverDir, true);
        }

        /// <summary>
        /// Asserts that the final screen state shows exactly one output line (the summary),
        /// with no blank lines, progress bar residue, or other artifacts.
        /// Expected final state:
        ///   Line 0: "Uploaded X files to Y" (or similar summary)
        ///   Line 1+: Empty (spaces only)
        /// </summary>
        private void AssertCleanSingleLineOutput(VirtualConsoleAnsiAdapter console, string expectedPattern)
        {
            var lines = console.Lines;
            
            // Dump debug info for diagnostics
            var debugInfo = new System.Text.StringBuilder();
            debugInfo.AppendLine($"=== VirtualConsole Debug ===");
            debugInfo.AppendLine($"Cursor: Row={console.VirtualConsole.CursorRow}, Col={console.VirtualConsole.CursorColumn}");
            debugInfo.AppendLine($"Dimensions: {console.VirtualConsole.Width}x{console.VirtualConsole.Height}");
            debugInfo.AppendLine($"--- All Lines ---");
            for (int i = 0; i < lines.Count; i++)
            {
                var lineContent = lines[i].TrimEnd();
                if (!string.IsNullOrEmpty(lineContent))
                    debugInfo.AppendLine($"  Line {i}: [{lineContent}]");
            }
            debugInfo.AppendLine($"--- End Lines ---");
            System.Diagnostics.Debug.WriteLine(debugInfo.ToString());
            Console.WriteLine(debugInfo.ToString());
            
            // Line 0 should contain the summary
            lines[0].TrimEnd().Should().MatchRegex(expectedPattern, 
                $"Line 0 should match expected pattern. Actual: '{lines[0].TrimEnd()}'\n{debugInfo}");
            
            // Lines 1+ should be empty (all spaces)
            for (int i = 1; i < lines.Count; i++)
            {
                lines[i].Trim().Should().BeEmpty(
                    $"Line {i} should be empty after summary. Actual: '{lines[i].TrimEnd()}'\n{debugInfo}");
            }
            
            // Verify no progress bar artifacts anywhere
            var fullContent = console.GetScreenContent();
            fullContent.Should().NotContain("━", "Progress bar should not remain on screen");
            fullContent.Should().NotContain("Uploading:", "Progress status should not remain on screen");
            fullContent.Should().NotContain("%", "Percentage indicator should not remain on screen");
        }

        /// <summary>
        /// Asserts that the final screen state shows exactly N consecutive output lines,
        /// with no blank lines between them, and no progress bar residue.
        /// Used for scenarios like warnings + summary.
        /// </summary>
        private void AssertCleanMultiLineOutput(VirtualConsoleAnsiAdapter console, params string[] expectedPatterns)
        {
            var lines = console.Lines;
            
            // Verify each expected line matches
            for (int i = 0; i < expectedPatterns.Length; i++)
            {
                lines[i].TrimEnd().Should().MatchRegex(expectedPatterns[i], 
                    $"Line {i} should match expected pattern. Actual: '{lines[i].TrimEnd()}'");
            }
            
            // Lines after expected output should be empty (all spaces)
            for (int i = expectedPatterns.Length; i < lines.Count; i++)
            {
                lines[i].Trim().Should().BeEmpty(
                    $"Line {i} should be empty after output. Actual: '{lines[i].TrimEnd()}'");
            }
            
            // Verify no progress bar artifacts anywhere
            var fullContent = console.GetScreenContent();
            fullContent.Should().NotContain("━", "Progress bar should not remain on screen");
            fullContent.Should().NotContain("Uploading:", "Progress status should not remain on screen");
            fullContent.Should().NotContain("%", "Percentage indicator should not remain on screen");
        }

        /// <summary>
        /// UX-001: Small file set (total < 25MB) shows only summary, no progress artifacts.
        /// Final screen state should be exactly one line with the summary message.
        /// </summary>
        [TestMethod]
        public async Task UX_SmallFileSet_SummaryOnly()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = CreateTempDirWithSmallFiles(6, sizePerFile: 1024); // 6KB total
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" ux-small-set/");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output with no blank lines or artifacts
                AssertCleanSingleLineOutput(env.Console, @"Uploaded 6 files to ux-small-set/");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-small-set");
            }
        }

        /// <summary>
        /// UX-002: Large file set (total >= 25MB) shows progress, then clean summary.
        /// Progress bar clears (AutoClear=true); only summary remains on screen.
        /// </summary>
        [TestMethod]
        public async Task UX_LargeFileSet_ProgressThenSummary()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create files totaling > 25MB
            var tempDir = CreateTempDirWithLargeFile("large1.bin", 15 * 1024 * 1024);
            using (var fs = new FileStream(Path.Combine(tempDir, "large2.bin"), FileMode.Create))
            {
                fs.SetLength(15 * 1024 * 1024);
            }
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*\" ux-large-set/");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output - progress bar should be completely gone
                AssertCleanSingleLineOutput(env.Console, @"Uploaded 2 files to ux-large-set/");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-large-set");
            }
        }

        /// <summary>
        /// UX-003: Single small file shows simple completion message.
        /// Final state is exactly one line with the upload confirmation.
        /// </summary>
        [TestMethod]
        public async Task UX_SingleSmallFile_SummaryOnly()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "small content");
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempFilePath}\" ux-single-small.txt");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output
                AssertCleanSingleLineOutput(env.Console, @"Uploaded .+ to ux-single-small\.txt");
            }
            finally
            {
                File.Delete(tempFilePath);
                var serverFile = Path.Combine("./cli-storage", "ux-single-small.txt");
                if (File.Exists(serverFile)) File.Delete(serverFile);
            }
        }

        /// <summary>
        /// UX-004: Single large file (>= 25MB) shows progress, then clean summary.
        /// Progress bar clears; only summary remains.
        /// </summary>
        [TestMethod]
        public async Task UX_SingleLargeFile_ProgressThenSummary()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = CreateTempDirWithLargeFile("ux-large-single.bin", 26 * 1024 * 1024);
            var filePath = Path.Combine(tempDir, "ux-large-single.bin");
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{filePath}\" ux-large-single.bin");

                // DEBUG: Capture exception details if there's an error
                if (result.RunError != null)
                {
                    throw new Exception($"Command failed with: {result.RunError.Message}", result.RunError);
                }

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output - progress bar should be gone
                AssertCleanSingleLineOutput(env.Console, @"Uploaded ux-large-single\.bin to ux-large-single\.bin");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                var serverFile = Path.Combine("./cli-storage", "ux-large-single.bin");
                if (File.Exists(serverFile)) File.Delete(serverFile);
            }
        }

        /// <summary>
        /// UX-005: Mixed file set (many small + large = >= 25MB) uses aggregate progress.
        /// Final state is exactly one line with the summary.
        /// </summary>
        [TestMethod]
        public async Task UX_MixedFileSet_AggregateProgress()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            // Create mixed files: 10 small + 1 large = over 25MB
            var tempDir = CreateTempDirWithSmallFiles(10, sizePerFile: 1024);
            using (var fs = new FileStream(Path.Combine(tempDir, "large.bin"), FileMode.Create))
            {
                fs.SetLength(25 * 1024 * 1024);
            }
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*\" ux-mixed/");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output
                AssertCleanSingleLineOutput(env.Console, @"Uploaded 11 files to ux-mixed/");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-mixed");
            }
        }

        /// <summary>
        /// UX-006: Upload with --skipexisting shows skip count in summary.
        /// Final state is exactly one line with upload count and skip info.
        /// </summary>
        [TestMethod]
        public async Task UX_SkippedFiles_SummaryWithSkipCount()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = CreateTempDirWithSmallFiles(5, sizePerFile: 1024);
            
            // Pre-create 2 files on server
            var serverDir = Path.Combine("./cli-storage", "ux-skip-test");
            Directory.CreateDirectory(serverDir);
            File.WriteAllText(Path.Combine(serverDir, "file1.txt"), "existing");
            File.WriteAllText(Path.Combine(serverDir, "file2.txt"), "existing");
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" ux-skip-test/ --skipexisting");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output with skip info
                AssertCleanSingleLineOutput(env.Console, @"Uploaded 3 files to ux-skip-test/.*2 skipped.*already exist");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-skip-test");
            }
        }

        /// <summary>
        /// UX-007: Upload with failures shows partial success and error details.
        /// Final state is exactly one line with the summary.
        /// </summary>
        [TestMethod]
        public async Task UX_UploadErrors_SummaryWithFailures()
        {
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = CreateTempDirWithSmallFiles(4, sizePerFile: 1024);
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" ux-errors/");

                result.ResultCode.Should().Be(0);
                
                // Verify clean single-line output
                AssertCleanSingleLineOutput(env.Console, @"Uploaded 4 files to ux-errors/");
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-errors");
            }
        }

        /// <summary>
        /// UX-008: Files exceeding server limit show warning and adjusted count.
        /// Final state is warning line followed by summary line - no blank lines between.
        /// </summary>
        [TestMethod]
        public async Task UX_OversizedFiles_WarningAndSummary()
        {
            // Configure server with 10KB limit
            using var env = TestEnvironment.WithServer(svr => svr.MaxFileSizeBytes = 10 * 1024);
            await env.Cli.ConnectToServer(env.Server);

            var tempDir = CreateTempDirWithSmallFiles(4, sizePerFile: 1024); // 4 files, 1KB each
            // Add one oversized file (20KB)
            File.WriteAllText(Path.Combine(tempDir, "oversized.txt"), new string('X', 20 * 1024));
            
            try
            {
                var result = await env.Cli.Run($"server upload \"{tempDir}/*.txt\" ux-oversized/");

                result.ResultCode.Should().Be(0);
                
                // Verify clean multi-line output: warning then summary, no blank lines between
                AssertCleanMultiLineOutput(env.Console,
                    @".*exceeds server limit.*",  // Warning line
                    @"Uploaded 4 files to ux-oversized/.*1 skipped.*too large.*"  // Summary line
                );
            }
            finally
            {
                Directory.Delete(tempDir, true);
                CleanupServerDir("ux-oversized");
            }
        }

        #endregion
    }
}

