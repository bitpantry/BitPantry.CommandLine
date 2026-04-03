using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Tests for the ProcessGate deadlock fix.
    /// 
    /// The deadlock was caused by ReceiveRequest() trying to acquire the ProcessGate
    /// lock while Run() already held it. When a server command uses interactive input
    /// (ReadKey, ConfirmationPrompt, etc.), the server sends a ReadKey RPC to the client.
    /// The client's ReceiveRequest() handles this RPC, but if it tried to acquire
    /// the ProcessGate lock (which Run() already holds), it would deadlock.
    /// 
    /// The fix removes the lock acquisition from ReceiveRequest() since input RPCs
    /// are inherently scoped to an active Run() that already holds the lock.
    /// </summary>
    [TestClass]
    public class IntegrationTests_ProcessGateDeadlock
    {
        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: YES (runs rm command that triggers ReadKey RPC via confirmation prompt)
        ///   Breakage detection: YES (deadlock causes test timeout; fix allows completion)
        ///   Not a tautology: YES (exercises Run->ReceiveRequest->ReadKey flow)
        ///
        /// This test exercises the deadlock-prone code path:
        /// 1. Client sends command to server
        /// 2. Server executes rm command with glob matching 4+ files
        /// 3. Server calls Console.Prompt(ConfirmationPrompt) which internally uses ReadKey
        /// 4. Server sends ReadKey RPC back to client
        /// 5. Client's ReceiveRequest handles the RPC
        /// 6. WITHOUT FIX: ReceiveRequest tries to acquire ProcessGate - DEADLOCK
        /// 7. WITH FIX: ReceiveRequest handles ReadKey without lock - sends 'n' response
        /// 
        /// We pre-push 'n' + Enter to cancel the prompt (simpler than trying to confirm).
        /// The test verifies the command completes without deadlock, even if files aren't deleted.
        /// 
        /// RED (before fix): Test times out because ReadKey RPC deadlocks
        /// GREEN (after fix): Test completes (command receives 'n', files remain)
        /// </summary>
        [TestMethod]
        public async Task Run_RemoteCommandWithConfirmPrompt_CompletesWithoutDeadlock()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create 4+ files to trigger confirmation prompt in rm command
            // (RmCommand.ConfirmationThreshold = 4)
            var testFolder = Path.Combine(env.RemoteFileSystem.ServerStorageRoot, "deadlock-test");
            Directory.CreateDirectory(testFolder);
            for (int i = 1; i <= 5; i++)
            {
                File.WriteAllText(Path.Combine(testFolder, $"file{i}.txt"), $"content{i}");
            }

            // Type the rm command (without --force so it prompts for confirmation)
            await env.Keyboard.TypeTextAsync("server rm deadlock-test/*.txt");
            
            // Pre-push 'n' + Enter to deny the confirmation when prompted
            // These keys will be consumed by the ReadKey RPC handler in ReceiveRequest
            env.Input.PushKey(ConsoleKey.N);
            env.Input.PushKey(ConsoleKey.Enter);
            
            // Submit the command
            // This exercises the full path:
            // - Run() acquires ProcessGate and sends command to server
            // - Server matches 5 files, calls ConfirmationPrompt
            // - Server sends ReadKey RPC to client  
            // - ReceiveRequest handles RPC (without acquiring lock - that's the fix)
            // - ReadKey gets 'n' from queue, returns to server
            // - Server cancels deletion
            var commandTask = env.Keyboard.PressEnterAsync();

            // Wait for command with timeout
            // CRITICAL ASSERTION: If the deadlock exists, this times out after 5 seconds.
            // If the fix is working, the command completes quickly (even if confirmation is denied).
            var completedTask = await Task.WhenAny(commandTask, Task.Delay(5000));

            // Verify no deadlock occurred
            completedTask.Should().Be(commandTask, 
                "Command should complete without deadlock. " +
                "A timeout here indicates the deadlock is present - ReceiveRequest is blocked " +
                "waiting for ProcessGate that Run() holds.");

            // Verify files still exist (we denied the confirmation with 'n')
            var remainingFiles = Directory.GetFiles(testFolder, "*.txt");
            remainingFiles.Should().HaveCount(5, 
                "Files should remain because we denied confirmation. " +
                "(If files were deleted, the confirmation prompt didn't work correctly, " +
                "but the absence of deadlock is the primary verification.)");
        }
    }
}
