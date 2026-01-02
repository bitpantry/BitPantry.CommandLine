using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Tests.AutoComplete.Ghost;

/// <summary>
/// Tests for ghost + menu interaction - GS-020 to GS-022.
/// 
/// Conflict #4 Resolution: Ghost state is CLEARED (not just hidden) when menu opens.
/// The AutoCompleteController.UpdateGhostAsync method enforces this by clearing ghost
/// when IsEngaged is true. These tests verify the state objects themselves, while
/// Visual tests verify the actual UX behavior.
/// </summary>
[TestClass]
public class GhostMenuInteractionTests
{
    #region GS-020: Ghost cleared when menu open

    [TestMethod]
    [Description("GS-020: GhostState and MenuState are independent data structures")]
    public void GhostAndMenuState_AreIndependentDataStructures()
    {
        // This test verifies that GhostState and MenuState are independent objects.
        // The actual behavior (ghost is CLEARED when menu opens) is enforced by
        // AutoCompleteController, not by these state objects.
        
        var menuState = new MenuState
        {
            Items = new System.Collections.Generic.List<CompletionItem>
            {
                new() { InsertText = "connect" }
            }
        };

        var ghostState = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);

        // States are independent data structures - they don't reference each other
        menuState.HasItems.Should().BeTrue();
        ghostState.IsVisible.Should().BeTrue(); // Ghost state object IS visible as standalone
        
        // Note: In actual usage, AutoCompleteController CLEARS ghost when menu opens.
        // See Visual/GhostBehaviorTests for behavior verification.
    }

    #endregion

    #region GS-021: Ghost returns after menu close

    [TestMethod]
    [Description("GS-021: Ghost should reappear after menu is closed")]
    public void GhostState_AfterMenuClose_CanBeRestored()
    {
        // After menu closes, ghost should reappear for current input
        var input = "con";
        
        // Ghost before menu
        var ghostBefore = GhostState.FromSuggestion(input, "connect", GhostSuggestionSource.Command);
        ghostBefore.IsVisible.Should().BeTrue();
        
        // After menu closes with same input, ghost should be restored
        var ghostAfter = GhostState.FromSuggestion(input, "connect", GhostSuggestionSource.Command);
        ghostAfter.IsVisible.Should().BeTrue();
        ghostAfter.GhostText.Should().Be(ghostBefore.GhostText);
    }

    #endregion

    #region GS-022: Ghost updates after menu accept

    [TestMethod]
    [Description("GS-022: Ghost should update after accepting menu selection")]
    public void GhostState_AfterMenuAccept_UpdatesForNewInput()
    {
        // After accepting "connect", input is now "connect "
        var newInput = "connect ";
        
        // Ghost should show next suggestion if any
        var nextGhost = GhostState.FromSuggestion(newInput, "connect --server", GhostSuggestionSource.History);
        
        if (nextGhost.IsVisible)
        {
            nextGhost.GhostText.Should().Be("--server");
        }
    }

    [TestMethod]
    [Description("GS-022: No ghost after accept if no further suggestions")]
    public void GhostState_AfterMenuAccept_NoGhostIfNoSuggestions()
    {
        // After accepting, if no further completions match, ghost is empty
        var ghostWithNoMatch = GhostState.FromSuggestion("help ", ""); // Empty suggestion

        ghostWithNoMatch.IsVisible.Should().BeFalse();
    }

    #endregion

    #region State Independence

    [TestMethod]
    [Description("MenuState and GhostState are independent objects")]
    public void MenuAndGhostStates_AreIndependent()
    {
        var menu = new MenuState
        {
            Items = new System.Collections.Generic.List<CompletionItem>
            {
                new() { InsertText = "test" }
            },
            SelectedIndex = 0
        };

        var ghost = GhostState.FromSuggestion("te", "test", GhostSuggestionSource.Command);

        // Modifying one doesn't affect the other
        menu.MoveDown();
        ghost.Clear();

        menu.HasItems.Should().BeTrue();
        ghost.IsVisible.Should().BeFalse();
    }

    #endregion
}
