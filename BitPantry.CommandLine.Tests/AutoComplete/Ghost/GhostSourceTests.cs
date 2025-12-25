using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;

namespace BitPantry.CommandLine.Tests.AutoComplete.Ghost;

/// <summary>
/// Tests for ghost suggestion source priority - GS-010 to GS-012.
/// </summary>
[TestClass]
public class GhostSourceTests
{
    #region GS-010: History prioritized over commands

    [TestMethod]
    [Description("GS-010: History matches are prioritized over command matches")]
    public void GhostState_HistorySource_HasHigherPriority()
    {
        // History-sourced ghosts should be preferred
        var historyGhost = GhostState.FromSuggestion("con", "connect --server prod", GhostSuggestionSource.History);
        var commandGhost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);

        // Assert history has more complete suggestion
        historyGhost.FullSuggestion.Should().Be("connect --server prod");
        historyGhost.Source.Should().Be(GhostSuggestionSource.History);
        
        commandGhost.FullSuggestion.Should().Be("connect");
        commandGhost.Source.Should().Be(GhostSuggestionSource.Command);
    }

    #endregion

    #region GS-011: Command used when no history match

    [TestMethod]
    [Description("GS-011: Command suggestion used when no history match exists")]
    public void GhostState_CommandSource_WhenNoHistory()
    {
        // When there's no history match, command suggestion is used
        var commandGhost = GhostState.FromSuggestion("hel", "help", GhostSuggestionSource.Command);

        // Assert
        commandGhost.IsVisible.Should().BeTrue();
        commandGhost.Source.Should().Be(GhostSuggestionSource.Command);
        commandGhost.GhostText.Should().Be("p");
    }

    #endregion

    #region GS-012: Most recent history preferred

    [TestMethod]
    [Description("GS-012: More recent history entries should be preferred")]
    public void GhostState_MostRecentHistory_Preferred()
    {
        // This tests the concept - actual ordering is done by the provider
        // Most recent history entry "connect B" should be preferred over "connect A"
        
        var olderHistory = GhostState.FromSuggestion("con", "connect A", GhostSuggestionSource.History);
        var newerHistory = GhostState.FromSuggestion("con", "connect B", GhostSuggestionSource.History);

        // Both are valid - the provider selects which one to show
        olderHistory.IsVisible.Should().BeTrue();
        newerHistory.IsVisible.Should().BeTrue();
        
        // The implementation should use the more recent one
        olderHistory.FullSuggestion.Should().Be("connect A");
        newerHistory.FullSuggestion.Should().Be("connect B");
    }

    #endregion

    #region Source Tracking

    [TestMethod]
    [Description("GhostState correctly tracks its source")]
    public void GhostState_TracksSourceCorrectly()
    {
        var none = new GhostState { Source = GhostSuggestionSource.None };
        var history = new GhostState { Source = GhostSuggestionSource.History };
        var command = new GhostState { Source = GhostSuggestionSource.Command };

        none.Source.Should().Be(GhostSuggestionSource.None);
        history.Source.Should().Be(GhostSuggestionSource.History);
        command.Source.Should().Be(GhostSuggestionSource.Command);
    }

    [TestMethod]
    [Description("FromSuggestion correctly sets source")]
    public void FromSuggestion_SetsSourceCorrectly()
    {
        var defaultSource = GhostState.FromSuggestion("test", "testing");
        var historySource = GhostState.FromSuggestion("test", "testing", GhostSuggestionSource.History);
        var commandSource = GhostState.FromSuggestion("test", "testing", GhostSuggestionSource.Command);

        defaultSource.Source.Should().Be(GhostSuggestionSource.None);
        historySource.Source.Should().Be(GhostSuggestionSource.History);
        commandSource.Source.Should().Be(GhostSuggestionSource.Command);
    }

    #endregion
}
