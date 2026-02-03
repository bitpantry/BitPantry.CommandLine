using System;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
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
                    builder.RegisterGroup(typeof(ServerGroup));
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

            // Act - type something that matches nothing
            env.Keyboard.TypeText("xyz");
            await WaitForProcessing();

            // Assert - no ghost text, just what was typed
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("> xyz");
        }

        /// <summary>
        /// Implements: 008:UX-010
        /// Given: Ghost text visible
        /// When: User types a character
        /// Then: Character is inserted, ghost text updates to new first match
        /// </summary>
        [TestMethod]
        public async Task ContinuedTyping_UpdatesGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();

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

        /// <summary>
        /// Implements: 008:UX-025
        /// Given: Menu is open with filter text
        /// When: User presses Backspace
        /// Then: Last character removed, menu re-filters to show more options
        /// </summary>
        [TestMethod]
        public async Task Backspace_UpdatesGhostText()
        {
            // Arrange
            using var env = CreateTestEnvironment();

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
        public async Task Tab_MultipleOptions_OpensMenu()
        {
            // Arrange
            using var env = CreateTestEnvironment();

            // Type "h" - matches both "help" and "history"
            env.Keyboard.TypeText("h");
            await WaitForProcessing(100); // Longer wait for ghost text

            // Act - press Tab to open menu
            env.Keyboard.PressTab();
            await WaitForProcessing(100);

            // Assert - menu should be displayed below input line
            // The menu should show options starting with "h"
            var row1Text = env.Console.VirtualConsole.GetRow(1).GetText();
            (row1Text.Contains("help") || row1Text.Contains("history")).Should().BeTrue(
                because: "menu should display matching options");
        }

        [TestMethod]
        public async Task Tab_NoSuggestions_DoesNothing()
        {
            // Arrange
            using var env = CreateTestEnvironment();

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

        [TestMethod]
        public async Task Enter_WithGhostText_ClearsGhostTextFromDisplay()
        {
            // Bug: When Enter is pressed with ghost text visible, the ghost text
            // should be cleared from the display before the command is submitted.
            // This ensures the input line shows only what the user typed, not the
            // ghost text suggestion.

            // Arrange
            using var env = CreateTestEnvironment();

            // Type partial command with ghost text
            env.Keyboard.TypeText("hel");
            await WaitForProcessing();

            // Verify ghost text is showing with dim style
            GetInputLineText(env).Should().EndWith("> help");
            env.Console.VirtualConsole.Should()
                .HaveRangeWithStyle(row: 0, startColumn: PromptLength + 3, length: 1, CellAttributes.Dim,
                    because: "ghost text 'p' should be dim");

            // Act - press Enter (should clear ghost text before submitting)
            env.Keyboard.PressEnter();
            await WaitForProcessing(100);

            // Assert - the original input line (row 0) should show "hel" without ghost text "p"
            var row0Text = env.Console.VirtualConsole.GetRow(0).GetText().TrimEnd();
            row0Text.Should().EndWith("> hel",
                because: "ghost text should be cleared from display before submission");
            row0Text.Should().NotEndWith("> help",
                because: "ghost text 'p' should have been cleared");
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

        #region Quoted Values Tests - FR-053 & FR-054

        // Test infrastructure for quoted value tests

        /// <summary>
        /// Handler that returns file paths, some with spaces.
        /// </summary>
        private class FilePathAutoCompleteHandler : BitPantry.CommandLine.AutoComplete.Handlers.IAutoCompleteHandler
        {
            private static readonly string[] Paths = 
            { 
                "Documents",           // No spaces - no quotes needed
                "My Documents",        // Space - should be quoted
                "Program Files",       // Space - should be quoted
                "AppData"              // No spaces - no quotes needed
            };

            public Task<System.Collections.Generic.List<BitPantry.CommandLine.AutoComplete.AutoCompleteOption>> GetOptionsAsync(
                BitPantry.CommandLine.AutoComplete.Handlers.AutoCompleteContext context,
                System.Threading.CancellationToken cancellationToken = default)
            {
                var options = new System.Collections.Generic.List<BitPantry.CommandLine.AutoComplete.AutoCompleteOption>();
                var query = context.QueryString ?? "";

                foreach (var path in Paths)
                {
                    if (string.IsNullOrEmpty(query) || 
                        path.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                    {
                        options.Add(new BitPantry.CommandLine.AutoComplete.AutoCompleteOption(path));
                    }
                }

                return Task.FromResult(options);
            }
        }

        /// <summary>
        /// Command with file path argument for testing quoted value autocomplete.
        /// </summary>
        [Command(Name = "open")]
        [Description("Open a file")]
        private class OpenCommand : CommandBase
        {
            [Argument]
            [BitPantry.CommandLine.AutoComplete.Handlers.AutoComplete<FilePathAutoCompleteHandler>]
            [Description("File path")]
            public string Path { get; set; }

            public void Execute(CommandExecutionContext ctx) { }
        }

        /// <summary>
        /// Creates a test environment configured for quoted value tests.
        /// </summary>
        private TestEnvironment CreateQuotedValueTestEnvironment()
        {
            return new TestEnvironment(opt =>
            {
                opt.ConfigureCommands(builder =>
                {
                    builder.RegisterCommand<OpenCommand>();
                });
                opt.ConfigureServices(services =>
                {
                    services.AddTransient<FilePathAutoCompleteHandler>();
                });
            });
        }

        /// <summary>
        /// Implements: 008:UX-028
        /// Given: Handler returns value containing spaces (e.g., "My Documents")
        /// When: Value is inserted via autocomplete
        /// Then: Value is wrapped in double quotes
        /// </summary>
        [TestMethod]
        public async Task Accept_ValueWithSpaces_WrapsInQuotes()
        {
            // FR-053: System MUST automatically wrap completion values containing spaces in double quotes when inserting
            // GIVEN: A handler returns "My Documents" (value with spaces)
            // WHEN: User types "open --path My" and accepts ghost text
            // THEN: Buffer should contain 'open --path "My Documents"' (quoted)

            using var env = CreateQuotedValueTestEnvironment();

            // Type command and partial argument value
            env.Keyboard.TypeText("open --path My");
            await WaitForProcessing();

            // Verify ghost text is showing for "My Documents"
            var lineTextBefore = GetInputLineText(env);
            lineTextBefore.Should().Contain("My Documents",
                because: "ghost text should show the completion for 'My Documents'");

            // Accept with Tab (single match since only "My Documents" starts with "My")
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - value should be wrapped in quotes
            var lineText = GetInputLineText(env);
            lineText.Should().Contain("\"My Documents\"",
                because: "values containing spaces should be wrapped in double quotes");
        }

        /// <summary>
        /// Implements: 008:UX-029
        /// Given: Handler returns value without spaces
        /// When: Value is inserted via autocomplete
        /// Then: Value is inserted without quotes
        /// </summary>
        [TestMethod]
        public async Task Accept_ValueWithoutSpaces_NoQuotes()
        {
            // FR-053: Only values with spaces need quoting
            // GIVEN: A handler returns "Documents" (no spaces)
            // WHEN: User types "open --path Doc" and accepts ghost text
            // THEN: Buffer should contain 'open --path Documents' (no quotes)

            using var env = CreateQuotedValueTestEnvironment();

            // Type command and partial argument value
            env.Keyboard.TypeText("open --path Doc");
            await WaitForProcessing();

            // Verify ghost text is showing
            var lineTextBefore = GetInputLineText(env);
            lineTextBefore.Should().Contain("Documents");

            // Accept with Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - value should NOT be wrapped in quotes
            var lineText = GetInputLineText(env);
            lineText.Should().Contain("Documents");
            lineText.Should().NotContain("\"Documents\"",
                because: "values without spaces should not be wrapped in quotes");
        }

        /// <summary>
        /// Implements: 008:UX-030
        /// Given: User has already typed opening quote
        /// When: Autocomplete inserts a value
        /// Then: Completion continues within quote context, adds closing quote
        /// </summary>
        [TestMethod]
        public async Task Accept_WithOpeningQuote_CompletesWithinQuote()
        {
            // FR-054: If the user has already typed an opening quote, completion MUST continue within the quote context
            // GIVEN: User has typed 'open --path "My'
            // WHEN: Handler returns "My Documents" and user accepts
            // THEN: Buffer should contain 'open --path "My Documents"' (with closing quote)

            using var env = CreateQuotedValueTestEnvironment();

            // Type command with opening quote and partial value
            env.Keyboard.TypeText("open --path \"My");
            await WaitForProcessing();

            // Verify ghost text is showing
            var lineTextBefore = GetInputLineText(env);
            // Ghost text should complete the value and add closing quote
            lineTextBefore.Should().Contain("My Documents\"",
                because: "ghost text should complete within quote context and add closing quote");

            // Accept with Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Assert - value should be completed within the quote
            var lineText = GetInputLineText(env);
            lineText.Should().Contain("\"My Documents\"",
                because: "completion should finish the quoted value with closing quote");
            lineText.Should().NotContain("\"\"",
                because: "should not double-quote");
        }

        [TestMethod]
        public async Task GhostText_ValueWithSpaces_ShowsRemainderThenQuotesOnAccept()
        {
            // FR-053 clarification: Ghost text shows the remainder (as ghost text can only append).
            // When accepted, the full value is wrapped in quotes.
            // GIVEN: A handler returns "My Documents" (value with spaces)
            // WHEN: User types "open --path M"
            // THEN: Ghost text shows "y Documents" (remainder), and after Tab, result is "My Documents" with quotes

            using var env = CreateQuotedValueTestEnvironment();

            // Type command and start of argument value
            env.Keyboard.TypeText("open --path M");
            await WaitForProcessing();

            // Ghost text should show the remainder without quotes (ghost text appends at cursor)
            var lineTextBefore = GetInputLineText(env);
            lineTextBefore.Should().Contain("My Documents",
                because: "ghost text should show the completion for 'My Documents'");

            // Accept with Tab
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // After acceptance, the value should be wrapped in quotes
            var lineTextAfter = GetInputLineText(env);
            lineTextAfter.Should().Contain("\"My Documents\"",
                because: "when inserting values with spaces, they should be wrapped in double quotes");
        }

        [TestMethod]
        public async Task GhostText_WithOpeningQuote_ShowsCompletionWithClosingQuote()
        {
            // FR-054: When already in quote context, ghost text shows completion + closing quote
            // GIVEN: User types 'open --path "M'
            // WHEN: Handler matches "My Documents"
            // THEN: Ghost text should show 'y Documents"' (rest of value + closing quote)

            using var env = CreateQuotedValueTestEnvironment();

            // Type command with opening quote and start of value
            env.Keyboard.TypeText("open --path \"M");
            await WaitForProcessing();

            // Assert - ghost text should complete value and add closing quote
            var lineText = GetInputLineText(env);
            lineText.Should().EndWith("My Documents\"",
                because: "ghost text should complete the value within quote context and show closing quote");
        }

        /// <summary>
        /// Implements: 008:UX-026b
        /// Given: Menu is open AND cursor is within an open quote
        /// When: User presses Space
        /// Then: Space is added to filter text, menu re-filters to show values containing the space
        /// </summary>
        [TestMethod]
        public async Task Space_InQuotedMenuContext_FiltersInsteadOfAccepting()
        {
            // UX-026b: Space filters within quoted context
            // GIVEN: Menu is open with cursor inside an opening quote
            // WHEN: User presses Space
            // THEN: Space adds to filter (does not accept selection)

            using var env = CreateQuotedValueTestEnvironment();

            // Type command with opening quote
            // Options: "Documents", "My Documents", "Program Files", "AppData" 
            // We need to type this in a way that gets us to quoted context
            env.Keyboard.TypeText("open");
            await WaitForProcessing();
            
            env.Keyboard.TypeText(" --path ");
            await WaitForProcessing();
            
            env.Keyboard.TypeText("\"");
            await WaitForProcessing();

            // Verify we have the opening quote in the line
            var lineBeforeTab = env.Console.VirtualConsole.GetRow(0).GetText();
            lineBeforeTab.Should().Contain("--path \"",
                because: "line should contain the opening quote");

            // Press Tab to open menu (should show all 4 options since empty query matches all)
            env.Keyboard.PressTab();
            await WaitForProcessing();

            // Verify menu is showing - check if first row after prompt has content
            var menuRow1 = env.Console.VirtualConsole.GetRow(1).GetText();
            menuRow1.Should().NotBeNullOrWhiteSpace(
                because: "menu should be showing after Tab");

            // Now press Space - since we're in a quoted context with menu open, 
            // it should filter instead of accept
            env.Keyboard.PressKey(ConsoleKey.Spacebar);
            await WaitForProcessing();

            // Assert - the space should have been added to the input (creating filter " ")
            // Check raw line without TrimEnd to see the space after quote
            var lineAfterSpace = env.Console.VirtualConsole.GetRow(0).GetText();
            
            // If space was treated as filter, the line should contain: " 
            // (quote followed by space)
            lineAfterSpace.Should().Contain("\" ",
                because: "space in quoted menu context should add to filter, not accept selection");
            
            // The selection should NOT have been accepted
            // (no closing quote from completed value)
            lineAfterSpace.Should().NotContain("\"Documents\"",
                because: "space should not have accepted Documents");
            lineAfterSpace.Should().NotContain("\"AppData\"",
                because: "space should not have accepted AppData");
            lineAfterSpace.Should().NotContain("\"My Documents\"",
                because: "space should not have accepted My Documents");
            lineAfterSpace.Should().NotContain("\"Program Files\"",
                because: "space should not have accepted Program Files");
        }

        #endregion
    }
}
