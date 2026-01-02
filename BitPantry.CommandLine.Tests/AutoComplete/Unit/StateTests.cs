using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using System.Collections.Generic;

namespace BitPantry.CommandLine.Tests.AutoComplete.Unit;

[TestClass]
public class MenuStateTests
{
    [TestMethod]
    public void MenuState_DefaultValues_ShouldBeInitialized()
    {
        // Arrange & Act
        var state = new MenuState();

        // Assert
        state.Items.Should().BeEmpty();
        state.SelectedIndex.Should().Be(0);
        state.FilterText.Should().BeEmpty();
        state.IsLoading.Should().BeFalse();
        state.ErrorMessage.Should().BeNull();
        state.TotalCount.Should().Be(0);
        state.MaxVisibleItems.Should().Be(10);
    }

    [TestMethod]
    public void MenuState_SelectedItem_WithValidIndex_ShouldReturnItem()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" }
        };
        var state = new MenuState { Items = items, SelectedIndex = 1 };

        // Assert
        state.SelectedItem.Should().NotBeNull();
        state.SelectedItem!.InsertText.Should().Be("item2");
    }

    [TestMethod]
    public void MenuState_SelectedItem_WithInvalidIndex_ShouldReturnNull()
    {
        // Arrange
        var state = new MenuState { Items = [], SelectedIndex = 0 };

        // Assert
        state.SelectedItem.Should().BeNull();
    }

    [TestMethod]
    public void MenuState_HasItems_WithItems_ShouldReturnTrue()
    {
        // Arrange
        var state = new MenuState
        {
            Items = [new CompletionItem { InsertText = "item" }]
        };

        // Assert
        state.HasItems.Should().BeTrue();
    }

    [TestMethod]
    public void MenuState_HasItems_Empty_ShouldReturnFalse()
    {
        // Arrange
        var state = new MenuState { Items = [] };

        // Assert
        state.HasItems.Should().BeFalse();
    }

    [TestMethod]
    public void MenuState_MoveUp_ShouldDecrementIndex()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var state = new MenuState { Items = items, SelectedIndex = 1 };

        // Act
        state.MoveUp();

        // Assert
        state.SelectedIndex.Should().Be(0);
    }

    [TestMethod]
    public void MenuState_MoveUp_AtTop_ShouldWrapToBottom()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var state = new MenuState { Items = items, SelectedIndex = 0 };

        // Act
        state.MoveUp();

        // Assert
        state.SelectedIndex.Should().Be(2); // Wrapped to last item
    }

    [TestMethod]
    public void MenuState_MoveDown_ShouldIncrementIndex()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var state = new MenuState { Items = items, SelectedIndex = 0 };

        // Act
        state.MoveDown();

        // Assert
        state.SelectedIndex.Should().Be(1);
    }

    [TestMethod]
    public void MenuState_MoveDown_AtBottom_ShouldWrapToTop()
    {
        // Arrange
        var items = new List<CompletionItem>
        {
            new() { InsertText = "item1" },
            new() { InsertText = "item2" },
            new() { InsertText = "item3" }
        };
        var state = new MenuState { Items = items, SelectedIndex = 2 };

        // Act
        state.MoveDown();

        // Assert
        state.SelectedIndex.Should().Be(0); // Wrapped to first item
    }

    [TestMethod]
    public void MenuState_MoveUp_EmptyList_ShouldNotThrow()
    {
        // Arrange
        var state = new MenuState { Items = [] };

        // Act & Assert
        var act = () => state.MoveUp();
        act.Should().NotThrow();
        state.SelectedIndex.Should().Be(0);
    }
}

[TestClass]
public class GhostStateTests
{
    [TestMethod]
    public void GhostState_Empty_ShouldHaveNoText()
    {
        // Act
        var state = GhostState.Empty;

        // Assert
        state.Text.Should().BeNull();
        state.StartColumn.Should().Be(0);
        state.SourceItem.Should().BeNull();
        state.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void GhostState_Create_ShouldSetProperties()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "command" };

        // Act
        var state = GhostState.Create("mand", 3, item);

        // Assert
        state.Text.Should().Be("mand");
        state.StartColumn.Should().Be(3);
        state.SourceItem.Should().Be(item);
        state.IsActive.Should().BeTrue();
    }

    [TestMethod]
    public void GhostState_Clear_ShouldResetAllProperties()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "command" };
        var state = GhostState.Create("mand", 3, item);

        // Act
        state.Clear();

        // Assert
        state.Text.Should().BeNull();
        state.StartColumn.Should().Be(0);
        state.SourceItem.Should().BeNull();
        state.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void GhostState_IsActive_EmptyString_ShouldBeFalse()
    {
        // Arrange
        var state = new GhostState { Text = "" };

        // Assert
        state.IsActive.Should().BeFalse();
    }

    [TestMethod]
    public void GhostState_IsActive_WithText_ShouldBeTrue()
    {
        // Arrange
        var state = new GhostState { Text = "completion" };

        // Assert
        state.IsActive.Should().BeTrue();
    }
}

[TestClass]
public class CompletionActionTests
{
    [TestMethod]
    public void CompletionAction_None_ShouldHaveCorrectProperties()
    {
        // Act
        var action = CompletionAction.None();

        // Assert
        action.Type.Should().Be(CompletionActionType.None);
        action.InsertText.Should().BeNull();
        action.RequiresMenuRedraw.Should().BeFalse();
        action.RequiresInputRedraw.Should().BeFalse();
    }

    [TestMethod]
    public void CompletionAction_Close_ShouldRequireRedraw()
    {
        // Act
        var action = CompletionAction.Close();

        // Assert
        action.Type.Should().Be(CompletionActionType.CloseMenu);
        action.RequiresMenuRedraw.Should().BeTrue();
    }

    [TestMethod]
    public void CompletionAction_Accept_ShouldContainText()
    {
        // Act
        var action = CompletionAction.Accept("test-value");

        // Assert
        action.Type.Should().Be(CompletionActionType.InsertText);
        action.InsertText.Should().Be("test-value");
        action.RequiresMenuRedraw.Should().BeTrue();
        action.RequiresInputRedraw.Should().BeTrue();
    }

    [TestMethod]
    public void CompletionAction_UpdateMenu_ShouldRequireMenuRedraw()
    {
        // Arrange
        var menuState = new MenuState { Items = new List<CompletionItem>() };

        // Act
        var action = CompletionAction.UpdateMenu(menuState);

        // Assert
        action.Type.Should().Be(CompletionActionType.SelectionChanged);
        action.RequiresMenuRedraw.Should().BeTrue();
        action.MenuState.Should().Be(menuState);
    }

    [TestMethod]
    public void CompletionAction_Error_ShouldContainMessage()
    {
        // Act
        var action = CompletionAction.Error("Something went wrong");

        // Assert
        action.Type.Should().Be(CompletionActionType.Error);
        action.ErrorMessage.Should().Be("Something went wrong");
    }

    [TestMethod]
    public void CompletionAction_Loading_ShouldHaveCorrectType()
    {
        // Act
        var action = CompletionAction.Loading();

        // Assert
        action.Type.Should().Be(CompletionActionType.Loading);
    }

    [TestMethod]
    public void CompletionAction_NoMatches_ShouldHaveCorrectType()
    {
        // Act
        var action = CompletionAction.NoMatches();

        // Assert
        action.Type.Should().Be(CompletionActionType.NoMatches);
    }

    [TestMethod]
    public void CompletionAction_ShowMenu_ShouldRequireMenuRedraw()
    {
        // Arrange
        var menuState = new MenuState { Items = new List<CompletionItem>() };

        // Act
        var action = CompletionAction.ShowMenu(menuState);

        // Assert
        action.Type.Should().Be(CompletionActionType.OpenMenu);
        action.RequiresMenuRedraw.Should().BeTrue();
        action.MenuState.Should().Be(menuState);
    }
}

[TestClass]
public class MatchResultTests
{
    [TestMethod]
    public void MatchResult_NoMatch_ShouldHaveZeroScore()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "test" };

        // Act
        var result = MatchResult.NoMatch(item);

        // Assert
        result.Score.Should().Be(0);
        result.Mode.Should().Be(MatchMode.None);
        result.IsExactMatch.Should().BeFalse();
        result.IsPrefixMatch.Should().BeFalse();
    }

    [TestMethod]
    public void MatchResult_Exact_ShouldHaveFullScore()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "test" };

        // Act
        var result = MatchResult.Exact(item);

        // Assert
        result.Score.Should().Be(1.0);
        result.Mode.Should().Be(MatchMode.Exact);
        result.IsExactMatch.Should().BeTrue();
        result.MatchRanges.Should().ContainSingle()
            .Which.Should().Be((0, 4));
    }

    [TestMethod]
    public void MatchResult_Prefix_CaseSensitive_ShouldHaveProportionalScore()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "testing" };

        // Act
        var result = MatchResult.Prefix(item, 4, caseSensitive: true);

        // Assert
        result.Score.Should().BeApproximately(4.0 / 7.0, 0.01);
        result.Mode.Should().Be(MatchMode.Prefix);
        result.IsPrefixMatch.Should().BeTrue();
        result.IsCaseInsensitive.Should().BeFalse();
    }

    [TestMethod]
    public void MatchResult_Prefix_CaseInsensitive_ShouldHaveCorrectMode()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "testing" };

        // Act
        var result = MatchResult.Prefix(item, 4, caseSensitive: false);

        // Assert
        result.Mode.Should().Be(MatchMode.PrefixCaseInsensitive);
        result.IsCaseInsensitive.Should().BeTrue();
    }

    [TestMethod]
    public void MatchResult_Contains_ShouldHaveLowerScore()
    {
        // Arrange
        var item = new CompletionItem { InsertText = "testing" };

        // Act
        var result = MatchResult.Contains(item, 2, 3, caseSensitive: true);

        // Assert
        result.Score.Should().BeLessThan(0.5); // Contains is scored at 50% of prefix
        result.Mode.Should().Be(MatchMode.Contains);
        result.MatchRanges.Should().ContainSingle()
            .Which.Should().Be((2, 3));
    }
}
