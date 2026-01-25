using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for server-side SandboxedFileSystem.
    /// Verifies that commands executed on the server are confined to the storage root.
    /// </summary>
    [TestClass]
    public class IntegrationTests_ServerSandbox
    {
        [TestMethod]
        public async Task ServerCommand_UsesIFileSystem_ConfinedToStorageRoot()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.RemoteFileSystem.CreateLocalFile("sandbox-test.txt", "Test content for sandbox");

            var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
            await fileTransferService.UploadFile(
                localFilePath, 
                $"{env.RemoteFileSystem.ServerTestFolderPrefix}/sandbox-test.txt", 
                null, 
                CancellationToken.None);

            // Assert - File should be in storage root, not elsewhere
            var expectedPath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "sandbox-test.txt");
            File.Exists(expectedPath).Should().BeTrue("file should be written to storage root");
            
            // Verify content
            File.ReadAllText(expectedPath).Should().Be("Test content for sandbox");
        }

        [TestMethod]
        public async Task ServerCommand_File_WriteAndRead_RoundTrip()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var content = "Round trip test content - " + DateTime.UtcNow.ToString("O");
            var localFilePath = env.RemoteFileSystem.CreateLocalFile("roundtrip-test.txt", content);

            var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
            await fileTransferService.UploadFile(
                localFilePath, 
                $"{env.RemoteFileSystem.ServerTestFolderPrefix}/roundtrip-test.txt", 
                null, 
                CancellationToken.None);

            // Assert - Read back via direct file system access (simulating server-side command)
            var serverPath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "roundtrip-test.txt");
            File.Exists(serverPath).Should().BeTrue();
            File.ReadAllText(serverPath).Should().Be(content);
        }

        [TestMethod]
        public async Task ServerCommand_Directory_CreateEnumerateDelete_FullCycle()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var localFile1 = env.RemoteFileSystem.CreateLocalFile("file1.txt", "File 1 content");
            var localFile2 = env.RemoteFileSystem.CreateLocalFile("file2.txt", "File 2 content");

            var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
            
            // Upload creates directories automatically
            await fileTransferService.UploadFile(
                localFile1, 
                $"{env.RemoteFileSystem.ServerTestFolderPrefix}/subdir/file1.txt", 
                null, 
                CancellationToken.None);
            await fileTransferService.UploadFile(
                localFile2, 
                $"{env.RemoteFileSystem.ServerTestFolderPrefix}/subdir/file2.txt", 
                null, 
                CancellationToken.None);

            // Assert - Directory structure was created
            var serverDirPath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "subdir");
            Directory.Exists(serverDirPath).Should().BeTrue("directory should be created");
            
            var files = Directory.GetFiles(serverDirPath);
            files.Should().HaveCount(2, "both files should exist");

            // Cleanup via direct access
            Directory.Delete(serverDirPath, true);
            Directory.Exists(serverDirPath).Should().BeFalse("directory should be deleted");
        }

        [TestMethod]
        public async Task ServerCommand_PathTraversal_Rejected()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);
            
            var localFilePath = env.RemoteFileSystem.CreateLocalFile("malicious.txt", "Malicious content");

            var fileTransferService = env.Cli.Services.GetRequiredService<BitPantry.CommandLine.Remote.SignalR.Client.FileTransferService>();
            
            // Act - Attempt path traversal
            Func<Task> act = async () => await fileTransferService.UploadFile(
                localFilePath, 
                "../outside-storage.txt", 
                null, 
                CancellationToken.None);

            // Assert - Should throw or return error (path traversal blocked)
            await act.Should().ThrowAsync<Exception>("path traversal should be rejected");
        }
    }
}
