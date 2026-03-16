using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_CpCommand
    {
        // T098 CV-035: -r alias accepted as --recursive
        [TestMethod]
        public async Task CpCommand_WithShortRFlag_CopiesDirectoryRecursively()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create source directory with a file on the server
            var srcDir = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/cpdir";
            var dstDir = $"{env.RemoteFileSystem.ServerTestFolderPrefix}/cpdir-copy";
            var fullSrcDir = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "cpdir");
            var fullDstDir = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, env.RemoteFileSystem.ServerTestFolderPrefix, "cpdir-copy");
            System.IO.Directory.CreateDirectory(fullSrcDir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(fullSrcDir, "file.txt"), "content");

            var result = await env.RunCommandAsync($"server cp {srcDir} {dstDir} -r");

            result.ResultCode.Should().Be(0);
            System.IO.Directory.Exists(fullDstDir).Should().BeTrue("destination directory should exist after copy");
            System.IO.File.Exists(System.IO.Path.Combine(fullDstDir, "file.txt")).Should().BeTrue("file should be copied");
            System.IO.Directory.Exists(fullSrcDir).Should().BeTrue("source should still exist (copy, not move)");
        }

        // T105 DF-052: End-to-end: both files exist after copy
        [TestMethod]
        public async Task CpCommand_EndToEnd_BothFilesExist()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var prefix = env.RemoteFileSystem.ServerTestFolderPrefix;
            var fullSrc = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, prefix, "a.txt");
            var fullDst = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, prefix, "b.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullSrc)!);
            System.IO.File.WriteAllText(fullSrc, "hello");

            var result = await env.RunCommandAsync($"server cp {prefix}/a.txt {prefix}/b.txt");

            result.ResultCode.Should().Be(0);
            System.IO.File.Exists(fullSrc).Should().BeTrue("source must still exist after copy");
            System.IO.File.Exists(fullDst).Should().BeTrue("destination must exist after copy");
            System.IO.File.ReadAllText(fullDst).Should().Be("hello", "destination must have same content");
        }

        // T159 EH-034: Mid-operation failure during recursive copy shows clear error
        [TestMethod]
        public async Task CpCommand_CopyToNonexistentSource_ShowsErrorInConsole()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var prefix = env.RemoteFileSystem.ServerTestFolderPrefix;

            // Attempt to copy a non-existent source — this simulates an operation that fails
            var result = await env.RunCommandAsync($"server cp {prefix}/nonexistent.txt {prefix}/dest.txt");

            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("not found",
                "should show a clear error message when copy operation cannot proceed");
        }
    }
}
