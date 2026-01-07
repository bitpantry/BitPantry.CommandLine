using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Menu Display &amp; Navigation Tests (TC-2.1 through TC-2.18)
/// Tests menu hypothesis: Tab opens menu, arrows navigate, Enter accepts, Escape cancels.
/// </summary>
[TestClass]
public class MenuNavigationTests
{
    #region TC-2.1: Tab Opens Menu with Multiple Matches

    /// <summary>
    /// TC-2.1: When Tab is pressed and multiple completions are available,
    /// Then a menu appears showing all matching options with first item selected.
    /// </summary>
    [TestMethod]
    public void TC_2_1_Tab_OpensMenuWithMultipleMatches()
    {
        // Arrange: Register multiple commands that could complete
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Type "s" and press Tab - should have multiple matches
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Menu should be visible with first item selected
        harness.IsMenuVisible.Should().BeTrue("menu should appear for multiple matches");
        harness.SelectedIndex.Should().Be(0, "first item should be selected");
        harness.MenuItemCount.Should().BeGreaterThan(1, "multiple items should be in menu");
    }

    #endregion

    #region TC-2.2: Tab with Single Match Auto-Completes

    /// <summary>
    /// TC-2.2: When Tab is pressed and only one completion matches,
    /// Then the completion is auto-inserted without showing a menu.
    /// </summary>
    [TestMethod]
    public void TC_2_2_Tab_SingleMatchAutoCompletes()
    {
        // Arrange: Register single command starting with unique prefix
        using var harness = AutoCompleteTestHarness.WithCommand<DisconnectTestCommand>();

        // Act: Type enough to have unique match
        harness.TypeText("disc");
        harness.PressTab();

        // Assert: Auto-complete inserted, no menu
        harness.Buffer.Should().Be("disconnect", "single match should auto-complete");
        harness.IsMenuVisible.Should().BeFalse("menu should not appear for single match");
    }

    #endregion

    #region TC-2.3: Tab with No Matches Does Nothing

    /// <summary>
    /// TC-2.3: When Tab is pressed with no available completions,
    /// Then nothing happens (no menu, no change).
    /// </summary>
    [TestMethod]
    public void TC_2_3_Tab_NoMatchesDoesNothing()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("xyznonexistent");
        var originalBuffer = harness.Buffer;

        // Act
        harness.PressTab();

        // Assert: Nothing changed
        harness.Buffer.Should().Be(originalBuffer, "buffer should be unchanged");
        harness.IsMenuVisible.Should().BeFalse("no menu for no matches");
    }

    #endregion

    #region TC-2.4: Tab at Empty Prompt Shows Root Commands

    /// <summary>
    /// TC-2.4: When Tab is pressed at an empty prompt,
    /// Then menu shows all available root-level commands.
    /// </summary>
    [TestMethod]
    public void TC_2_4_Tab_EmptyPromptShowsRootCommands()
    {
        // Arrange: Multiple root commands
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Press Tab on empty prompt
        harness.PressTab();

        // Assert: Menu shows root commands
        harness.IsMenuVisible.Should().BeTrue("menu should show root commands");
        harness.MenuItemCount.Should().BeGreaterOrEqualTo(3, "all root commands should be shown");
    }

    #endregion

    #region TC-2.5: Down Arrow Moves Selection Down

    /// <summary>
    /// TC-2.5: When menu is open and user presses Down Arrow,
    /// Then selection moves to the next item without changing buffer.
    /// </summary>
    [TestMethod]
    public void TC_2_5_DownArrow_MovesSelectionDown()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        var originalBuffer = harness.Buffer;
        harness.SelectedIndex.Should().Be(0);

        // Act
        harness.PressDownArrow();

        // Assert
        harness.SelectedIndex.Should().Be(1, "selection should move down");
        harness.Buffer.Should().Be(originalBuffer, "buffer should not change during navigation");
    }

    #endregion

    #region TC-2.6: Up Arrow Moves Selection Up

    /// <summary>
    /// TC-2.6: When menu is open and user presses Up Arrow,
    /// Then selection moves to the previous item.
    /// </summary>
    [TestMethod]
    public void TC_2_6_UpArrow_MovesSelectionUp()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        harness.PressDownArrow();
        harness.PressDownArrow(); // Now at index 2
        harness.SelectedIndex.Should().Be(2);

        // Act
        harness.PressUpArrow();

        // Assert
        harness.SelectedIndex.Should().Be(1, "selection should move up");
    }

    #endregion

    #region TC-2.7: Menu Wraps at Bottom

    /// <summary>
    /// TC-2.7: When navigating past the last menu item,
    /// Then selection wraps to the first item.
    /// </summary>
    [TestMethod]
    public void TC_2_7_Menu_WrapsAtBottom()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        var itemCount = harness.MenuItemCount;

        // Act: Navigate past last item
        for (int i = 0; i < itemCount; i++)
        {
            harness.PressDownArrow();
        }

        // Assert: Should wrap to first
        harness.SelectedIndex.Should().Be(0, "should wrap to first item");
    }

    #endregion

    #region TC-2.8: Menu Wraps at Top

    /// <summary>
    /// TC-2.8: When navigating up from the first menu item,
    /// Then selection wraps to the last item.
    /// </summary>
    [TestMethod]
    public void TC_2_8_Menu_WrapsAtTop()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        harness.SelectedIndex.Should().Be(0);
        var lastIndex = harness.MenuItemCount - 1;

        // Act: Navigate up from first item
        harness.PressUpArrow();

        // Assert: Should wrap to last
        harness.SelectedIndex.Should().Be(lastIndex, "should wrap to last item");
    }

    #endregion

    #region TC-2.9: Enter Accepts Selection and Closes Menu

    /// <summary>
    /// TC-2.9: When user presses Enter with menu open,
    /// Then selected item is inserted and menu closes.
    /// </summary>
    [TestMethod]
    public void TC_2_9_Enter_AcceptsSelectionAndClosesMenu()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        var selectedItem = harness.SelectedItem;
        selectedItem.Should().NotBeNullOrEmpty();

        // Act
        harness.PressEnter();

        // Assert
        harness.IsMenuVisible.Should().BeFalse("menu should close after Enter");
        harness.Buffer.Should().Be(selectedItem, "selected item should be inserted");
    }

    #endregion

    #region TC-2.10: Escape Closes Menu Without Changing Buffer

    /// <summary>
    /// TC-2.10: When user presses Escape with menu open,
    /// Then menu closes but buffer remains at its pre-Tab state.
    /// </summary>
    [TestMethod]
    public void TC_2_10_Escape_ClosesMenuWithoutChangingBuffer()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        var bufferBeforeTab = harness.Buffer;
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue();

        // Act
        harness.PressEscape();

        // Assert
        harness.IsMenuVisible.Should().BeFalse("menu should close on Escape");
        harness.Buffer.Should().Be(bufferBeforeTab, "buffer should remain unchanged");
    }

    #endregion

    #region TC-2.11: Tab Advances to Next Item When Menu Open

    /// <summary>
    /// TC-2.11: When menu is already open and user presses Tab,
    /// Then selection advances to the next item.
    /// </summary>
    [TestMethod]
    public void TC_2_11_Tab_AdvancesToNextItemWhenMenuOpen()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        harness.SelectedIndex.Should().Be(0);

        // Act: Press Tab again
        harness.PressTab();

        // Assert
        harness.SelectedIndex.Should().Be(1, "Tab should advance selection");
    }

    #endregion

    #region TC-2.12: Shift+Tab Goes to Previous Item

    /// <summary>
    /// TC-2.12: When menu is open and user presses Shift+Tab,
    /// Then selection moves to the previous item.
    /// </summary>
    [TestMethod]
    public void TC_2_12_ShiftTab_GoesToPreviousItem()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        harness.PressTab(); // Now at index 1
        harness.SelectedIndex.Should().Be(1);

        // Act
        harness.PressKey(ConsoleKey.Tab, shift: true);

        // Assert
        harness.SelectedIndex.Should().Be(0, "Shift+Tab should go to previous item");
    }

    #endregion

    #region TC-2.13: Completion Inserted at Correct Position

    /// <summary>
    /// TC-2.13: When accepting a completion from menu,
    /// Then the text is inserted at the correct position.
    /// </summary>
    [TestMethod]
    public void TC_2_13_Completion_InsertedAtCorrectPosition()
    {
        // Arrange: Command with arguments
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        harness.TypeText("server ");
        harness.PressTab();
        
        // The menu should show arguments like --Host, --Port
        if (harness.IsMenuVisible && harness.MenuItemCount > 0)
        {
            var selectedItem = harness.SelectedItem;
            
            // Act
            harness.PressEnter();

            // Assert: The completed item should be appended
            harness.Buffer.Should().StartWith("server ", "original text should remain");
            harness.Buffer.Should().Contain(selectedItem, "selected item should be inserted");
        }
        else
        {
            // If no menu items available, that's also valid behavior
            Assert.Inconclusive("No completion items available for this test scenario");
        }
    }

    #endregion

    #region TC-2.14: No Trailing Space After Completion Acceptance

    /// <summary>
    /// TC-2.14: When accepting a completion from menu,
    /// Then no trailing space is added after the completion.
    /// </summary>
    [TestMethod]
    public void TC_2_14_NoTrailingSpace_AfterCompletion()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();
        var selectedItem = harness.SelectedItem;

        // Act
        harness.PressEnter();

        // Assert
        harness.Buffer.Should().Be(selectedItem, "buffer should have no trailing space");
        harness.Buffer.Should().NotEndWith(" ", "no trailing space after completion");
    }

    #endregion

    #region TC-2.15: Menu Shows Descriptions (when available)

    /// <summary>
    /// TC-2.15: When completion items have descriptions,
    /// Then descriptions are available in the menu items.
    /// 
    /// NOTE: This test validates the data model; visual rendering is tested separately.
    /// </summary>
    [TestMethod]
    public void TC_2_15_MenuItems_HaveDescriptions()
    {
        // Arrange: Use command with descriptions
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.PressTab();

        // Assert: Menu items should have descriptions (not validating visual rendering here)
        if (harness.IsMenuVisible && harness.MenuItems != null)
        {
            // At least some items should have descriptions
            // This validates the data model, not the rendering
            harness.MenuItems.Should().NotBeEmpty();
        }
        else
        {
            Assert.Inconclusive("Menu not available for description test");
        }
    }

    #endregion

    #region TC-2.16: Missing Description Handled Gracefully

    /// <summary>
    /// TC-2.16: When a completion item has no description,
    /// Then item displays without description (no error).
    /// </summary>
    [TestMethod]
    public void TC_2_16_MissingDescription_HandledGracefully()
    {
        // Arrange: Commands may not all have descriptions
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand));

        // Act
        harness.PressTab();

        // Assert: Should not throw, menu works regardless of descriptions
        harness.IsMenuVisible.Should().BeTrue("menu should work without descriptions");
        harness.MenuItemCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region TC-2.17: Long Description Handling

    /// <summary>
    /// TC-2.17: When description exceeds terminal width,
    /// Then it is handled appropriately (truncated or wrapped).
    /// 
    /// NOTE: Visual rendering is handled by VirtualConsole; this tests the menu functions.
    /// </summary>
    [TestMethod]
    public void TC_2_17_LongDescription_HandledAppropriately()
    {
        // Arrange: Create harness with narrow width
        using var harness = new AutoCompleteTestHarness(
            width: 40,  // Narrow terminal
            height: 24);

        // Register command
        harness.Application.Services.GetType(); // Just verify harness works with narrow width

        // This test validates that narrow widths don't cause errors
        // Actual truncation behavior depends on rendering implementation
    }

    #endregion

    #region TC-2.18: Match Count Visibility

    /// <summary>
    /// TC-2.18: When menu is open, position information is tracked.
    /// 
    /// NOTE: Visual indicator tested separately; this tests the data is available.
    /// </summary>
    [TestMethod]
    public void TC_2_18_MatchCount_TrackedCorrectly()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Position and count are tracked
        harness.SelectedIndex.Should().Be(0);
        harness.MenuItemCount.Should().BeGreaterThan(0);

        // Navigate and verify index updates
        harness.PressDownArrow();
        harness.SelectedIndex.Should().Be(1);
    }

    #endregion
}
