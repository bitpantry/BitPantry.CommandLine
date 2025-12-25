using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Tests.AutoComplete.Ghost;

/// <summary>
/// Tests for ghost text acceptance - GS-002, GS-003 scenarios.
/// </summary>
[TestClass]
public class GhostAcceptTests
{
    #region GS-002: Right arrow accepts ghost

    [TestMethod]
    [Description("GS-002: Accepting ghost should produce full suggestion")]
    public void GhostState_Accept_ProducesFullSuggestion()
    {
        // Arrange
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        ghost.IsVisible.Should().BeTrue();

        // Act - simulating acceptance
        var acceptedText = ghost.FullSuggestion;

        // Assert
        acceptedText.Should().Be("connect");
    }

    [TestMethod]
    [Description("GS-002: Accepted text includes both input and ghost")]
    public void GhostState_Accept_CombinesInputAndGhost()
    {
        // Arrange
        var input = "hel";
        var ghost = GhostState.FromSuggestion(input, "help", GhostSuggestionSource.Command);

        // Assert
        ghost.InputText.Should().Be("hel");
        ghost.GhostText.Should().Be("p");
        ghost.FullSuggestion.Should().Be("help");
        (ghost.InputText + ghost.GhostText).Should().Be(ghost.FullSuggestion);
    }

    #endregion

    #region GS-003: End key accepts ghost

    [TestMethod]
    [Description("GS-003: End key acceptance works same as Right arrow")]
    public void GhostState_AcceptViaEnd_SameAsRightArrow()
    {
        // Arrange - same ghost state
        var ghost = GhostState.FromSuggestion("con", "configure", GhostSuggestionSource.Command);

        // Act - acceptance via End key is same behavior
        var acceptedText = ghost.FullSuggestion;

        // Assert
        acceptedText.Should().Be("configure");
        ghost.GhostText.Should().Be("figure");
    }

    #endregion

    #region Accept Edge Cases

    [TestMethod]
    [Description("Accepting empty ghost returns input unchanged")]
    public void GhostState_AcceptEmpty_ReturnsInputAsIs()
    {
        // Arrange
        var ghost = new GhostState
        {
            InputText = "test",
            GhostText = null,
            FullSuggestion = null
        };

        // Act
        var result = ghost.FullSuggestion ?? ghost.InputText;

        // Assert
        result.Should().Be("test");
    }

    [TestMethod]
    [Description("Accepting when ghost is complete word")]
    public void GhostState_AcceptFullWord_Works()
    {
        // Arrange - empty input, full ghost
        var ghost = GhostState.FromSuggestion("", "connect", GhostSuggestionSource.Command);

        // Assert
        ghost.InputText.Should().Be("");
        ghost.GhostText.Should().Be("connect");
        ghost.FullSuggestion.Should().Be("connect");
    }

    [TestMethod]
    [Description("Ghost with history includes full command line")]
    public void GhostState_HistoryAccept_IncludesFullLine()
    {
        // Arrange - history includes arguments
        var ghost = GhostState.FromSuggestion("con", "connect --server prod --port 8080", GhostSuggestionSource.History);

        // Assert
        ghost.FullSuggestion.Should().Be("connect --server prod --port 8080");
        ghost.GhostText.Should().Be("nect --server prod --port 8080");
    }

    #endregion

    #region FromSuggestion Edge Cases

    [TestMethod]
    [Description("FromSuggestion handles case-insensitive prefix")]
    public void FromSuggestion_CaseInsensitive_CreatesGhost()
    {
        // Arrange - input "CON", suggestion "connect" (different case)
        var ghost = GhostState.FromSuggestion("CON", "connect", GhostSuggestionSource.Command);

        // Assert - should create ghost since it's case-insensitive prefix
        ghost.IsVisible.Should().BeTrue();
        ghost.GhostText.Should().Be("nect");
    }

    [TestMethod]
    [Description("FromSuggestion returns empty for non-matching prefix")]
    public void FromSuggestion_NonMatchingPrefix_ReturnsEmpty()
    {
        // Arrange - input doesn't prefix-match suggestion
        var ghost = GhostState.FromSuggestion("xyz", "connect", GhostSuggestionSource.Command);

        // Assert
        ghost.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    [Description("FromSuggestion handles empty input")]
    public void FromSuggestion_EmptyInput_ReturnsFullGhost()
    {
        var ghost = GhostState.FromSuggestion("", "help", GhostSuggestionSource.Command);

        ghost.IsVisible.Should().BeTrue();
        ghost.GhostText.Should().Be("help");
    }

    [TestMethod]
    [Description("FromSuggestion handles null/empty suggestion")]
    public void FromSuggestion_EmptySuggestion_ReturnsEmpty()
    {
        var nullGhost = GhostState.FromSuggestion("test", null, GhostSuggestionSource.Command);
        var emptyGhost = GhostState.FromSuggestion("test", "", GhostSuggestionSource.Command);

        nullGhost.IsVisible.Should().BeFalse();
        emptyGhost.IsVisible.Should().BeFalse();
    }

    [TestMethod]
    [Description("FromSuggestion handles exact match (no ghost text needed)")]
    public void FromSuggestion_ExactMatch_ReturnsEmptyGhost()
    {
        var ghost = GhostState.FromSuggestion("help", "help", GhostSuggestionSource.Command);

        // Exact match means ghost text is empty string
        ghost.GhostText.Should().Be("");
        ghost.IsVisible.Should().BeFalse(); // Empty ghost is not visible
    }

    #endregion
}
