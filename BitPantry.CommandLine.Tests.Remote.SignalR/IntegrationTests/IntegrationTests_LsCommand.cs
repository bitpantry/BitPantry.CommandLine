using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    [TestClass]
    public class IntegrationTests_LsCommand
    {
        // T019 DF-048: End-to-end: files in tempDir appear after connect
        [TestMethod]
        public async Task LsCommand_FilesInStorageRoot_AppearInOutput()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Plant report.txt in test directory on server
            env.RemoteFileSystem.CreateServerFile("report.txt", "quarterly report");

            // Execute ls targeting test folder
            var result = await env.RunCommandAsync($"server ls {env.RemoteFileSystem.ServerTestFolderPrefix}");

            result.ResultCode.Should().Be(0);
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("report.txt");
        }

        // T020 DF-055: Server commands appear after connect
        [TestMethod]
        public async Task LsCommand_AfterConnect_ResolvesAsKnownCommand()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Run server ls with default path — should resolve and not error as unknown command
            var result = await env.RunCommandAsync("server ls");

            result.ResultCode.Should().Be(0);
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().NotContain("not found", "ls should be a recognized command");
            consoleOutput.Should().NotContain("Unknown command", "ls should be a recognized command");
        }

        // T024 EH-029: Path not found returns error (not exception)
        [TestMethod]
        public async Task LsCommand_NonexistentPath_ReturnsErrorNotException()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            var result = await env.RunCommandAsync("server ls /nosuchdir");

            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().Contain("not found", "should display error message");
            consoleOutput.Should().NotContain("Exception", "should not show stack trace");
            consoleOutput.Should().NotContain("StackTrace", "should not show stack trace");
        }

        // T037 UX-026: End-to-end output visible in VirtualConsole
        [TestMethod]
        public async Task LsCommand_RealFileInTempDir_FilenameVisibleInVirtualConsole()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            env.RemoteFileSystem.CreateServerFile("inventory.dat", "test content");

            await env.RunCommandAsync($"server ls {env.RemoteFileSystem.ServerTestFolderPrefix}");

            env.Console.VirtualConsole.Should().ContainText("inventory.dat");
        }

        // T155 EH-031: Command invoked while disconnected returns not-connected message
        [TestMethod]
        public async Task LsCommand_Disconnected_ShowsNotConnectedOrNotFound()
        {
            using var env = TestEnvironment.WithServer();
            // Deliberately do NOT connect — server commands not registered

            var result = await env.RunCommandAsync("server ls");

            var consoleOutput = string.Concat(env.Console.Lines);
            // Without connection, server commands are not registered, so framework shows "not found"
            consoleOutput.Should().Contain("not found",
                "should indicate server commands are unavailable when disconnected");
        }

        // Bug fix: server ls on fresh server with no uploaded files should show empty listing, not error
        [TestMethod]
        public async Task LsCommand_EmptyServer_ReturnsEmptyListing()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Run server ls with default path (.) on a fresh server — no files uploaded
            var result = await env.RunCommandAsync("server ls");

            // Should succeed (not produce an error)
            result.ResultCode.Should().Be(0);
            
            // Should NOT show "Directory not found" error
            var consoleOutput = string.Concat(env.Console.Lines);
            consoleOutput.Should().NotContain("Directory not found",
                "empty server should not produce 'Directory not found' error");
            consoleOutput.Should().NotContain("not found",
                "empty server should show empty listing, not error");
        }
    }
}
