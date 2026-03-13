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
    }
}
