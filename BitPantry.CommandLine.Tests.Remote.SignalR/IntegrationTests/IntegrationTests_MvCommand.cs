using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_MvCommand
    {
        // T080 CV-037: -f alias accepted as --force
        [TestMethod]
        public async Task MvCommand_WithShortFFlag_OverwritesExistingFile()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create source and destination files on server
            var srcPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/mv-src.txt";
            var dstPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/mv-dst.txt";
            var fullSrcPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "mv-src.txt");
            var fullDstPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "mv-dst.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullSrcPath));
            System.IO.File.WriteAllText(fullSrcPath, "new content");
            System.IO.File.WriteAllText(fullDstPath, "old content");

            var result = await env.RunCommandAsync($"server mv {srcPath} {dstPath} -f");

            result.ResultCode.Should().Be(0);
            System.IO.File.Exists(fullSrcPath).Should().BeFalse("source should be gone after move");
            System.IO.File.Exists(fullDstPath).Should().BeTrue("destination should exist");
            System.IO.File.ReadAllText(fullDstPath).Should().Be("new content", "-f alias should overwrite destination like --force");
        }

        // T087 DF-051: End-to-end: file at new location
        [TestMethod]
        public async Task MvCommand_EndToEnd_FileMovesToNewLocation()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var srcPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/a.txt";
            var dstPath = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/b.txt";
            var fullSrcPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "a.txt");
            var fullDstPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "b.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullSrcPath));
            System.IO.File.WriteAllText(fullSrcPath, "hello world");

            var result = await env.RunCommandAsync($"server mv {srcPath} {dstPath}");

            result.ResultCode.Should().Be(0);
            System.IO.File.Exists(fullDstPath).Should().BeTrue("b.txt should exist after move");
            System.IO.File.Exists(fullSrcPath).Should().BeFalse("a.txt should be gone after move");
            System.IO.File.ReadAllText(fullDstPath).Should().Be("hello world");
        }
    }
}
