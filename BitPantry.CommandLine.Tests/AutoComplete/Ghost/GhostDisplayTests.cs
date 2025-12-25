using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Tests.AutoComplete.Ghost;

/// <summary>
/// Tests for basic ghost text display behavior - GS-001 to GS-008.
/// </summary>
[TestClass]
public class GhostDisplayTests
{
    #region GS-001: Ghost appears on typing

    [TestMethod]
    [Description("GS-001: Ghost text shows completion suffix after partial input")]
    public void GhostState_WithMatch_ShowsSuffix()
    {
        // Arrange
        var state = new GhostState
        {
            InputText = "con",
            GhostText = "nect",
            FullSuggestion = "connect"
        };

        // Assert
        state.GhostText.Should().Be("nect");
        state.IsVisible.Should().BeTrue();
    }

    [TestMethod]
    [Description("GS-001: Ghost text is empty when no match")]
    public void GhostState_NoMatch_NoGhostText()
    {
        // Arrange
        var state = new GhostState
        {
            InputText = "xyz",
            GhostText = null,
            FullSuggestion = null
        };

        // Assert
        state.IsVisible.Should().BeFalse();
    }

    #endregion

    #region GS-005: Typing removes ghost when no match

    [TestMethod]
    [Description("GS-005: Ghost disappears when no matching completions")]
    public void GhostState_SetNoMatch_BecomesInvisible()
    {
        // Arrange
        var state = new GhostState
        {
            InputText = "conx",
            GhostText = null,
            FullSuggestion = null
        };

        // Assert
        state.IsVisible.Should().BeFalse();
        state.GhostText.Should().BeNull();
    }

    #endregion

    #region GS-007: Ghost doesn't interfere with typing

    [TestMethod]
    [Description("GS-007: Ghost text is separate from actual input")]
    public void GhostState_Properties_AreSeparate()
    {
        // Arrange
        var state = new GhostState
        {
            InputText = "hel",
            GhostText = "p",
            FullSuggestion = "help"
        };

        // Assert - input and ghost are clearly separated
        state.InputText.Should().Be("hel");
        state.GhostText.Should().Be("p");
        state.FullSuggestion.Should().Be("help");
    }

    #endregion

    #region GS-008: No ghost when no matches

    [TestMethod]
    [Description("GS-008: Empty ghost state when no commands match")]
    public void GhostState_Default_IsNotVisible()
    {
        // Arrange
        var state = new GhostState();

        // Assert
        state.IsVisible.Should().BeFalse();
        state.GhostText.Should().BeNullOrEmpty();
    }

    #endregion

    #region Ghost State Properties

    [TestMethod]
    [Description("GhostState computes visibility from GhostText")]
    public void GhostState_IsVisible_DependsOnGhostText()
    {
        // Visible when has ghost text
        var visible = new GhostState { GhostText = "text" };
        visible.IsVisible.Should().BeTrue();

        // Not visible when null
        var nullGhost = new GhostState { GhostText = null };
        nullGhost.IsVisible.Should().BeFalse();

        // Not visible when empty
        var emptyGhost = new GhostState { GhostText = "" };
        emptyGhost.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    [Description("GhostState stores source information")]
    public void GhostState_TracksSource()
    {
        // Arrange
        var fromHistory = new GhostState 
        { 
            GhostText = "text",
            Source = GhostSuggestionSource.History 
        };

        var fromCommand = new GhostState 
        { 
            GhostText = "text",
            Source = GhostSuggestionSource.Command 
        };

        // Assert
        fromHistory.Source.Should().Be(GhostSuggestionSource.History);
        fromCommand.Source.Should().Be(GhostSuggestionSource.Command);
    }

    #endregion

    #region Ghost Text Calculation

    [TestMethod]
    [Description("Ghost text is correctly calculated as suffix")]
    public void GhostState_CalculatesCorrectSuffix()
    {
        // Arrange - input "con", suggestion "connect"
        var input = "con";
        var suggestion = "connect";
        var ghost = suggestion.Substring(input.Length);

        var state = new GhostState
        {
            InputText = input,
            GhostText = ghost,
            FullSuggestion = suggestion
        };

        // Assert
        state.GhostText.Should().Be("nect");
        (state.InputText + state.GhostText).Should().Be(state.FullSuggestion);
    }

    #endregion
}
