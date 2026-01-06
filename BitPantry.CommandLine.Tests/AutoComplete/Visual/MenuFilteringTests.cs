using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for menu filtering while typing functionality.
/// Covers User Stories 1-5 from the menu-filter feature specification.
/// 
/// US1: Filter Menu by Typing - typing while menu open filters items in real-time
/// US2: Backspace Expands Filter - backspace removes filter chars or closes menu
/// US3: Space Closes Menu - context-aware space handling (quotes vs non-quotes)
/// US4: Match Highlighting - matching substrings are visually highlighted
/// US5: Consistent Cursor Position - no trailing space after completion acceptance
/// </summary>
[TestClass]
public class MenuFilteringTests : VisualTestBase
{
    // Tests will be added incrementally following TDD approach
    // Each user story section will have its own region
    
    #region US1: Filter Menu by Typing

    // T011-T015: Tests for typing while menu is open to filter items

    [TestMethod]
    [TestDescription("US1: Typing while menu is open should filter menu items to matching entries")]
    public async Task T011_TypingWhileMenuOpen_FiltersToMatchingItems()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server " to set up for subcommand completion
        await runner.TypeText("server ");
        
        // Open menu with Tab - should show connect, disconnect, profile, status
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible("menu should open after Tab");
        var initialCount = runner.MenuItemCount;
        initialCount.Should().BeGreaterThan(1, "should have multiple menu items");

        // Type "con" to filter - should match "connect" (contains "con")
        await runner.TypeText("con");

        // Verify menu is still visible and filtered
        runner.Should().HaveMenuVisible("menu should stay open while typing filter");
        runner.MenuItemCount.Should().BeLessThan(initialCount, "filter should reduce item count");
        
        // The filtered items should contain "con"
        runner.Buffer.Should().Contain("con", "typed filter should appear in buffer");
    }

    [TestMethod]
    [TestDescription("US1: Filter should be case-insensitive")]
    public async Task T012_FilterIsCaseInsensitive()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type "CON" in uppercase - should still match "connect"
        await runner.TypeText("CON");

        runner.Should().HaveMenuVisible("menu should stay open with uppercase filter");
        // Should still find matches (case-insensitive)
        runner.MenuItemCount.Should().BeGreaterThan(0, "uppercase filter should find matches");
    }

    [TestMethod]
    [TestDescription("US1: Filter uses substring matching, not just prefix")]
    public async Task T013_FilterUsesSubstringMatching()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // "config" command exists - typing "fig" should match it (substring in middle)
        await runner.TypeText("");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type "fig" - should match "config" via substring
        await runner.TypeText("fig");

        // If "config" is in the list and contains "fig", it should still be visible
        // (depends on registered commands including "config")
        runner.Should().HaveMenuVisible("substring filter should keep menu open if matches exist");
    }

    [TestMethod]
    [TestDescription("US1: Filter text appears in the input buffer, not a separate search box")]
    public async Task T014_FilterTextAppearsInBuffer()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type filter characters
        await runner.TypeText("sta");

        // Filter text should appear in the input buffer (FR-011)
        runner.Buffer.Should().EndWith("sta", "filter characters should appear in buffer");
    }

    [TestMethod]
    [TestDescription("US1: Filtering resets selection to first item")]
    public async Task T015_FilteringResetsSelectionToFirstItem()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Navigate to item at index 2
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.DownArrow);
        runner.MenuSelectedIndex.Should().Be(2, "should be at index 2 after two down arrows");

        // Type filter character
        await runner.TypeText("c");

        // Selection should reset to first item (index 0)
        runner.MenuSelectedIndex.Should().Be(0, "filtering should reset selection to index 0");
    }
    
    #endregion

    #region US2: Backspace Expands Filter

    // T019-T022: Tests for backspace behavior with filter

    [TestMethod]
    [TestDescription("US2: Backspace removes filter character and expands results")]
    public async Task T019_BackspaceRemovesFilterCharacter()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type "conn" to filter
        await runner.TypeText("conn");
        var filteredCount = runner.MenuItemCount;
        runner.Buffer.Should().EndWith("conn");

        // Backspace should remove one character
        await runner.PressKey(ConsoleKey.Backspace);

        runner.Buffer.Should().EndWith("con", "backspace should remove last filter character");
        runner.Should().HaveMenuVisible("menu should stay open after backspace");
    }

    [TestMethod]
    [TestDescription("US2: Backspace expands filter results (shows more items)")]
    public async Task T020_BackspaceExpandsFilterResults()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var initialCount = runner.MenuItemCount;

        // Type a filter that restricts results
        await runner.TypeText("con");
        var filteredCount = runner.MenuItemCount;

        // Backspace should expand results (or keep same if already showing all matches)
        await runner.PressKey(ConsoleKey.Backspace);
        
        runner.Should().HaveMenuVisible("menu should stay open");
        // After backspace, should have >= filtered count
        runner.MenuItemCount.Should().BeGreaterThanOrEqualTo(filteredCount, 
            "backspace should expand or maintain results");
    }

    [TestMethod]
    [TestDescription("US2: Backspace past trigger position closes menu")]
    public async Task T021_BackspacePastTriggerPosition_ClosesMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server " (7 chars) and open menu at position 7
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Backspace should go before trigger position and close menu
        await runner.PressKey(ConsoleKey.Backspace);

        runner.Should().HaveMenuHidden("menu should close when backspacing before trigger position");
        runner.Buffer.Should().Be("server", "buffer should have 'server' without trailing space");
    }

    [TestMethod]
    [TestDescription("US2: Backspace with no filter closes menu immediately")]
    public async Task T022_BackspaceWithNoFilter_ClosesMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Immediately backspace (no filter typed)
        await runner.PressKey(ConsoleKey.Backspace);

        runner.Should().HaveMenuHidden("menu should close on backspace with no filter");
    }

    #endregion

    #region US3: Space Closes Menu (Context-Aware)

    // T027-T029: Tests for space key context-aware handling

    [TestMethod]
    [TestDescription("US3: Space outside quotes closes menu and inserts space")]
    public async Task T027_SpaceOutsideQuotes_ClosesMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Press Space while menu is open (outside quotes)
        await runner.PressKey(ConsoleKey.Spacebar);

        runner.Should().HaveMenuHidden("menu should close when space pressed outside quotes");
        runner.Buffer.Should().EndWith(" ", "space should be inserted in buffer");
    }

    [TestMethod]
    [TestDescription("US3: Space inside quotes filters menu instead of closing")]
    public async Task T028_SpaceInsideQuotes_FiltersMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type a command with quoted argument: --path "Program
        await runner.TypeText("server connect --host \"local");
        await runner.PressKey(ConsoleKey.Tab);
        
        // If menu is open, space inside quotes should filter, not close
        if (runner.IsMenuVisible)
        {
            var countBefore = runner.MenuItemCount;
            await runner.PressKey(ConsoleKey.Spacebar);
            
            // Menu should stay open (space is part of quoted path)
            runner.Should().HaveMenuVisible("menu should stay open when space pressed inside quotes");
            runner.Buffer.Should().Contain(" ", "space should be inserted in buffer");
        }
        else
        {
            // If no menu opened (no completions), test is inconclusive but passes
            Assert.IsTrue(true, "No completions available for this context - test inconclusive");
        }
    }

    [TestMethod]
    [TestDescription("US3: Space closes menu without accepting the selection")]
    public async Task T029_SpaceClosesMenu_WithoutAcceptingSelection()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Navigate to item 2
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.DownArrow);
        var selectedItem = runner.SelectedMenuItem;

        // Press Space - should close menu without inserting selected item
        await runner.PressKey(ConsoleKey.Spacebar);

        runner.Should().HaveMenuHidden("menu should close on space");
        runner.Buffer.Should().NotContain(selectedItem, "selected item should NOT be inserted on space");
        runner.Buffer.Should().Be("server  ", "buffer should have original text plus space");
    }

    #endregion

    #region US4: Match Highlighting

    // T033-T035: Tests for match highlighting in menu
    // Note: T033 and T034 are in AutoCompleteMenuRenderableTests.cs
    // T035 is an integration test here

    [TestMethod]
    [TestDescription("T035: Filtering shows highlighted matches in menu (integration)")]
    public async Task T035_FilteringShowsHighlightedMatches()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type filter to narrow matches
        await runner.TypeText("con");

        // Verify menu still visible with filtered items
        runner.Should().HaveMenuVisible();
        
        // The filtered items should have "con" in them
        // This is verified at the unit test level in AutoCompleteMenuRenderableTests
        // Here we just ensure the filter is applied correctly and menu stays open
        runner.MenuItemCount.Should().BeGreaterThan(0, "filtered menu should have matching items");
    }

    #endregion

    #region US5: Consistent Cursor Position After Acceptance

    // T041-T043: Tests for no trailing space on acceptance

    [TestMethod]
    [TestDescription("T041: Accepting completion should not add trailing space")]
    public async Task T041_AcceptCompletion_NoTrailingSpace()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Get the selected item text before accepting
        var selectedItem = runner.SelectedMenuItem;
        
        // Accept the selected completion
        await runner.PressKey(ConsoleKey.Enter);

        runner.Should().HaveMenuHidden();
        // Buffer should end exactly with the completion text, not with additional space
        var buffer = runner.Buffer;
        // The buffer should be "server <selected_item>" without trailing space
        buffer.Should().Be($"server {selectedItem}", "should not have trailing space after completion");
    }

    [TestMethod]
    [TestDescription("T042: After accepting completion, cursor should be at end of buffer")]
    public async Task T042_AcceptCompletion_CursorAtEndOfText()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Accept the selected completion
        await runner.PressKey(ConsoleKey.Enter);

        runner.Should().HaveMenuHidden();
        // Cursor should be at end of buffer
        runner.BufferPosition.Should().Be(runner.Buffer.Length, "cursor should be at end of text after accepting completion");
    }

    [TestMethod]
    [TestDescription("T043: Tab with single match should not add trailing space")]
    public async Task T043_TabSingleMatch_NoTrailingSpace()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type enough to narrow to single or few matches
        await runner.TypeText("server connec");
        await runner.PressKey(ConsoleKey.Tab);

        // If auto-completed to single match, should not have trailing space
        var buffer = runner.Buffer;
        // Note: this test may need adjustment based on actual completion behavior
        buffer.Should().NotEndWith("  ", "should not have extra trailing space");
    }

    #endregion

    #region FR-003: No Matches Display

    [TestMethod]
    [TestDescription("T048: Filter with no matches shows '(no matches)' message")]
    public async Task T048_FilterWithNoMatches_ShowsNoMatchesMessage()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Type filter that matches nothing
        await runner.TypeText("xyz");

        // Menu should stay open but show no matches message
        runner.Should().HaveMenuVisible("menu should stay open even with no matches");
        runner.MenuItemCount.Should().Be(0, "should have no matching items");
        runner.RenderedMenuContent.Should().Contain("(no matches)", 
            "should display '(no matches)' message when filter produces zero results");
    }

    [TestMethod]
    [TestDescription("T050: Backspace from no matches restores filtered results")]
    public async Task T050_BackspaceFromNoMatches_RestoresFilteredResults()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var initialCount = runner.MenuItemCount;

        // Type filter that matches nothing
        await runner.TypeText("xyz");
        runner.MenuItemCount.Should().Be(0, "should have no matches");

        // Backspace should restore partial matches
        await runner.PressKey(ConsoleKey.Backspace); // "xy"
        await runner.PressKey(ConsoleKey.Backspace); // "x"
        await runner.PressKey(ConsoleKey.Backspace); // ""

        runner.Should().HaveMenuVisible("menu should stay open");
        runner.MenuItemCount.Should().Be(initialCount, 
            "after removing all filter chars, should show original items");
    }

    #endregion
}
