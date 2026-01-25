using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.IO.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Tests that verify the client ALWAYS uses a local FileSystem implementation,
    /// regardless of connection state. The client never swaps IFileSystem on connect/disconnect.
    /// </summary>
    [TestClass]
    public class IntegrationTests_ClientFileSystem
    {
        [TestMethod]
        public void IFileSystem_LocalExecution_IsFileSystemType()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();

            // Act
            var fileSystem = env.Cli.Services.GetRequiredService<IFileSystem>();

            // Assert
            fileSystem.Should().BeOfType<FileSystem>(
                "client should always use the concrete FileSystem type for local operations");
        }

        [TestMethod]
        public void IFileSystem_LocalExecution_HasUnrestrictedAccess()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            var fileSystem = env.Cli.Services.GetRequiredService<IFileSystem>();
            var tempFilePath = Path.GetTempFileName();

            try
            {
                // Act - Write to a temp file (outside any sandbox)
                fileSystem.File.WriteAllText(tempFilePath, "unrestricted test content");
                var content = fileSystem.File.ReadAllText(tempFilePath);

                // Assert
                content.Should().Be("unrestricted test content",
                    "client file system should have unrestricted access to the local file system");
            }
            finally
            {
                // Cleanup
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);
            }
        }

        [TestMethod]
        public async Task IFileSystem_AfterConnect_StillIsFileSystemType()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            var fileSystemBefore = env.Cli.Services.GetRequiredService<IFileSystem>();

            // Act - Connect to the server
            await env.Cli.ConnectToServer(env.Server);

            // Get IFileSystem after connection
            var fileSystemAfter = env.Cli.Services.GetRequiredService<IFileSystem>();

            // Assert - Client should NEVER swap to a different IFileSystem implementation
            fileSystemAfter.Should().BeOfType<FileSystem>(
                "client should still use the concrete FileSystem type after connecting to server");
            fileSystemAfter.Should().BeSameAs(fileSystemBefore,
                "client should use the exact same IFileSystem instance after connecting (no swap)");
        }

        [TestMethod]
        public async Task IFileSystem_AfterDisconnect_StillIsFileSystemType()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            await env.Cli.ConnectToServer(env.Server);
            var fileSystemWhileConnected = env.Cli.Services.GetRequiredService<IFileSystem>();

            // Act - Disconnect from the server
            var serverProxy = env.Cli.Services.GetRequiredService<IServerProxy>();
            await serverProxy.Disconnect();

            // Get IFileSystem after disconnection
            var fileSystemAfterDisconnect = env.Cli.Services.GetRequiredService<IFileSystem>();

            // Assert - Client should NEVER swap IFileSystem implementation
            fileSystemAfterDisconnect.Should().BeOfType<FileSystem>(
                "client should still use the concrete FileSystem type after disconnecting from server");
            fileSystemAfterDisconnect.Should().BeSameAs(fileSystemWhileConnected,
                "client should use the exact same IFileSystem instance after disconnecting (no swap)");
        }

        [TestMethod]
        public void Command_InjectsIFileSystem_CanReadWriteLocally()
        {
            // Arrange
            using var env = TestEnvironment.WithServer();
            var fileSystem = env.Cli.Services.GetRequiredService<IFileSystem>();
            var testDir = Path.Combine(Path.GetTempPath(), $"cmdline-test-{Guid.NewGuid()}");
            var testFile = Path.Combine(testDir, "test.txt");

            try
            {
                // Act - Create directory and file using IFileSystem
                fileSystem.Directory.CreateDirectory(testDir);
                fileSystem.File.WriteAllText(testFile, "test content from command");

                // Assert
                fileSystem.Directory.Exists(testDir).Should().BeTrue(
                    "directory should be created locally");
                fileSystem.File.Exists(testFile).Should().BeTrue(
                    "file should be created locally");
                fileSystem.File.ReadAllText(testFile).Should().Be("test content from command",
                    "file content should be readable locally");
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(testDir))
                    Directory.Delete(testDir, recursive: true);
            }
        }
    }
}
