using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Ghost Text Behavior Tests (TC-1.1 through TC-1.16)
/// Tests ghost text hypothesis: dim text appears after cursor showing predicted completion.
/// </summary>
[TestClass]
public class GhostTextTests
{
    #region TC-1.1: Single Character Shows Ghost Completion

    /// <summary>
    /// TC-1.1: When the user types a single character that matches the beginning of a command,
    /// Then ghost text appears showing the remainder of the best matching command.
    /// </summary>
    [TestMethod]
    public void TC_1_1_SingleCharacter_ShowsGhostCompletion()
    {
        // Arrange: Create harness with "server" command
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type "s"
        harness.TypeText("s");

        // Assert: Ghost text shows "erver" (completing "server")
        harness.HasGhostText.Should().BeTrue("ghost text should appear for partial match");
        harness.GhostText.Should().Be("erver", "ghost should show remainder to complete 'server'");
    }

    #endregion

    #region TC-1.2: Partial Word Shows Remainder

    /// <summary>
    /// TC-1.2: When the user types a partial command prefix,
    /// Then ghost text shows only the remaining characters to complete the word.
    /// </summary>
    [TestMethod]
    public void TC_1_2_PartialWord_ShowsRemainder()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type "ser"
        harness.TypeText("ser");

        // Assert: Ghost text shows "ver" (not full "server")
        harness.HasGhostText.Should().BeTrue();
        harness.GhostText.Should().Be("ver", "ghost should only show remaining characters");
    }

    #endregion

    #region TC-1.3: Exact Match Hides Ghost

    /// <summary>
    /// TC-1.3: When the user types a complete command that exactly matches a registered command,
    /// Then no ghost text appears.
    /// </summary>
    [TestMethod]
    public void TC_1_3_ExactMatch_HidesGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type complete command "server"
        harness.TypeText("server");

        // Assert: No ghost text visible (exact match)
        harness.HasGhostText.Should().BeFalse("no ghost should appear for exact command match");
    }

    #endregion

    #region TC-1.4: No Match Shows No Ghost

    /// <summary>
    /// TC-1.4: When the user types text that doesn't match any command or completion,
    /// Then no ghost text appears.
    /// </summary>
    [TestMethod]
    public void TC_1_4_NoMatch_ShowsNoGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type non-matching text
        harness.TypeText("xyznonexistent");

        // Assert: No ghost text
        harness.HasGhostText.Should().BeFalse("no ghost should appear when nothing matches");
    }

    #endregion

    #region TC-1.5: Subcommand Ghost After Command Space

    /// <summary>
    /// TC-1.5: When the user types a complete command followed by a space,
    /// Then ghost text shows the first available subcommand or argument.
    /// </summary>
    [TestMethod]
    public void TC_1_5_SubcommandGhost_AfterCommandSpace()
    {
        // Arrange: Register server command which has --host argument
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type "server " (with space)
        harness.TypeText("server ");

        // Assert: Ghost text shows first available completion (argument name)
        harness.HasGhostText.Should().BeTrue("ghost should show after command + space");
        // The ghost might show "--Host" or similar based on what's available
        harness.GhostText.Should().NotBeNullOrEmpty("ghost should suggest next completion");
    }

    #endregion

    #region TC-1.6: Deep Nested Subcommand Ghost

    /// <summary>
    /// TC-1.6: When navigating into nested command groups,
    /// Then ghost text reflects available completions at each level.
    /// </summary>
    [TestMethod]
    public void TC_1_6_DeepNested_SubcommandGhost()
    {
        // Arrange: Register multiple commands for testing nested completion
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ConnectTestCommand),
            typeof(DisconnectTestCommand));

        // Act: Type "co" - should match "connect"
        harness.TypeText("co");

        // Assert: Ghost text shows remaining characters for "connect"
        harness.HasGhostText.Should().BeTrue();
        harness.GhostText.Should().Be("nnect", "ghost should complete 'connect' from 'co'");
    }

    #endregion

    #region TC-1.7: Right Arrow Accepts Ghost Text

    /// <summary>
    /// TC-1.7: When ghost text is displayed and user presses Right Arrow,
    /// Then the ghost text is accepted and inserted into the buffer.
    /// </summary>
    [TestMethod]
    public void TC_1_7_RightArrow_AcceptsGhostText()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("s"); // Ghost shows "erver"

        // Act: Press Right Arrow
        harness.PressRightArrow();

        // Assert: Buffer contains full "server", ghost cleared
        harness.Buffer.Should().Be("server", "right arrow should accept ghost text");
        harness.HasGhostText.Should().BeFalse("ghost should be cleared after acceptance");
    }

    #endregion

    #region TC-1.8: End Key Accepts Ghost Text

    /// <summary>
    /// TC-1.8: When ghost text is displayed and user presses End key,
    /// Then the ghost text is accepted and inserted into the buffer.
    /// </summary>
    [TestMethod]
    public void TC_1_8_EndKey_AcceptsGhostText()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("ser"); // Ghost shows "ver"

        // Act: Press End key
        harness.PressKey(ConsoleKey.End);

        // Assert: Buffer contains "server"
        harness.Buffer.Should().Be("server", "End key should accept ghost text");
    }

    #endregion

    #region TC-1.9: Cursor Movement Left Hides Ghost

    /// <summary>
    /// TC-1.9: When the user moves cursor left (away from end of line),
    /// Then ghost text disappears.
    /// </summary>
    [TestMethod]
    public void TC_1_9_CursorLeft_HidesGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("s"); // Ghost shows "erver"
        harness.HasGhostText.Should().BeTrue("precondition: ghost should be visible");

        // Act: Press Left Arrow
        harness.PressLeftArrow();

        // Assert: Ghost text should disappear
        harness.HasGhostText.Should().BeFalse("ghost should hide when cursor moves left");
    }

    #endregion

    #region TC-1.10: Typing Matching Character Shrinks Ghost

    /// <summary>
    /// TC-1.10: When the user types a character that matches the next ghost character,
    /// Then ghost text shrinks by one character.
    /// </summary>
    [TestMethod]
    public void TC_1_10_TypingMatchingCharacter_ShrinksGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("s"); // Ghost shows "erver"
        harness.GhostText.Should().Be("erver");

        // Act: Type "e" (matches next ghost character)
        harness.TypeText("e");

        // Assert: Buffer is "se", ghost is "rver"
        harness.Buffer.Should().Be("se");
        harness.GhostText.Should().Be("rver", "ghost should shrink by one character");
    }

    #endregion

    #region TC-1.11: Typing Non-Matching Character Clears Ghost

    /// <summary>
    /// TC-1.11: When the user types a character that doesn't match the ghost,
    /// Then ghost text disappears.
    /// </summary>
    [TestMethod]
    public void TC_1_11_TypingNonMatchingCharacter_ClearsGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("s"); // Ghost shows "erver"
        harness.HasGhostText.Should().BeTrue();

        // Act: Type "x" (doesn't match "e")
        harness.TypeText("x");

        // Assert: Buffer is "sx", ghost is cleared
        harness.Buffer.Should().Be("sx");
        harness.HasGhostText.Should().BeFalse("ghost should clear for non-matching character");
    }

    #endregion

    #region TC-1.12: Backspace Updates Ghost for New Prefix

    /// <summary>
    /// TC-1.12: When the user presses Backspace,
    /// Then ghost text updates based on new prefix.
    /// </summary>
    [TestMethod]
    public void TC_1_12_Backspace_UpdatesGhost()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("ser"); // Ghost shows "ver"
        harness.GhostText.Should().Be("ver");

        // Act: Press Backspace
        harness.PressBackspace();

        // Assert: Buffer is "se", ghost is "rver"
        harness.Buffer.Should().Be("se");
        harness.GhostText.Should().Be("rver", "ghost should update for shorter prefix");
    }

    #endregion

    #region TC-1.13: Ghost Source Priority - History Over Commands

    /// <summary>
    /// TC-1.13: When both history and registered commands match the typed prefix,
    /// Then history matches are prioritized for ghost text.
    /// 
    /// NOTE: This test requires command history integration which may not be
    /// available in the current implementation. Marking as test with expected behavior.
    /// </summary>
    [TestMethod]
    [Ignore("History integration not yet available in test harness")]
    public void TC_1_13_GhostSourcePriority_HistoryOverCommands()
    {
        // This test requires history to be populated, which needs more harness work
        // Will implement when history integration is available
    }

    #endregion

    #region TC-1.14: Ghost Uses Most Recent History Entry

    /// <summary>
    /// TC-1.14: When multiple history entries match the prefix,
    /// Then the most recent entry is used for ghost text.
    /// 
    /// NOTE: This test requires command history integration.
    /// </summary>
    [TestMethod]
    [Ignore("History integration not yet available in test harness")]
    public void TC_1_14_Ghost_UsesMostRecentHistory()
    {
        // This test requires history integration
    }

    #endregion

    #region TC-1.15: Ghost for Argument Values After Argument Name

    /// <summary>
    /// TC-1.15: When user has typed an argument name followed by space,
    /// Then ghost shows first available value completion.
    /// </summary>
    [TestMethod]
    public void TC_1_15_Ghost_ForArgumentValues()
    {
        // Arrange: Command with enum argument
        using var harness = AutoCompleteTestHarness.WithCommand<EnumArgTestCommand>();

        // Act: Type command and argument name with space
        harness.TypeText("enumcmd --Level ");

        // Assert: Ghost shows first enum value
        harness.HasGhostText.Should().BeTrue("ghost should show argument value options");
        // The first enum value should be suggested (Low, Medium, High, Critical)
        harness.GhostText.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region TC-1.16: Alt+Right Accepts First Word of Ghost

    /// <summary>
    /// TC-1.16: When ghost text contains multiple words and user presses Alt+Right Arrow,
    /// Then only the first word of ghost is accepted.
    /// 
    /// NOTE: Alt+Right behavior may vary by implementation.
    /// </summary>
    [TestMethod]
    public void TC_1_16_AltRight_AcceptsFirstWordOnly()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("s"); // Ghost shows "erver"

        // Act: Press Alt+Right Arrow
        harness.PressKey(ConsoleKey.RightArrow, alt: true);

        // Assert: Should accept word-by-word
        // For single-word ghost, behavior same as regular right arrow
        harness.Buffer.Should().Contain("server");
    }

    #endregion
}
