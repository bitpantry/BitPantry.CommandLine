using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_CommandHelp
    {
        // T154 UX-029: Command help text reflects remote file system syntax
        [TestMethod]
        public async Task LsCommand_Help_ShowsExpectedArgumentsAndFlags()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            await env.RunCommandAsync("server ls --help");

            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("path", "ls help should reference the path argument");
            consoleOutput.Should().Contain("long", "ls help should reference the --long flag");
        }

        // T154 UX-029 (continued): rm command help
        [TestMethod]
        public async Task RmCommand_Help_ShowsExpectedArgumentsAndFlags()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            await env.RunCommandAsync("server rm --help");

            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("path", "rm help should reference the path argument");
            consoleOutput.Should().Contain("recursive", "rm help should reference the --recursive flag");
        }
    }
}
