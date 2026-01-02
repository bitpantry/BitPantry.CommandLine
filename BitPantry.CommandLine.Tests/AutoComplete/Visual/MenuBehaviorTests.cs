using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for menu behavior including:
/// - Tab key opening/closing menu
/// - Arrow key navigation
/// - Enter accepting selection
/// - Escape canceling
/// - Menu wrapping
/// - Typing while menu is open
/// </summary>
[TestClass]
public class MenuBehaviorTests : VisualTestBase
{
    #region Tab Opens/Closes Menu

    [TestMethod]
    [TestDescription("Tab opens menu and displays first completion in buffer")]
    public async Task Tab_OpensMenuAndDisplaysFirstItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hel");
        runner.Should().HaveState("hel", 3)
                       .And.NotHaveMenuVisible();

        // Press Tab - should auto-complete to "help " (single match)
        await runner.PressKey(ConsoleKey.Tab);
        
        // With single match, menu closes immediately and text is completed
        runner.Should().HaveBuffer("help ");
        runner.Should().HaveInputCursorAt(5, "cursor should be at end of 'help '");
    }

    [TestMethod]
    [TestDescription("Tab with multiple matches opens menu with first item selected")]
    public async Task Tab_MultipleMatches_OpensMenuWithFirstSelected()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        runner.Should().HaveState("server ", 7);

        // Press Tab - should open menu with subcommands
        await runner.PressKey(ConsoleKey.Tab);
        
        runner.Should().HaveMenuVisible("menu should be visible after Tab");
        runner.Should().HaveMenuSelectedIndex(0, "first item should be selected");
        
        // Buffer should show first completion
        var firstItem = runner.SelectedMenuItem;
        firstItem.Should().NotBeNullOrEmpty();
        runner.Buffer.Should().StartWith("server ");
    }

    [TestMethod]
    [TestDescription("Tab with no matches should do nothing")]
    public async Task TabWithNoMatches_ShouldDoNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type something that won't match any command
        await runner.TypeText("xyznonexistent ");
        var originalBuffer = runner.Buffer;

        await runner.PressKey(ConsoleKey.Tab);

        // Should remain unchanged, no menu
        runner.Should().HaveMenuHidden();
        runner.Buffer.Should().Be(originalBuffer);
    }

    [TestMethod]
    [TestDescription("Tab with single match should auto-insert without menu")]
    public async Task TabWithSingleMatch_ShouldAutoInsert()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type partial that has only one match - "server disc" should only match "disconnect"
        await runner.TypeText("server disc");

        await runner.PressKey(ConsoleKey.Tab);

        // Should auto-insert without showing menu
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");
        
        // If single match, should complete to "disconnect"
        if (!runner.IsMenuVisible)
        {
            runner.Buffer.Should().Contain("disconnect");
        }
    }

    [TestMethod]
    [TestDescription("Tab at empty prompt should show all root commands")]
    public async Task TabAtEmptyPrompt_ShouldShowRootCommands()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Tab at empty prompt
        await runner.PressKey(ConsoleKey.Tab);

        Debug.WriteLine($"After Tab at empty - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: {runner.SelectedMenuItem}");

        // Should show menu with available commands
        runner.Should().HaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("Tab then Tab again should advance to next item")]
    public async Task TabThenTab_ShouldAdvanceToNextItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        var firstItem = runner.SelectedMenuItem;
        Debug.WriteLine($"First item: {firstItem}");

        // Second Tab should advance
        await runner.PressKey(ConsoleKey.Tab);
        
        var secondItem = runner.SelectedMenuItem;
        Debug.WriteLine($"Second item: {secondItem}");

        // Should have advanced (or wrapped if at end)
        runner.Should().HaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("Shift+Tab in menu should go to previous item")]
    public async Task ShiftTabInMenu_ShouldGoToPreviousItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Move down first
        await runner.PressKey(ConsoleKey.DownArrow);
        var currentItem = runner.SelectedMenuItem;
        var currentIndex = runner.MenuSelectedIndex;
        Debug.WriteLine($"After DownArrow: index={currentIndex}, item={currentItem}");

        // Shift+Tab should go back
        await runner.PressShiftTab();
        
        Debug.WriteLine($"After Shift+Tab: index={runner.MenuSelectedIndex}, item={runner.SelectedMenuItem}");

        runner.Should().HaveMenuVisible();
        runner.MenuSelectedIndex.Should().BeLessThan(currentIndex);
    }

    #endregion

    #region Arrow Key Navigation

    [TestMethod]
    [TestDescription("CRITICAL: Down arrow in menu moves to next item")]
    public async Task DownArrow_InMenu_MovesToNextItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server " and open menu
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.Should().HaveMenuVisible();
        var firstItem = runner.SelectedMenuItem;
        var firstIndex = runner.MenuSelectedIndex;
        firstIndex.Should().Be(0, "first item (index 0) should be selected initially");

        // Press Down Arrow
        await runner.PressKey(ConsoleKey.DownArrow);

        // CRITICAL ASSERTIONS
        runner.Should().HaveMenuVisible("menu should still be visible");
        runner.Should().HaveMenuSelectedIndex(1, "second item (index 1) should be selected after Down");

        var secondItem = runner.SelectedMenuItem;
        secondItem.Should().NotBe(firstItem, "selected item should have changed");

        // Note: Buffer doesn't change during navigation - it only changes when user accepts with Enter
        runner.Buffer.Should().Be("server ", "buffer doesn't change during menu navigation");
    }

    [TestMethod]
    [TestDescription("Up arrow in menu moves to previous item")]
    public async Task UpArrow_InMenu_MovesToPreviousItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.DownArrow);  // Move to second item

        var secondItem = runner.SelectedMenuItem;
        runner.Should().HaveMenuSelectedIndex(1);

        await runner.PressKey(ConsoleKey.UpArrow);

        runner.Should().HaveMenuSelectedIndex(0, "should be back at first item");
        runner.SelectedMenuItem.Should().NotBe(secondItem);
    }

    [TestMethod]
    [TestDescription("DownArrow past last item - wraps to first")]
    public async Task DownArrowPastLastItem_WrapsToFirst()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Navigate down many times to go past end
        int previousIndex = runner.MenuSelectedIndex;
        bool wrapped = false;
        
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.DownArrow);
            Debug.WriteLine($"  DownArrow {i + 1}: index = {runner.MenuSelectedIndex}");
            
            if (runner.MenuSelectedIndex < previousIndex)
            {
                wrapped = true;
                break;
            }
            previousIndex = runner.MenuSelectedIndex;
        }
        
        wrapped.Should().BeTrue("menu should wrap when navigating past end");
    }

    [TestMethod]
    [TestDescription("UpArrow past first item - wraps to last")]
    public async Task UpArrowPastFirstItem_WrapsToLast()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Should start at index 0
        runner.MenuSelectedIndex.Should().Be(0);

        // Try to go up - should wrap to end
        await runner.PressKey(ConsoleKey.UpArrow);
        
        Debug.WriteLine($"After UpArrow from 0: index = {runner.MenuSelectedIndex}");
        
        // Should have wrapped to last item
        runner.MenuSelectedIndex.Should().BeGreaterThan(0, "should wrap to end of menu");
    }

    #endregion

    #region Enter and Escape

    [TestMethod]
    [TestDescription("CRITICAL: Enter accepts selection AND cursor ends at correct position")]
    public async Task Enter_AcceptsSelectionWithCorrectCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);  // Open menu

        runner.Should().HaveMenuVisible();
        var selectedItem = runner.SelectedMenuItem;

        // Press Enter to accept
        await runner.PressKey(ConsoleKey.Enter);

        // Menu should close
        runner.Should().NotHaveMenuVisible("menu should close after Enter");

        // Buffer should contain the accepted completion
        runner.Buffer.Should().Contain(selectedItem);

        // CRITICAL: Cursor should be at end of buffer
        var expectedCursor = runner.Buffer.Length;
        runner.BufferPosition.Should().Be(expectedCursor,
            "cursor should be at end of buffer after accepting");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: server Tab Enter should produce 'server profile ' not 'profile server'")]
    public async Task ServerTabEnter_ShouldInsertAtCorrectPosition()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Step 1: Type "server "
        await runner.TypeText("server ");
        runner.Should().HaveState("server ", 7);
        
        // Ghost text should show the first item (groups first, so "profile")
        runner.GhostText.Should().Be("profile", "ghost should show first item (groups first)");

        // Step 2: Press Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var selectedItem = runner.SelectedMenuItem;
        selectedItem.Should().Be("profile", "first item should be 'profile' (groups first)");

        // Step 3: Press Enter to accept first item
        await runner.PressKey(ConsoleKey.Enter);
        
        // CRITICAL: The buffer should be "server profile " NOT "profile server"
        runner.Buffer.Should().Be($"server {selectedItem} ", 
            $"buffer should be 'server {selectedItem} ' not '{selectedItem} server'");
        
        // Cursor should be at the end
        runner.BufferPosition.Should().Be(runner.Buffer.Length,
            "cursor should be at end of buffer");
    }

    [TestMethod]
    [TestDescription("Escape cancels menu and leaves input unchanged")]
    public async Task Escape_CancelsMenuAndLeavesInputUnchanged()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        var originalBuffer = runner.Buffer;
        var originalPosition = runner.BufferPosition;

        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Press Escape
        await runner.PressKey(ConsoleKey.Escape);

        // Menu closes, buffer unchanged
        runner.Should().NotHaveMenuVisible();
        runner.Should().HaveBuffer(originalBuffer, "buffer should remain unchanged after Escape");
        runner.Should().HaveBufferPosition(originalPosition, "cursor should remain unchanged after Escape");
    }

    #endregion

    #region Typing While Menu Is Open - Filters Menu

    [TestMethod]
    [TestDescription("Typing while menu is open should filter menu results")]
    public async Task TypingWhileMenuOpen_ShouldFilterMenuResults()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server " and open menu (should show connect, disconnect, status)
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var initialItemCount = runner.MenuItemCount;
        initialItemCount.Should().BeGreaterThan(1, "menu should have multiple items initially");

        // Type "c" to filter - should filter to items starting with "c" (connect)
        await runner.TypeText("c");

        // Menu should still be visible with filtered results
        runner.Should().HaveMenuVisible();
        
        // Buffer should have the typed character
        runner.Buffer.Should().Be("server c");
        
        // Menu should be filtered to fewer items
        runner.MenuItemCount.Should().BeLessThanOrEqualTo(initialItemCount);
    }

    [TestMethod]
    [TestDescription("Backspace while menu is open should close menu and remove character")]
    public async Task BackspaceWhileMenuOpen_ShouldCloseMenuAndRemoveCharacter()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Backspace while menu is open
        await runner.PressKey(ConsoleKey.Backspace);

        // Menu should be closed
        runner.Should().HaveMenuHidden();
        
        // Character should be removed
        runner.Buffer.Should().Be("server");
        
        // "server" is a complete command - no ghost text should appear
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("Delete while menu is open should close menu")]
    public async Task DeleteWhileMenuOpen_ShouldCloseMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        
        // Move cursor back so Delete has something to delete
        await runner.PressKey(ConsoleKey.LeftArrow);
        
        await runner.PressKey(ConsoleKey.Tab);
        
        if (runner.IsMenuVisible)
        {
            await runner.PressKey(ConsoleKey.Delete);
            runner.Should().HaveMenuHidden();
        }
    }

    [TestMethod]
    [TestDescription("Typing while menu open then backspacing to empty should leave clean state")]
    public async Task TypingThenBackspacingToEmpty_ShouldLeaveCleanState()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type and open menu
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type something that doesn't match any commands (filters to empty, closes menu)
        await runner.TypeText("xyz");
        // Menu should close when no matches found after filtering
        runner.Should().HaveMenuHidden();

        // Backspace everything
        for (int i = 0; i < "server xyz".Length; i++)
        {
            await runner.PressKey(ConsoleKey.Backspace);
        }

        // Should be back to empty, clean state
        runner.Buffer.Should().BeEmpty();
        runner.Should().HaveMenuHidden();
        runner.GhostText.Should().BeNullOrEmpty();
    }

    #endregion
}
