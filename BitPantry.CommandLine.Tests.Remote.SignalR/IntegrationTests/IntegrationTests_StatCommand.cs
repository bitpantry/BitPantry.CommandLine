using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_StatCommand
    {
        // T147 DF-054: End-to-end: stat output visible
        [TestMethod]
        public async Task StatCommand_EndToEnd_OutputContainsFileName()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var prefix = env.RemoteFileSystem.ServerTestFolderPrefix;
            var fullPath = System.IO.Path.Combine(env.RemoteFileSystem.ServerStorageRoot, prefix, "report.txt");
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(fullPath)!);
            System.IO.File.WriteAllText(fullPath, "quarterly report data");

            var result = await env.RunCommandAsync($"server stat {prefix}/report.txt");

            result.ResultCode.Should().Be(0);
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("report.txt", "stat output should contain the file name");
        }
    }
}
