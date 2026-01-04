using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for file commands (ls, rm, mkdir, cat, info, upload, download).
    /// Tests use TestEnvironment for E2E testing with real server/client communication.
    /// </summary>
    [TestClass]
    public class IntegrationTests_FileCommands
    {
        private static readonly string StorageRoot = Path.GetFullPath("./cli-storage");

        [TestInitialize]
        public void Setup()
        {
            // Ensure storage root exists
            if (!Directory.Exists(StorageRoot))
                Directory.CreateDirectory(StorageRoot);
        }

        #region US1 - file ls command (T008-T015)

        [TestMethod]
        public async Task FileLs_ListsEmptyDirectory_ShowsEmptyMessage()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-ls-empty-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);

            try
            {
                // Act
                await env.Cli.Run($"file ls {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("empty", because: "empty directory should show empty message");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileLs_ListsDirectoryWithFiles_ShowsFilesAndDirectories()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-ls-files-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "test.txt"), "content");
            Directory.CreateDirectory(Path.Combine(dirPath, "subdir"));

            try
            {
                // Act
                await env.Cli.Run($"file ls {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("test.txt", because: "should list files");
                output.Should().Contain("subdir", because: "should list directories");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileLs_LongFormat_ShowsSizesAndDates()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-ls-long-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "test.txt"), "content");

            try
            {
                // Act
                await env.Cli.Run($"file ls {uniqueDir} -l");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("test.txt", because: "should list files");
                // Long format should show size and/or date
                output.Should().MatchRegex(@"\d+.*B|\d{4}-\d{2}-\d{2}", because: "long format should show size or date");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileLs_NonexistentPath_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file ls nonexistent-path-12345");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate path not found");
        }

        [TestMethod]
        public async Task FileLs_PathTraversalAttempt_RejectsAccess()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act - Try to list parent directory (path traversal)
            await env.Cli.Run("file ls ../");

            // Assert - Should be rejected by SandboxedFileSystem
            var output = env.Console.Buffer;
            output.Should().ContainAny(new[] { "access", "denied", "not found", "error", "outside" }, 
                because: "path traversal should be rejected");
        }

        #endregion

        #region US2 - file upload command (T016-T020)

        [TestMethod]
        public async Task FileUpload_ValidFile_UploadsSuccessfully()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-upload-{Guid.NewGuid():N}.txt";
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Test content for upload");

            try
            {
                // Act
                await env.Cli.Run($"file upload \"{tempFilePath}\" {uniqueFile}");

                // Assert - file should exist on server with correct content
                var serverPath = Path.Combine(StorageRoot, uniqueFile);
                File.Exists(serverPath).Should().BeTrue(because: "file should exist on server");
                File.ReadAllText(serverPath).Should().Be("Test content for upload");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                var serverPath = Path.Combine(StorageRoot, uniqueFile);
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
            }
        }

        [TestMethod]
        public async Task FileUpload_NonexistentSource_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file upload nonexistent-local-file.txt remote.txt");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate source file not found");
        }

        [TestMethod]
        public async Task FileUpload_NotConnected_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            // Do NOT connect to server

            // Create a temp file to upload
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Test content");

            try
            {
                // Act
                await env.Cli.Run($"file upload \"{tempFilePath}\" remote.txt");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("connect", because: "should indicate not connected");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        #endregion

        #region US3 - file download command (T021-T028)

        [TestMethod]
        public async Task FileDownload_ValidFile_DownloadsSuccessfully()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-download-{Guid.NewGuid():N}.txt";
            var serverPath = Path.Combine(StorageRoot, uniqueFile);
            File.WriteAllText(serverPath, "Test content for download");
            var tempDownloadPath = Path.Combine(Path.GetTempPath(), $"downloaded-{Guid.NewGuid():N}.txt");

            try
            {
                // Act
                await env.Cli.Run($"file download {uniqueFile} \"{tempDownloadPath}\"");

                // Assert - file should be downloaded with correct content
                File.Exists(tempDownloadPath).Should().BeTrue(because: "file should be downloaded");
                File.ReadAllText(tempDownloadPath).Should().Be("Test content for download");
            }
            finally
            {
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
                if (File.Exists(tempDownloadPath))
                    File.Delete(tempDownloadPath);
            }
        }

        [TestMethod]
        public async Task FileDownload_NonexistentRemote_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file download nonexistent-remote-file.txt local.txt");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate remote file not found");
        }

        [TestMethod]
        public async Task FileDownload_NotConnected_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            // Do NOT connect to server

            // Act
            await env.Cli.Run("file download remote.txt local.txt");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("connect", because: "should indicate not connected");
        }

        #endregion

        #region US4 - file rm command (T029-T035)

        [TestMethod]
        public async Task FileRm_ExistingFile_RemovesFile()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-rm-{Guid.NewGuid():N}.txt";
            var serverPath = Path.Combine(StorageRoot, uniqueFile);
            File.WriteAllText(serverPath, "Content to delete");

            // Act - Use -f to skip confirmation
            await env.Cli.Run($"file rm {uniqueFile} -f");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("Removed", because: "should indicate file was removed");
            File.Exists(serverPath).Should().BeFalse(because: "file should be deleted");
        }

        [TestMethod]
        public async Task FileRm_NonexistentFile_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file rm nonexistent-file.txt -f");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate file not found");
        }

        [TestMethod]
        public async Task FileRm_DirectoryWithoutRecursive_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-rm-dir-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");

            try
            {
                // Act
                await env.Cli.Run($"file rm {uniqueDir} -f");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("-r", because: "should indicate need for recursive flag");
                Directory.Exists(dirPath).Should().BeTrue(because: "directory should still exist");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileRm_DirectoryWithRecursive_RemovesDirectory()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-rm-recursive-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");

            // Act - Use -r for recursive and -f to skip confirmation
            await env.Cli.Run($"file rm {uniqueDir} -r -f");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("Removed", because: "should indicate directory was removed");
            Directory.Exists(dirPath).Should().BeFalse(because: "directory should be deleted");
        }

        [TestMethod]
        public async Task FileRm_PathTraversalAttempt_RejectsAccess()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act - Try to delete parent directory (path traversal)
            await env.Cli.Run("file rm ../outside -f");

            // Assert - Should be rejected by SandboxedFileSystem
            var output = env.Console.Buffer;
            output.Should().ContainAny(new[] { "access", "denied", "not found", "error", "outside" },
                because: "path traversal should be rejected");
        }

        #endregion

        #region US5 - file mkdir command (T036-T039)

        [TestMethod]
        public async Task FileMkdir_ValidPath_CreatesDirectory()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-mkdir-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);

            try
            {
                // Act
                await env.Cli.Run($"file mkdir {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("Created", because: "should indicate directory was created");
                Directory.Exists(dirPath).Should().BeTrue(because: "directory should exist");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileMkdir_ExistingDirectory_ShowsWarning()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-mkdir-exists-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);

            try
            {
                // Act
                await env.Cli.Run($"file mkdir {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("already exists", because: "should indicate directory already exists");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileMkdir_NestedPath_CreatesAllDirectories()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-mkdir-nested-{Guid.NewGuid():N}/level1/level2";
            var basePath = Path.Combine(StorageRoot, uniqueDir.Split('/')[0]);

            try
            {
                // Act
                await env.Cli.Run($"file mkdir {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("Created", because: "should indicate directory was created");
                var fullPath = Path.Combine(StorageRoot, uniqueDir.Replace('/', Path.DirectorySeparatorChar));
                Directory.Exists(fullPath).Should().BeTrue(because: "nested directory should exist");
            }
            finally
            {
                if (Directory.Exists(basePath))
                    Directory.Delete(basePath, true);
            }
        }

        #endregion

        #region US6 - file cat command (T040-T043)

        [TestMethod]
        public async Task FileCat_ValidTextFile_DisplaysContent()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-cat-{Guid.NewGuid():N}.txt";
            var serverPath = Path.Combine(StorageRoot, uniqueFile);
            var content = "Line 1\nLine 2\nLine 3";
            File.WriteAllText(serverPath, content);

            try
            {
                // Act
                await env.Cli.Run($"file cat {uniqueFile}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("Line 1", because: "should display file content");
                output.Should().Contain("Line 2", because: "should display file content");
                output.Should().Contain("Line 3", because: "should display file content");
            }
            finally
            {
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
            }
        }

        [TestMethod]
        public async Task FileCat_WithLineNumbers_DisplaysLineNumbers()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-cat-ln-{Guid.NewGuid():N}.txt";
            var serverPath = Path.Combine(StorageRoot, uniqueFile);
            var content = "Line 1\nLine 2\nLine 3";
            File.WriteAllText(serverPath, content);

            try
            {
                // Act
                await env.Cli.Run($"file cat {uniqueFile} -n");

                // Assert
                var output = env.Console.Buffer;
                output.Should().MatchRegex(@"1.*Line 1|Line 1.*1", because: "should show line numbers");
            }
            finally
            {
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
            }
        }

        [TestMethod]
        public async Task FileCat_NonexistentFile_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file cat nonexistent-file.txt");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate file not found");
        }

        #endregion

        #region US7 - file info command (T044-T047)

        [TestMethod]
        public async Task FileInfo_ValidFile_DisplaysFileInfo()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueFile = $"test-info-{Guid.NewGuid():N}.txt";
            var serverPath = Path.Combine(StorageRoot, uniqueFile);
            File.WriteAllText(serverPath, "Test content");

            try
            {
                // Act
                await env.Cli.Run($"file info {uniqueFile}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("File", because: "should show file type");
                output.Should().Contain(uniqueFile, because: "should show file name");
                output.Should().MatchRegex(@"\d+.*B|Size", because: "should show size");
            }
            finally
            {
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
            }
        }

        [TestMethod]
        public async Task FileInfo_ValidDirectory_DisplaysDirectoryInfo()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueDir = $"test-info-dir-{Guid.NewGuid():N}";
            var dirPath = Path.Combine(StorageRoot, uniqueDir);
            Directory.CreateDirectory(dirPath);
            File.WriteAllText(Path.Combine(dirPath, "file.txt"), "content");

            try
            {
                // Act
                await env.Cli.Run($"file info {uniqueDir}");

                // Assert
                var output = env.Console.Buffer;
                output.Should().Contain("Directory", because: "should show directory type");
                output.Should().MatchRegex(@"Files.*1|1.*file", because: "should show file count");
            }
            finally
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, true);
            }
        }

        [TestMethod]
        public async Task FileInfo_NonexistentPath_ShowsError()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            // Act
            await env.Cli.Run("file info nonexistent-path");

            // Assert
            var output = env.Console.Buffer;
            output.Should().Contain("not found", because: "should indicate path not found");
        }

        #endregion
    }
}
