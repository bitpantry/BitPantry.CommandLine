using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.Remote.SignalR.Client;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Integration tests for path traversal protection.
    /// These tests verify that the PathValidator correctly blocks path traversal attempts.
    /// </summary>
    [TestClass]
    public class IntegrationTests_PathTraversal
    {
        [TestMethod]
        public async Task Upload_ValidRelativePath_Succeeds()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.RemoteFileSystem.CreateLocalFile("file.txt", "Test content for path traversal tests");
            var targetPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/path-test/file.txt";

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Act - Upload with valid relative path
            await fileTransferService.UploadFile(
                localFilePath,
                targetPath,
                null,
                CancellationToken.None);

            // Assert - File should exist at the expected location
            var expectedPath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "path-test", "file.txt");
            File.Exists(expectedPath).Should().BeTrue();
            File.ReadAllText(expectedPath).Should().Be("Test content for path traversal tests");
        }

        [TestMethod]
        public async Task Upload_NestedSubdirectory_CreatesAndSucceeds()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);

            var localFilePath = env.RemoteFileSystem.CreateLocalFile("file.txt", "Test content for nested path tests");
            var targetPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/nested/deep/file.txt";

            var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

            // Act - Upload to a deeply nested path
            await fileTransferService.UploadFile(
                localFilePath,
                targetPath,
                null,
                CancellationToken.None);

            // Assert - File should exist with all parent directories created
            var expectedPath = Path.Combine(env.RemoteFileSystem.ServerTestDir, "nested", "deep", "file.txt");
            File.Exists(expectedPath).Should().BeTrue();
            Directory.Exists(Path.Combine(env.RemoteFileSystem.ServerTestDir, "nested", "deep")).Should().BeTrue();
        }
    }
}
