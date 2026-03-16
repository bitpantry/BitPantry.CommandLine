using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_CatCommand
    {
        // T128 DF-053: End-to-end: file content visible in VirtualConsole
        [TestMethod]
        public async Task CatCommand_EndToEnd_FileContentVisible()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var prefix = env.RemoteFileSystem.ServerTestFolderPrefix;
            var fullPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, prefix, "hello.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            System.IO.File.WriteAllText(fullPath, "hello world");

            var result = await env.RunCommandAsync($"server cat {prefix}/hello.txt");

            result.ResultCode.Should().Be(0);
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("hello world", "file content should be visible in console output");
        }
    }
}
