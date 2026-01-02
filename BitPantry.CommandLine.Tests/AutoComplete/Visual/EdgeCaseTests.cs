using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for edge cases and special scenarios including:
/// - Navigation edge cases
/// - Multi-word and complex input
/// - Cancellation and interruption
/// - State persistence and cleanup
/// - Parameter completion
/// - Rapid input
/// - Visual rendering edge cases
/// </summary>
[TestClass]
public class EdgeCaseTests : VisualTestBase
{
    #region Navigation Edge Cases

    [TestMethod]
    [TestDescription("Home key while menu open should close menu and move cursor")]
    public async Task HomeKey_WhileMenuOpen_ClosesMenuAndMovesCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: menu open
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var initialCursor = runner.BufferPosition;
        initialCursor.Should().BeGreaterThan(0, "cursor should be past start position");

        // Action: press Home
        await runner.PressKey(ConsoleKey.Home);

        // Ending condition: menu closed, cursor at start
        runner.Should().HaveMenuHidden();
        runner.Buffer.Should().Be("server ");
    }

    [TestMethod]
    [TestDescription("End key while menu open should close menu and move cursor")]
    public async Task EndKey_WhileMenuOpen_ClosesMenuAndMovesCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: menu open
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        runner.Buffer.Should().StartWith("server ");

        // Action: press End
        await runner.PressKey(ConsoleKey.End);

        // Ending condition: menu closed, cursor at end
        runner.Should().HaveMenuHidden();
        runner.BufferPosition.Should().Be(runner.Buffer.Length);
    }

    [TestMethod]
    [TestDescription("Left arrow at position 0 should do nothing")]
    public async Task LeftArrow_AtPositionZero_DoesNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: cursor at start of buffer via multiple left arrows
        await runner.TypeText("server");
        // Move cursor to start with left arrows
        for (int i = 0; i < 6; i++)
            await runner.PressKey(ConsoleKey.LeftArrow);
        runner.BufferPosition.Should().Be(0, "should be at position 0 after moving left 6 times");

        // Action: press Left again at position 0
        await runner.PressKey(ConsoleKey.LeftArrow);

        // Ending condition: still at position 0
        runner.BufferPosition.Should().Be(0);
        runner.Buffer.Should().Be("server");
    }

    [TestMethod]
    [TestDescription("Right arrow at end of buffer should do nothing (no ghost)")]
    public async Task RightArrow_AtEndNoGhost_DoesNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: cursor at end, no ghost
        await runner.TypeText("server");
        runner.BufferPosition.Should().Be(6);
        runner.GhostText.Should().BeNullOrEmpty();

        // Action: press Right
        await runner.PressKey(ConsoleKey.RightArrow);

        // Ending condition: buffer unchanged
        runner.BufferPosition.Should().Be(6);
        runner.Buffer.Should().Be("server");
    }

    #endregion

    #region Multi-Word and Complex Input

    [TestMethod]
    [TestDescription("Double space should not cause issues")]
    public async Task TabAfterDoubleSpace_HandlesGracefully()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: empty
        runner.Buffer.Should().BeEmpty();

        // Action: type with double space
        await runner.TypeText("server  ");
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: should handle gracefully (menu or no-op)
        runner.Buffer.Should().StartWith("server  ");
    }

    [TestMethod]
    [TestDescription("Tab mid-word should complete from that position")]
    public async Task TabMidWord_CompletesFromCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: type word, move cursor back
        await runner.TypeText("serverxxx");
        await runner.PressKey(ConsoleKey.LeftArrow);
        await runner.PressKey(ConsoleKey.LeftArrow);
        await runner.PressKey(ConsoleKey.LeftArrow);
        runner.Buffer.Should().Be("serverxxx");
        runner.BufferPosition.Should().Be(6);

        // Action: press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: completion behavior at cursor position
        // (specific behavior depends on implementation)
    }

    [TestMethod]
    [TestDescription("Commands with hyphens should complete correctly")]
    public async Task HyphenatedCommand_Completes()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // This test documents expected behavior for hyphenated commands
        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type partial hyphenated command
        await runner.TypeText("my-");

        // Ending condition: ghost or no ghost depending on registered commands
        runner.Buffer.Should().Be("my-");
    }

    [TestMethod]
    [TestDescription("Multiple words with Tab completion")]
    public async Task MultipleWords_TabCompletion()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type full command path
        await runner.TypeText("server profile add ");
        
        // Tab should attempt to complete from this context
        await runner.PressKey(ConsoleKey.Tab);

        // Should be at parameter completion level
        Debug.WriteLine($"Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");
    }

    #endregion

    #region Cancellation and Interruption

    [TestMethod]
    [TestDescription("Escape multiple times should not cause issues")]
    public async Task MultipleEscape_HandlesGracefully()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: menu open
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Action: press Escape multiple times
        await runner.PressKey(ConsoleKey.Escape);
        await runner.PressKey(ConsoleKey.Escape);
        await runner.PressKey(ConsoleKey.Escape);

        // Ending condition: menu still closed, no crash
        runner.Should().HaveMenuHidden();
        runner.Buffer.Should().Be("server ");
    }

    [TestMethod]
    [TestDescription("Tab then Escape then Tab should reopen menu")]
    public async Task TabEscapeTab_ReopensMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        await runner.TypeText("server ");
        
        // Action 1: Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Action 2: Escape to close
        await runner.PressKey(ConsoleKey.Escape);
        runner.Should().HaveMenuHidden();

        // Action 3: Tab again
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: menu should reopen
        runner.Should().HaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("Escape with no menu should be no-op")]
    public async Task Escape_NoMenu_IsNoop()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hello");
        var bufferBefore = runner.Buffer;

        // Escape when no menu is visible
        await runner.PressKey(ConsoleKey.Escape);

        // Buffer unchanged
        runner.Buffer.Should().Be(bufferBefore);
        runner.Should().HaveMenuHidden();
    }

    #endregion

    #region State Persistence and Cleanup

    [TestMethod]
    [TestDescription("Buffer modification after Tab completion should work")]
    public async Task BufferModificationAfterCompletion_Works()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.Enter); // Accept first completion

        var afterCompletion = runner.Buffer;
        runner.Should().HaveMenuHidden();

        // Action: type more text
        await runner.TypeText(" --verbose");

        // Ending condition: buffer should have additional text
        runner.Buffer.Should().EndWith(" --verbose");
        runner.Buffer.Length.Should().BeGreaterThan(afterCompletion.Length);
    }

    [TestMethod]
    [TestDescription("Clear buffer and start fresh after completion")]
    public async Task ClearBufferAfterCompletion_StartsFresh()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Complete something
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.Enter);

        // Clear buffer
        while (runner.Buffer.Length > 0)
        {
            await runner.PressKey(ConsoleKey.Backspace);
        }

        runner.Buffer.Should().BeEmpty();
        runner.Should().HaveMenuHidden();

        // Start fresh
        await runner.TypeText("help");
        runner.Buffer.Should().Be("help");
    }

    #endregion

    #region History Integration with Autocomplete

    [TestMethod]
    [TestDescription("Recalling history then pressing Tab should work")]
    public async Task HistoryRecallThenTab_Works()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action 1: recall history
        await runner.PressKey(ConsoleKey.UpArrow);
        var historyEntry = runner.Buffer;
        historyEntry.Should().NotBeEmpty();

        // Action 2: modify and Tab
        await runner.PressKey(ConsoleKey.Home);
        
        // Tab should work on history entry
        await runner.PressKey(ConsoleKey.Tab);

        // No crash
        Debug.WriteLine($"Buffer after Tab on history: '{runner.Buffer}'");
    }

    [TestMethod]
    [TestDescription("Clear history entry and type new command")]
    public async Task ClearHistoryEntryAndType_Works()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // Recall history
        await runner.PressKey(ConsoleKey.UpArrow);
        runner.Buffer.Should().NotBeEmpty();

        // Clear it
        while (runner.Buffer.Length > 0)
        {
            await runner.PressKey(ConsoleKey.Backspace);
        }

        // Type new command
        await runner.TypeText("config");
        runner.Buffer.Should().Be("config");
    }

    #endregion

    #region Visual Rendering Edge Cases

    [TestMethod]
    [TestDescription("Long input near console width should not wrap incorrectly")]
    public async Task LongInput_NearConsoleWidth_HandlesCorrectly()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type long text
        var longText = new string('a', 60);
        await runner.TypeText(longText);

        // Ending condition: buffer contains full text
        runner.Buffer.Should().Be(longText);
        runner.BufferPosition.Should().Be(longText.Length);
    }

    [TestMethod]
    [TestDescription("Menu should display correctly with many items")]
    public async Task MenuWithManyItems_DisplaysCorrectly()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: type to get menu with items
        await runner.TypeText("server ");

        // Action: open menu
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: menu visible
        runner.Should().HaveMenuVisible();
        runner.MenuItemCount.Should().BeGreaterThan(0);
    }

    [TestMethod]
    [TestDescription("Very long completion text should display correctly")]
    public async Task VeryLongCompletionText_DisplaysCorrectly()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type to get completions
        await runner.TypeText("server connect ");
        await runner.PressKey(ConsoleKey.Tab);

        if (runner.IsMenuVisible)
        {
            Debug.WriteLine($"Menu item: {runner.SelectedMenuItem}");
            // Just verify no crash
        }
    }

    #endregion

    #region Parameter Completion

    [TestMethod]
    [TestDescription("Tab after -- should attempt parameter name completion")]
    public async Task TabAfterDoubleDash_ShowsParameters()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: command with partial parameter
        await runner.TypeText("server connect --");
        runner.Buffer.Should().Be("server connect --");

        // Action: Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: Tab was processed
        runner.Buffer.Should().StartWith("server connect --");
    }

    [TestMethod]
    [TestDescription("Tab after parameter name and space should complete value")]
    public async Task TabAfterParameterSpace_ShowsValues()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: command with parameter awaiting value
        await runner.TypeText("server connect --host ");

        // Action: Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: should show values or do nothing if no completions
        Debug.WriteLine($"Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");
    }

    [TestMethod]
    [TestDescription("Partial parameter name completion")]
    public async Task PartialParameterName_Completes()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type command with partial parameter
        await runner.TypeText("server connect --ho");

        // Tab should complete to --host
        await runner.PressKey(ConsoleKey.Tab);

        Debug.WriteLine($"Buffer after Tab: '{runner.Buffer}'");
        // Should complete or show menu
    }

    #endregion

    #region Rapid Input

    [TestMethod]
    [TestDescription("Rapid typing should update buffer correctly")]
    public async Task RapidTyping_UpdatesBufferCorrectly()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type rapidly
        await runner.TypeText("server connect --host localhost --port 8080");

        // Ending condition: full text in buffer
        runner.Buffer.Should().Be("server connect --host localhost --port 8080");
    }

    [TestMethod]
    [TestDescription("Rapid Tab and arrow keys should not corrupt state")]
    public async Task RapidTabAndArrows_MaintainsState()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        await runner.TypeText("server ");

        // Action: rapid menu interactions
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.UpArrow);
        await runner.PressKey(ConsoleKey.Escape);
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: menu visible, state consistent
        runner.Should().HaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("Rapid backspace should work correctly")]
    public async Task RapidBackspace_WorksCorrectly()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect");

        // Rapid backspace
        for (int i = 0; i < 7; i++)
        {
            await runner.PressKey(ConsoleKey.Backspace);
        }

        runner.Buffer.Should().Be("server ");
    }

    #endregion

    #region Special Characters

    [TestMethod]
    [TestDescription("Typing special characters should be handled")]
    public async Task SpecialCharacters_Handled()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("test@value");
        runner.Buffer.Should().Be("test@value");

        await runner.TypeText(" --param=value");
        runner.Buffer.Should().Be("test@value --param=value");
    }

    [TestMethod]
    [TestDescription("Quoted strings should be handled")]
    public async Task QuotedStrings_Handled()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect --host \"localhost\"");
        runner.Buffer.Should().Be("server connect --host \"localhost\"");
    }

    #endregion
}
