using System;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Input
{
    /// <summary>
    /// Integration tests for InputBuilder autocomplete functionality.
    /// Uses TestEnvironment for full system testing at the user experience level.
    /// </summary>
    [TestClass]
    public class InputBuilderAutoCompleteTests
    {
        // Prompt is "testhost> " which is 10 characters (entry assembly name + "> ")
        private const int PromptLength = 10;

        #region Test Commands

        [Command(Name = "help")]
        [Description("Display help information")]
        private class HelpCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "history")]
        [Description("Show command history")]
        private class HistoryCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Name = "exit")]
        [Description("Exit the application")]
        private class ExitCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        [Group]
        [Description("Server operations")]
        private class ServerGroup { }

        [Command(Group = typeof(ServerGroup), Name = "connect")]
        [Description("Connect to server")]
        private class ConnectCommand : CommandBase
        {
            [Argument]
            [Alias('n')]
            [Description("Host name")]
            public string Host { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        [Command(Group = typeof(ServerGroup), Name = "disconnect")]
        [Description("Disconnect from server")]
        private class DisconnectCommand : CommandBase
        {
            public void Execute(CommandExecutionContext ctx) { }
        }

        #endregion

        #region Helper Methods

        private TestEnvironment CreateTestEnvironment()
        {
            return new TestEnvironment(opt =>
            {
                opt.ConfigureCommands(builder =>
                {
                    builder.RegisterCommand<HelpCommand>();
                    builder.RegisterCommand<HistoryCommand>();
                    builder.RegisterCommand<ExitCommand>();
                    builder.RegisterGroup<ServerGroup>();
                    builder.RegisterCommand<ConnectCommand>();
                    builder.RegisterCommand<DisconnectCommand>();
                });
            });
        }

        /// <summary>
        /// Waits for input processing to complete.
        /// </summary>
        private async Task WaitForProcessing(int ms = 50)
        {
            await Task.Delay(ms);
        }

        /// <summary>
        /// Gets the text content of the current input line (where cursor is).
        /// </summary>
        private string GetInputLineText(TestEnvironment env)
        {
            var cursorRow = env.Console.VirtualConsole.CursorRow;
            return env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();
        }

        #endregion

        #region Ghost Text Appearance Tests

        [TestMethod]
        public async Task Type_PartialCommand_ShowsGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Act - type partial command "hel"
            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Assert - ghost text "p" should appear after "hel"
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> help", because: "ghost text 'p' should complete 'hel' to 'help'");

            // Cursor should be after what was typed ("hel"), not after ghost text
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength + 3,
                because: "cursor should be at end of typed text, not after ghost text");

            // Verify ghost text is dim
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 3, length: 1, CellAttributes.Dim,
                    because: "ghost text should be rendered with dim style");
        }

        [TestMethod]
        public async Task Type_NoMatch_NoGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Act - type something that matches nothing
            env.Keyboard.TypeText("xyz");
            await WaitForProcessing();

            // Assert - no ghost text, just what was typed
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> xyz");
        }

        [TestMethod]
        public async Task ContinuedTyping_UpdatesGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Act - type "h", should show ghost text for first match
            env.Keyboard.TypeText("h");
            await WaitForProcessing();

            var lineTextAfterH = GetInputLineText(env);
            // "h" matches "help" and "history" - "help" comes first alphabetically
            lineTextAfterH.Should().EndWith("> help");

            // Type "e" - should update ghost text
            env.Keyboard.TypeText("e");
            await WaitForProcessing();

            var lineTextAfterHe = GetInputLineText(env);
            lineTextAfterHe.Should().EndWith("> help");

            // Verify ghost text portion is still dim
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 2, length: 2, CellAttributes.Dim);
        }

        [TestMethod]
        public async Task Backspace_UpdatesGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type full word then backspace
            env.Keyboard.TypeText("help");
            await WaitForProcessing();

            // "help" is exact match - no ghost text
            var lineTextExact = GetInputLineText(env);
            lineTextExact.Should().EndWith("> help");

            // Backspace to "hel"
            env.Keyboard.PressBackspace();
            await WaitForProcessing();

            // Should now show ghost text again
            var lineTextAfterBackspace = GetInputLineText(env);
            lineTextAfterBackspace.Should().EndWith("> help", because: "ghost text 'p' should reappear");

            // Verify ghost text is dim
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 3, length: 1, CellAttributes.Dim);
        }

        #endregion

        #region Tab Key Tests

        [TestMethod]
        public async Task Tab_SingleOption_Accepts()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "hel" - only matches "help"
            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Act - press Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - should accept ghost text
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> help", because: "Tab should accept single matching option");

            // Cursor should be after accepted text (help = 4 chars) plus trailing space
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength + 5,
                because: "cursor should be after 'help ' including trailing space");

            // Verify the text is no longer dim (accepted into buffer)
            env.Console.VirtualConsole.GetCell(0, PromptLength + 3)
                .Style.Attributes.HasFlag(CellAttributes.Dim).Should().BeFalse(
                    because: "accepted text should not be dim");
        }

        [TestMethod]
        public async Task Tab_MultipleOptions_DoesNothing()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "h" - matches both "help" and "history"
            env.Keyboard.TypeText("h");
            await WaitForProcessing();

            // Verify ghost text is showing
            var lineTextBefore = GetInputLineText(env);
            lineTextBefore.Should().EndWith("> help"); // First alphabetical match

            // Act - press Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - buffer should still be "h" (Tab consumed but did nothing)
            // Ghost text should still be showing
            var lineTextAfter = GetInputLineText(env);
            lineTextAfter.Should().EndWith("> help", because: "Tab with multiple options should not accept");

            // Ghost text should still be dim
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 1, length: 3, CellAttributes.Dim);
        }

        [TestMethod]
        public async Task Tab_NoSuggestions_DoesNothing()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type something with no match
            env.Keyboard.TypeText("xyz");
            await WaitForProcessing();

            // Act - press Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - nothing changed
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> xyz");
        }

        #endregion

        #region Right Arrow Key Tests

        [TestMethod]
        public async Task RightArrow_WithGhostText_Accepts()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Act - press Right Arrow
            env.Keyboard.PressRightArrow();
            await WaitForProcessing();

            // Assert - should accept ghost text
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> help");

            // Cursor should be after accepted text (help = 4 chars) plus trailing space
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength + 5,
                because: "cursor should be after 'help ' including trailing space");

            // Verify no longer dim
            env.Console.VirtualConsole.GetCell(0, PromptLength + 3)
                .Style.Attributes.HasFlag(CellAttributes.Dim).Should().BeFalse();
        }

        [TestMethod]
        public async Task RightArrow_WithoutGhostText_MovesCursor()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            env.Keyboard.TypeText("xyz"); // No match
            await WaitForProcessing();

            // Move cursor left
            env.Keyboard.PressLeftArrow();
            await WaitForProcessing();

            var cursorBefore = env.Console.VirtualConsole.CursorColumn;

            // Act - press Right Arrow
            env.Keyboard.PressRightArrow();
            await WaitForProcessing();

            // Assert - cursor should have moved right
            var cursorAfter = env.Console.VirtualConsole.CursorColumn;
            cursorAfter.Should().Be(cursorBefore + 1);
        }

        #endregion

        #region Escape Key Tests

        [TestMethod]
        public async Task Escape_WithGhostText_Dismisses()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Verify ghost text is showing
            GetInputLineText(env).Should().EndWith("> help");

            // Act - press Escape
            env.Keyboard.PressEscape();
            await WaitForProcessing();

            // Assert - ghost text should be dismissed
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> hel", because: "Escape should dismiss ghost text");

            // Cursor should still be after "hel"
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength + 3,
                because: "cursor should remain in place after Escape");
        }

        [TestMethod]
        public async Task Escape_ThenType_SameElement_StaysSuppressed()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "he" - shows ghost "lp" (completing to "help")
            env.Keyboard.TypeText("he");
            await WaitForProcessing();
            GetInputLineText(env).Should().EndWith("> help");
            
            // Verify ghost text "lp" is showing
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 2, length: 2, CellAttributes.Dim);

            // Escape to suppress
            env.Keyboard.PressEscape();
            await WaitForProcessing();
            GetInputLineText(env).Should().EndWith("> he");

            // Act - type "l" (still same element, would show "p" ghost if not suppressed)
            env.Keyboard.TypeText("l");
            await WaitForProcessing();

            // Assert - ghost text "p" should NOT appear (suppressed)
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> hel", because: "ghost text should be suppressed for this element");

            // Verify no dim text after "hel" - the "p" ghost should NOT appear
            var cell = env.Console.VirtualConsole.GetCell(0, PromptLength + 3);
            cell.Character.Should().Be(' ',
                because: "ghost text 'p' should be suppressed until moving to new element");

            // Cursor should be right after "hel"
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength + 3);
        }

        [TestMethod]
        public async Task Escape_ThenSpace_NewElement_GhostReturns()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "server" - shows ghost text for command in group
            env.Keyboard.TypeText("server");
            await WaitForProcessing();

            // Escape to dismiss
            env.Keyboard.PressEscape();
            await WaitForProcessing();

            // Act - type space (moves to new element)
            env.Keyboard.TypeText(" ");
            await WaitForProcessing();

            // Assert - ghost text should reappear for commands in server group
            var lineText = GetInputLineText(env);
            // "connect" comes before "disconnect" alphabetically
            lineText.Should().EndWith("> server connect", because: "ghost text should reappear when moving to new element");

            // Verify ghost text is dim
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 7, length: 7, CellAttributes.Dim);
        }

        #endregion

        #region Up/Down Arrow History Tests

        [TestMethod]
        public async Task UpArrow_DismissesGhostText_ThenNavigatesHistory()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Submit a command to create history
            env.Keyboard.TypeText("help");
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            // Start typing new command with ghost text
            env.Keyboard.TypeText("exi");
            await WaitForProcessing();
            GetInputLineText(env).Should().Contain("exit"); // ghost text showing

            // Act - press Up Arrow
            env.Keyboard.PressUpArrow();
            await WaitForProcessing();

            // Assert - should show previous command from history
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> help", because: "Up arrow should navigate to previous history entry");
        }

        [TestMethod]
        public async Task DownArrow_DismissesGhostText_ThenNavigatesHistory()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Submit two commands to create history
            env.Keyboard.TypeText("help");
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            env.Keyboard.TypeText("exit");
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            // Navigate up twice to get to "help"
            env.Keyboard.TypeText("h"); // Start typing to get ghost text
            await WaitForProcessing();
            
            env.Keyboard.PressUpArrow();
            await WaitForProcessing();
            
            env.Keyboard.PressUpArrow();
            await WaitForProcessing();

            // Now at "help" in history

            // Act - press Down Arrow
            env.Keyboard.PressDownArrow();
            await WaitForProcessing();

            // Assert - should navigate forward in history
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> exit");
        }

        #endregion

        #region Enter Key Tests

        [TestMethod]
        public async Task Enter_WithGhostText_SubmitsWithoutGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type partial command with ghost text
            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Verify ghost text is showing
            GetInputLineText(env).Should().EndWith("> help");

            // Act - press Enter (submits without accepting ghost text)
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            // Assert - command "hel" was submitted (not "help")
            // This will result in an error since "hel" is not a valid command
            // We should see an error message containing "hel" not found
            var screenContent = env.Console.VirtualConsole.GetScreenContent();
            screenContent.Should().Contain("hel",
                because: "the raw buffer 'hel' should be submitted, not the ghost text 'help'");
            screenContent.Should().Contain("not found",
                because: "'hel' is not a valid command and should produce an error");
        }

        #endregion

        #region Bug Fix Tests

        [TestMethod]
        public async Task Backspace_ToEmptyLine_ClearsGhostText()
        {
            // Bug #1: When backspacing to empty, residual ghost text remains
            // GIVEN: A command is registered "server connect"
            // WHEN: I type "serv" - ghost text appears
            // WHEN: I backspace 4 times to clear out the text
            // THEN: As soon as I clear the final character, line should be empty with no ghost text

            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "serv" - ghost text "er" should appear (completing to "server")
            env.Keyboard.TypeText("serv");
            await WaitForProcessing();

            // Verify ghost text is showing
            GetInputLineText(env).Should().EndWith("> server",
                because: "ghost text 'er' should complete 'serv' to 'server'");

            // Backspace 4 times to clear "serv"
            for (int i = 0; i < 4; i++)
            {
                env.Keyboard.PressBackspace();
                await WaitForProcessing();
            }

            // Assert - line should be completely empty (just the prompt)
            var lineText = GetInputLineText(env);
            lineText.Should().Be("testhost>",
                because: "after backspacing all typed text, line should be empty with no ghost text");

            // Cursor should be right after the prompt
            env.Console.VirtualConsole.CursorColumn.Should().Be(PromptLength,
                because: "cursor should be at prompt end with no ghost text");
        }

        [TestMethod]
        public async Task Enter_ClearsSuppressionForNextInput()
        {
            // Bug #2: Suppression persists between input line submissions
            // GIVEN: A command is registered "server connect"
            // WHEN: I type "serv" - ghost text appears
            // WHEN: I press ESC - ghost text is suppressed
            // WHEN: I press Enter - line is submitted
            // WHEN: I begin typing again on new prompt
            // THEN: Ghost text should appear (suppression should be cleared)

            using var env = CreateTestEnvironment();
            await using var run = env.Start();

            // Type "serv" - ghost text "er" should appear
            env.Keyboard.TypeText("serv");
            await WaitForProcessing();

            // Verify ghost text is showing
            GetInputLineText(env).Should().EndWith("> server");

            // Press Escape to suppress
            env.Keyboard.PressEscape();
            await WaitForProcessing();

            // Verify ghost text is hidden (suppressed)
            var afterEscape = GetInputLineText(env);
            afterEscape.Should().EndWith("> serv",
                because: "ghost text should be suppressed after Escape");

            // Press Enter to submit (will produce an error, that's fine)
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            // Now on a new prompt, type "hel" - ghost text should appear
            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Assert - ghost text should appear (suppression should have been cleared)
            var newLineText = GetInputLineText(env);
            newLineText.Should().EndWith("> help",
                because: "suppression should be cleared after Enter, so ghost text 'p' should appear for 'hel'");
        }

        #endregion
    }
}
