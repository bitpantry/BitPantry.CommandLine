using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests
{
    /// <summary>
    /// Reproduction test for: Spectre.Console Confirm() prompt hangs over SignalR remote CLI
    /// when keystrokes are sent AFTER the prompt renders (the normal interactive scenario).
    /// 
    /// The existing IntegrationTests_ProcessGateDeadlock test pre-pushes keys BEFORE the
    /// command is submitted, so ReadKeyAsync() finds a response already waiting. This test
    /// instead waits for the confirmation prompt to render, then sends keystrokes — matching
    /// the real interactive usage pattern where the user types after seeing the prompt.
    /// </summary>
    [TestClass]
    public class IntegrationTests_InteractivePromptHang
    {
        /// <summary>
        /// Test Validity Check:
        ///   Invokes code under test: YES (runs rm command that triggers ReadKey RPC via confirmation prompt)
        ///   Breakage detection: YES (hang causes test timeout; working code completes within timeout)
        ///   Not a tautology: YES (exercises interactive ReadKey flow where keys arrive after prompt)
        ///
        /// This test reproduces the reported bug:
        /// 1. Client sends rm command to server (5 files triggers confirmation)
        /// 2. Server renders confirmation prompt and sends ReadKey RPC to client
        /// 3. Client's ReceiveRequest handler calls ReadKey() which BLOCKS waiting for input
        /// 4. Test waits for the prompt text to appear in the console
        /// 5. THEN pushes 'n' + Enter keystokes (simulating interactive user typing)
        /// 6. The ReadKey() call should unblock and return the key to the server
        ///
        /// If the bug is present, step 6 never happens — ReadKey blocks forever.
        /// </summary>
        [TestMethod]
        public async Task Run_RemoteConfirmPrompt_InteractiveKeysAfterPrompt_CompletesWithoutHang()
        {
            using var env = TestEnvironment.WithServer();
            await env.ConnectToServerAsync();

            // Create 5 files to trigger confirmation prompt in rm command
            var testFolder = Path.Combine(env.RemoteFileSystem.ServerStorageRoot, "interactive-test");
            Directory.CreateDirectory(testFolder);
            for (int i = 1; i <= 5; i++)
            {
                File.WriteAllText(Path.Combine(testFolder, $"file{i}.txt"), $"content{i}");
            }

            // Submit the rm command — this will trigger a confirmation prompt on the server
            // Do NOT pre-push keys. We want to simulate interactive usage.
            await env.Keyboard.TypeTextAsync("server rm interactive-test/*.txt");
            
            // Press Enter to submit the command. Use PushKey (fire-and-forget) because
            // PressEnterAsync would wait for the key to be "processed" by the input loop,
            // but the input loop won't return to the prompt until the command completes —
            // which requires us to answer the confirmation prompt first.
            env.Input.PushKey(ConsoleKey.Enter);

            // Wait for the confirmation prompt to appear in the console output.
            // The rm command should render something like "Delete 5 files? [y/n]"
            var promptAppeared = await WaitForConsoleText(env, "Delete", timeoutMs: 5000);
            promptAppeared.Should().BeTrue(
                "The confirmation prompt should appear before we type our response. " +
                "If this fails, the command may not have reached the prompt stage.");

            // NOW push keystrokes to answer the prompt — this is the interactive scenario
            env.Input.PushKey(ConsoleKey.N);
            env.Input.PushKey(ConsoleKey.Enter);

            // Wait for the command to complete (prompt should reappear)
            var commandCompleted = await WaitForPromptReady(env, timeoutMs: 5000);

            // CRITICAL ASSERTION: If the bug is present, this times out
            commandCompleted.Should().BeTrue(
                "Command should complete after answering the confirmation prompt interactively. " +
                "A timeout here reproduces the reported bug: keystrokes sent after the prompt " +
                "renders are never received by the server-side ReadKeyAsync().");

            // Verify files still exist (we denied with 'n')
            var remainingFiles = Directory.GetFiles(testFolder, "*.txt");
            remainingFiles.Should().HaveCount(5,
                "Files should remain because we denied the confirmation.");
        }

        /// <summary>
        /// Waits for specific text to appear anywhere in the console output.
        /// </summary>
        private static async Task<bool> WaitForConsoleText(TestEnvironment env, string text, int timeoutMs = 3000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var consoleText = env.Console.GetScreenContent();
                if (consoleText.Contains(text, StringComparison.OrdinalIgnoreCase))
                    return true;
                await Task.Delay(50);
            }
            return false;
        }

        /// <summary>
        /// Waits for the CLI prompt to reappear (indicating command completion).
        /// </summary>
        private static async Task<bool> WaitForPromptReady(TestEnvironment env, int timeoutMs = 3000)
        {
            // First, small delay to let any in-flight output settle
            await Task.Delay(100);
            
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var cursorRow = env.Console.VirtualConsole.CursorRow;
                var lineText = env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();
                if (lineText.EndsWith("> ") || lineText.EndsWith(">"))
                    return true;
                await Task.Delay(50);
            }
            return false;
        }
    }
}
