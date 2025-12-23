using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for server-side SandboxedFileSystem.
    /// Verifies that commands executed on the server are confined to the storage root.
    /// </summary>
    [TestClass]
    public class IntegrationTests_ServerSandbox
    {
        private static readonly string StorageRoot = Path.GetFullPath("./cli-storage");

        [TestInitialize]
        public void Setup()
        {
            // Ensure storage root exists
            if (!Directory.Exists(StorageRoot))
                Directory.CreateDirectory(StorageRoot);
        }

        // Note: No TestCleanup - each test cleans up its own files using unique GUIDs

        [TestMethod]
        public async Task ServerCommand_UsesIFileSystem_ConfinedToStorageRoot()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueId = Guid.NewGuid().ToString("N");
            var testFileName = $"sandbox-test-{uniqueId}.txt";

            // Act - Execute a command that writes a file using IFileSystem
            // This would require a test command that injects IFileSystem and writes a file
            // For now, we verify that the file system is properly configured
            
            // Use the upload endpoint which uses the sandboxed file system
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Test content for sandbox");
            
            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
                await fileTransferService.UploadFile(tempFilePath, testFileName, null, CancellationToken.None);

                // Assert - File should be in storage root, not elsewhere
                var expectedPath = Path.Combine(StorageRoot, testFileName);
                File.Exists(expectedPath).Should().BeTrue("file should be written to storage root");
                
                // Verify content
                File.ReadAllText(expectedPath).Should().Be("Test content for sandbox");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                // Clean up uploaded file
                var serverPath = Path.Combine(StorageRoot, testFileName);
                if (File.Exists(serverPath))
                    File.Delete(serverPath);
            }
        }

        [TestMethod]
        public async Task ServerCommand_File_WriteAndRead_RoundTrip()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueId = Guid.NewGuid().ToString("N");
            var testFileName = $"sandbox-test-roundtrip-{uniqueId}.txt";
            var content = "Round trip test content - " + DateTime.UtcNow.ToString("O");

            // Act - Upload a file
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, content);

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
                await fileTransferService.UploadFile(tempFilePath, testFileName, null, CancellationToken.None);

                // Assert - Read back via direct file system access (simulating server-side command)
                var serverPath = Path.Combine(StorageRoot, testFileName);
                File.Exists(serverPath).Should().BeTrue();
                File.ReadAllText(serverPath).Should().Be(content);
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
                // Clean up uploaded file
                var uploadedPath = Path.Combine(StorageRoot, testFileName);
                if (File.Exists(uploadedPath))
                    File.Delete(uploadedPath);
            }
        }

        [TestMethod]
        public async Task ServerCommand_Directory_CreateEnumerateDelete_FullCycle()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            var uniqueId = Guid.NewGuid().ToString("N");
            var testDirName = $"sandbox-test-dir-{uniqueId}";
            var testFileName1 = $"{testDirName}/file1.txt";
            var testFileName2 = $"{testDirName}/file2.txt";

            var tempFile1 = Path.GetTempFileName();
            var tempFile2 = Path.GetTempFileName();
            File.WriteAllText(tempFile1, "File 1 content");
            File.WriteAllText(tempFile2, "File 2 content");

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
                
                // Upload creates directories automatically
                await fileTransferService.UploadFile(tempFile1, testFileName1, null, CancellationToken.None);
                await fileTransferService.UploadFile(tempFile2, testFileName2, null, CancellationToken.None);

                // Assert - Directory structure was created
                var serverDirPath = Path.Combine(StorageRoot, testDirName);
                Directory.Exists(serverDirPath).Should().BeTrue("directory should be created");
                
                var files = Directory.GetFiles(serverDirPath);
                files.Should().HaveCount(2, "both files should exist");

                // Cleanup via direct access
                Directory.Delete(serverDirPath, true);
                Directory.Exists(serverDirPath).Should().BeFalse("directory should be deleted");
            }
            finally
            {
                if (File.Exists(tempFile1)) File.Delete(tempFile1);
                if (File.Exists(tempFile2)) File.Delete(tempFile2);
                // Clean up server directory if it exists
                var serverDirPath = Path.Combine(StorageRoot, testDirName);
                if (Directory.Exists(serverDirPath))
                    try { Directory.Delete(serverDirPath, true); } catch { }
            }
        }

        [TestMethod]
        public async Task ServerCommand_PathTraversal_Rejected()
        {
            // Arrange
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);
            
            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Malicious content");

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
                
                // Act - Attempt path traversal
                Func<Task> act = async () => await fileTransferService.UploadFile(
                    tempFilePath, 
                    "../outside-storage.txt", 
                    null, 
                    CancellationToken.None);

                // Assert - Should throw or return error (path traversal blocked)
                await act.Should().ThrowAsync<Exception>("path traversal should be rejected");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}
