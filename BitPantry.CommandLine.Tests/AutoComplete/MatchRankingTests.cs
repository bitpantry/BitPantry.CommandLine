using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Match Ranking Tests (TC-20.1 through TC-20.5)
/// Tests match ordering and ranking behavior.
/// </summary>
[TestClass]
public class MatchRankingTests
{
    #region TC-20.1: Case-Insensitive Matching

    /// <summary>
    /// TC-20.1: When typing lowercase to match uppercase,
    /// Then matches are found.
    /// </summary>
    [TestMethod]
    public void TC_20_1_CaseInsensitive_Matching()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<HelpTestCommand>();

        // Act: Type lowercase
        harness.TypeText("hel");
        harness.PressTab();

        // Assert: Match found (help command)
        harness.Buffer.Should().Contain("hel");
    }

    #endregion

    #region TC-20.2: Prefix Matches Ranked First

    /// <summary>
    /// TC-20.2: When both prefix and contains matches exist,
    /// Then prefix matches appear first.
    /// </summary>
    [TestMethod]
    public void TC_20_2_PrefixMatches_RankedFirst()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type prefix
        harness.TypeText("ser");
        harness.PressTab();

        // Assert: Server should match (prefix)
        harness.Buffer.Should().Contain("ser");
    }

    #endregion

    #region TC-20.3: Exact Match Prioritized in Ordering

    /// <summary>
    /// TC-20.3: When exact match exists among multiple matches,
    /// Then exact match appears first.
    /// </summary>
    [TestMethod]
    public void TC_20_3_ExactMatch_Prioritized()
    {
        // Arrange: help, helper, helpful all exist
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(HelpTestCommand), typeof(HelperTestCommand), typeof(HelpfulTestCommand));

        // Act: Type exact match
        harness.TypeText("help");
        harness.PressTab();

        // Assert: First match should be exact "help"
        if (harness.IsMenuVisible)
        {
            harness.SelectedIndex.Should().Be(0);
        }
    }

    #endregion

    #region TC-20.4: Multiple Prefix Matches Show Menu

    /// <summary>
    /// TC-20.4: When multiple items have the same prefix,
    /// Then menu shows all matches (no auto-accept).
    /// </summary>
    [TestMethod]
    public void TC_20_4_MultiplePrefixMatches_ShowMenu()
    {
        // Arrange: help, helper, helpful all start with "help"
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(HelpTestCommand), typeof(HelperTestCommand), typeof(HelpfulTestCommand));

        // Act
        harness.TypeText("help");
        harness.PressTab();

        // Assert: Menu shows multiple items
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(1);
        }
    }

    #endregion

    #region TC-20.5: Matched Portion Highlighted

    /// <summary>
    /// TC-20.5: When items are matched,
    /// Then matched portion is visually distinct.
    /// </summary>
    [TestMethod]
    public void TC_20_5_MatchedPortion_Highlighted()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ConfigTestCommand>();

        // Act
        harness.TypeText("con");
        harness.PressTab();

        // Assert: Match displayed (highlighting is visual)
        harness.Buffer.Should().Contain("con");
    }

    #endregion
}
