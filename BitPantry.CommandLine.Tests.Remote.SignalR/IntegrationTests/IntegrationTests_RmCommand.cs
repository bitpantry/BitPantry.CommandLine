using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_RmCommand
    {
        // T056 CV-034: -r alias accepted as --recursive
        [TestMethod]
        public async Task RmCommand_WithShortRFlag_DeletesNonEmptyDirectory()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create a non-empty directory on the server
            var dirPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/rmdir";
            var fullDirPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "rmdir");
            System.IO.Directory.CreateDirectory(fullDirPath);
            System.IO.File.WriteAllText(System.IO.Path.Combine(fullDirPath, "file.txt"), "content");

            var result = await env.RunCommandAsync($"server rm {dirPath} -r");

            result.ResultCode.Should().Be(0);
            System.IO.Directory.Exists(fullDirPath).Should().BeFalse("non-empty directory should be deleted with -r alias");
        }

        // T067 DF-050: End-to-end: file gone after command
        [TestMethod]
        public async Task RmCommand_FileDeletedEndToEnd()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Plant a file on the server
            var filePath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/rmtarget.txt";
            var fullFilePath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "rmtarget.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullFilePath));
            System.IO.File.WriteAllText(fullFilePath, "content to delete");
            System.IO.File.Exists(fullFilePath).Should().BeTrue("file should exist before rm");

            var result = await env.RunCommandAsync($"server rm {filePath}");

            result.ResultCode.Should().Be(0);
            System.IO.File.Exists(fullFilePath).Should().BeFalse("file should be gone after server rm");
        }
    }
}
