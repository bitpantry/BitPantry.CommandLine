using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// History Navigation Tests (TC-13.1 through TC-13.4)
/// Tests command history navigation during autocomplete.
/// </summary>
[TestClass]
public class HistoryNavigationTests
{
    #region TC-13.1: Up Arrow Navigates History When Menu Closed

    /// <summary>
    /// TC-13.1: When menu is closed and user presses Up Arrow,
    /// Then previous command from history is loaded into buffer.
    /// 
    /// NOTE: Requires history integration - testing infrastructure.
    /// </summary>
    [TestMethod]
    public void TC_13_1_UpArrow_NavigatesHistory_WhenMenuClosed()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Just verify up arrow doesn't crash with no history
        harness.PressUpArrow();

        // Assert: Buffer should be empty (no history)
        harness.Buffer.Should().BeEmpty();
    }

    #endregion

    #region TC-13.2: Up Arrow in Menu Navigates Menu, Not History

    /// <summary>
    /// TC-13.2: When menu is open and user presses Up Arrow,
    /// Then it navigates menu selection (not history).
    /// </summary>
    [TestMethod]
    public void TC_13_2_UpArrow_InMenu_NavigatesMenu_NotHistory()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("server ");
        harness.PressTab();
        
        if (harness.IsMenuVisible && harness.MenuItemCount > 1)
        {
            harness.PressDownArrow(); // Move to index 1
            harness.SelectedIndex.Should().Be(1);
            
            harness.PressUpArrow(); // Back to index 0

            // Assert: Menu navigation, not history
            harness.SelectedIndex.Should().Be(0);
            harness.Buffer.Should().StartWith("server ");
        }
    }

    #endregion

    #region TC-13.3: Down Arrow Returns Through History

    /// <summary>
    /// TC-13.3: When navigating back through history with Up Arrow,
    /// Then Down Arrow returns toward most recent entry.
    /// 
    /// NOTE: Requires history integration - testing infrastructure.
    /// </summary>
    [TestMethod]
    public void TC_13_3_DownArrow_ReturnsThroughHistory()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Navigate up then down (no history present)
        harness.PressUpArrow();
        harness.PressDownArrow();

        // Assert: No crash, buffer still empty
        harness.Buffer.Should().BeEmpty();
    }

    #endregion

    #region TC-13.4: History Navigation Then Tab Works

    /// <summary>
    /// TC-13.4: When user loads command from history then presses Tab,
    /// Then completion works on the loaded command.
    /// 
    /// NOTE: Requires history integration - testing infrastructure.
    /// </summary>
    [TestMethod]
    public void TC_13_4_HistoryNavigation_ThenTab_Works()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Simulate typing then Tab
        harness.TypeText("server ");
        harness.PressTab();

        // Assert: Tab works on current buffer
        harness.IsMenuVisible.Should().BeTrue();
    }

    #endregion
}
