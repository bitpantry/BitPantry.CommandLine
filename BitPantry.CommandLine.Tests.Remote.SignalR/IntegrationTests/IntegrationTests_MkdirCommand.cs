using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_MkdirCommand
    {
        // T040 CV-036: --parents flag works in integration (originally -p alias, but 'p' is reserved by --profile)
        [TestMethod]
        public async Task MkdirCommand_WithParentsFlag_CreatesDeepDirectory()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var deepPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/a/b/c";
            var result = await env.RunCommandAsync($"server mkdir {deepPath} --parents");

            result.ResultCode.Should().Be(0);
            var fullPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "a", "b", "c");
            System.IO.Directory.Exists(fullPath).Should().BeTrue("deep directory should have been created via --parents");
        }

        // T045 DF-049: End-to-end: directory exists on disk after command
        [TestMethod]
        public async Task MkdirCommand_EndToEnd_DirectoryExistsOnDisk()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Ensure test folder exists on server
            System.IO.Directory.CreateDirectory(env.RemoteFileSystem.ServerTestDir);

            var newDirPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/newdir";
            var result = await env.RunCommandAsync($"server mkdir {newDirPath}");

            result.ResultCode.Should().Be(0);
            var fullPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "newdir");
            System.IO.Directory.Exists(fullPath).Should().BeTrue("directory should exist on disk after mkdir");
        }
    }
}
