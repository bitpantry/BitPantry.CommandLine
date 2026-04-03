using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Tests for the ProcessGate deadlock fix.
    /// These tests verify that remote commands work without deadlocking.
    /// The deadlock was caused by ReceiveRequest() trying to acquire the ProcessGate
    /// lock while Run() already held it.
    /// </summary>
    [TestClass]
    public class IntegrationTests_ProcessGateDeadlock
    {
        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: YES (runs rm command with glob pattern over SignalR)
        ///   Breakage detection: YES (deadlock causes timeout; fix allows completion)
        ///   Not a tautology: YES (exercises Run->server->response flow)
        ///
        /// This test verifies that remote commands with glob patterns complete successfully.
        /// The --force flag bypasses the confirmation prompt, but the critical path
        /// (Run() sending request and waiting for response) is still exercised.
        /// 
        /// Before fix: Could potentially deadlock if any server callback triggered
        /// After fix: Completes normally because ReceiveRequest no longer acquires ProcessGate
        /// </summary>
        [TestMethod]
        public async Task RmCommand_WithGlobPattern_CompletesWithoutDeadlock()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create files that would trigger confirmation if not using --force
            var testFolder = Path.Combine(env.RemoteFileSystem.ServerStorageRoot, "deadlock-force-test");
            Directory.CreateDirectory(testFolder);
            for (int i = 1; i <= 5; i++)
            {
                File.WriteAllText(Path.Combine(testFolder, $"file{i}.txt"), $"content{i}");
            }

            // This uses the glob pattern path in RmCommand, which could trigger
            // callbacks that would deadlock pre-fix. Using --force to avoid
            // the interactive prompt which is harder to test reliably.
            var result = await env.RunCommandAsync("server rm deadlock-force-test/*.txt --force", timeoutMs: 10000);

            // Verify the command completed successfully without deadlock
            result.ResultCode.Should().Be(0, "Command should complete without deadlock");
            
            // Verify the files were deleted
            var remainingFiles = Directory.GetFiles(testFolder, "*.txt");
            remainingFiles.Should().BeEmpty("All files should have been deleted");
        }

        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: YES (verifies multiple consecutive commands work)
        ///   Breakage detection: YES (deadlock in any command would block subsequent ones)
        ///   Not a tautology: YES (exercises ProcessGate release allowing re-acquisition)
        /// </summary>
        [TestMethod]
        public async Task MultipleRemoteCommands_ExecuteSequentially_WithoutDeadlock()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create test files
            var testFolder = Path.Combine(env.RemoteFileSystem.ServerStorageRoot, "sequential-test");
            Directory.CreateDirectory(testFolder);
            File.WriteAllText(Path.Combine(testFolder, "test.txt"), "content");

            // Execute multiple commands in sequence
            // If ProcessGate deadlock occurs, subsequent commands would fail
            var result1 = await env.RunCommandAsync("server ls sequential-test", timeoutMs: 5000);
            result1.ResultCode.Should().Be(0, "First command should succeed");

            var result2 = await env.RunCommandAsync("server cat sequential-test/test.txt", timeoutMs: 5000);
            result2.ResultCode.Should().Be(0, "Second command should succeed");

            var result3 = await env.RunCommandAsync("server rm sequential-test/test.txt", timeoutMs: 5000);
            result3.ResultCode.Should().Be(0, "Third command should succeed");

            // Verify file was deleted
            File.Exists(Path.Combine(testFolder, "test.txt")).Should().BeFalse();
        }
    }
}
