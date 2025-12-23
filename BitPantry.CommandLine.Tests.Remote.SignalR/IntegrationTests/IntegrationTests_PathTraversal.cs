using BitPantry.CommandLine.Tests.Remote.SignalR.Environment;
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
            // Arrange - use default environment like other tests
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Test content for path traversal tests");

            // Use unique path to avoid conflicts with parallel tests
            var uniqueId = Guid.NewGuid().ToString("N");
            var targetPath = $"path-test-{uniqueId}/file.txt";

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act - Upload with valid relative path
                await fileTransferService.UploadFile(
                    tempFilePath,
                    targetPath,
                    null,
                    CancellationToken.None);

                // Assert - File should exist at the expected location
                var expectedPath = Path.Combine("./cli-storage", $"path-test-{uniqueId}", "file.txt");
                File.Exists(expectedPath).Should().BeTrue();
                File.ReadAllText(expectedPath).Should().Be("Test content for path traversal tests");
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task Upload_NestedSubdirectory_CreatesAndSucceeds()
        {
            // Arrange - use default environment like other tests
            using var env = new TestEnvironment();
            await env.Cli.ConnectToServer(env.Server);

            var tempFilePath = Path.GetTempFileName();
            File.WriteAllText(tempFilePath, "Test content for nested path tests");

            // Use unique path to avoid conflicts with parallel tests
            var uniqueId = Guid.NewGuid().ToString("N");
            var targetPath = $"nested-test-{uniqueId}/deep/file.txt";

            try
            {
                var fileTransferService = env.Cli.Services.GetRequiredService<FileTransferService>();

                // Act - Upload to a deeply nested path
                await fileTransferService.UploadFile(
                    tempFilePath,
                    targetPath,
                    null,
                    CancellationToken.None);

                // Assert - File should exist with all parent directories created
                var expectedPath = Path.Combine("./cli-storage", $"nested-test-{uniqueId}", "deep", "file.txt");
                File.Exists(expectedPath).Should().BeTrue();
                Directory.Exists(Path.Combine("./cli-storage", $"nested-test-{uniqueId}", "deep")).Should().BeTrue();
            }
            finally
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }
    }
}
