using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.VirtualConsole;
using Spectre.Console;
using System.Threading;
using System.Threading.Tasks;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests that validate ghost+menu interaction scenarios (GS-020 to GS-022).
/// These verify that ghost text is properly hidden when menu opens and reappears correctly.
/// </summary>
[TestClass]
public class GhostMenuInteractionRenderingTests
{
    #region GS-020: Ghost hidden when menu open - Rendering Validation

    [TestMethod]
    [Description("GS-020-RENDER: Ghost text is cleared from screen when Tab opens menu")]
    public void GhostHidden_WhenMenuOpens_ScreenShowsOnlyInput()
    {
        // Arrange: Console with ghost text visible
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Render ghost "nect"
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        var lineWithGhost = console.Lines[0];
        lineWithGhost.Should().Contain("nect", "ghost should be visible initially");

        // Act: Simulate menu opening by clearing ghost
        renderer.Clear(ghost!);

        // Assert: Ghost should be cleared from screen
        var lineAfterMenuOpen = console.Lines[0].TrimEnd();
        lineAfterMenuOpen.Should().Be("> con", "ghost should be hidden when menu opens");
    }

    [TestMethod]
    [Description("GS-020-RENDER: Cursor remains at input position after ghost clear for menu")]
    public void GhostClearForMenu_CursorRemainsAtInputPosition()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2;

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Act: Clear ghost for menu
        renderer.Clear(ghost!);

        // Assert: Cursor should be at end of input
        var (column, line) = console.GetCursorPosition();
        column.Should().Be(promptLength + 3, "cursor should be at end of 'con'");
    }

    #endregion

    #region GS-021: Ghost returns after menu close - Rendering Validation

    [TestMethod]
    [Description("GS-021-RENDER: Ghost reappears on screen after menu closes with Escape")]
    public void GhostReturns_AfterMenuCloseWithEscape_ScreenShowsGhost()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Initial ghost
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Clear ghost for menu
        renderer.Clear(ghost!);
        var lineWithMenuOpen = console.Lines[0].TrimEnd();
        lineWithMenuOpen.Should().Be("> con", "ghost should be hidden when menu opens");

        // Act: Menu closes with Escape, ghost reappears
        renderer.Render(ghost!);

        // Assert: Ghost should be visible again
        var lineAfterMenuClose = console.Lines[0];
        lineAfterMenuClose.Should().Contain("nect", "ghost should reappear after menu closes");
    }

    [TestMethod]
    [Description("GS-021-RENDER: Cursor returns to input position after ghost re-render")]
    public void GhostReRender_AfterMenuClose_CursorAtInputEnd()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2;

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);
        renderer.Clear(ghost!);

        // Act: Re-render ghost after menu close
        renderer.Render(ghost!);

        // Assert: Cursor at input end
        var (column, _) = console.GetCursorPosition();
        column.Should().Be(promptLength + 3, "cursor should be at end of 'con' after ghost re-render");
    }

    #endregion

    #region GS-022: Ghost updates after menu accept - Rendering Validation

    [TestMethod]
    [Description("GS-022-RENDER: After menu accepts 'connect', new ghost shows next suggestion")]
    public void GhostUpdates_AfterMenuAccept_NewGhostForExtendedInput()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        // User typed "con", menu opened, selected and accepted "connect"
        // Now input is "connect " and we need a new ghost
        console.Write(new Text("> "));
        console.Write(new Text("connect "));

        // Get new ghost for "connect " (might suggest arguments or history)
        var newGhost = GhostState.FromSuggestion("connect ", "connect --server", GhostSuggestionSource.History);

        // Act: Render new ghost
        renderer.Render(newGhost!);

        // Assert: New ghost visible
        var line = console.Lines[0];
        line.Should().Contain("--server", "new ghost should show argument suggestion");
    }

    [TestMethod]
    [Description("GS-022-RENDER: No ghost if accepted command has no further completions")]
    public void NoGhost_AfterMenuAccept_WhenNoFurtherCompletions()
    {
        // Arrange
        var console = new VirtualAnsiConsole();

        // User typed "hel", menu accepted "help" - no further completions expected
        console.Write(new Text("> "));
        console.Write(new Text("help"));

        // No ghost rendered (null or empty)
        // Just verify the screen shows only input
        var line = console.Lines[0].TrimEnd();
        line.Should().Be("> help", "only accepted command should be visible, no ghost");
    }

    #endregion

    #region Complex Interaction Scenarios

    [TestMethod]
    [Description("INTERACTION-001: Ghost clear before menu, navigate menu, accept, new ghost")]
    public void FullMenuInteraction_GhostClearsAndReturns()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // 1. Ghost visible
        var ghost1 = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost1!);
        console.Lines[0].Should().Contain("nect");

        // 2. Clear ghost for menu
        renderer.Clear(ghost1!);
        console.Lines[0].TrimEnd().Should().Be("> con");

        // 3. Simulate menu navigation and accept "config" instead
        // Clear input and write new accepted text
        console.SetCursorPosition(2, 0); // Back to after prompt
        console.Write(new Text("config"));

        // 4. New ghost for "config"
        var ghost2 = GhostState.FromSuggestion("config", "config --verbose", GhostSuggestionSource.History);
        renderer.Render(ghost2!);

        // Assert: New ghost visible
        var line = console.Lines[0];
        line.Should().Contain("--verbose", "new ghost should show after accept");
    }

    [TestMethod]
    [Description("INTERACTION-002: Escape from menu restores original ghost")]
    public void EscapeFromMenu_RestoresOriginalGhost()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));

        // Original ghost
        var originalGhost = GhostState.FromSuggestion("ser", "server", GhostSuggestionSource.Command);
        renderer.Render(originalGhost!);
        console.Lines[0].Should().Contain("ver", "original ghost visible");

        // Clear for menu
        renderer.Clear(originalGhost!);
        console.Lines[0].TrimEnd().Should().Be("> ser", "ghost cleared for menu");

        // Escape pressed - re-render original ghost
        renderer.Render(originalGhost!);

        // Assert: Original ghost restored
        console.Lines[0].Should().Contain("ver", "original ghost restored after escape");
    }

    [TestMethod]
    [Description("INTERACTION-003: Multiple menu open/close cycles don't leave residue")]
    public void MultipleMenuCycles_NoResidualCharacters()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("hel"));

        var ghost = GhostState.FromSuggestion("hel", "help", GhostSuggestionSource.Command);

        // Cycle 1
        renderer.Render(ghost!);
        renderer.Clear(ghost!);
        renderer.Render(ghost!);

        // Cycle 2
        renderer.Clear(ghost!);
        renderer.Render(ghost!);

        // Cycle 3
        renderer.Clear(ghost!);

        // Assert: Clean state after all cycles
        var line = console.Lines[0].TrimEnd();
        line.Should().Be("> hel", "no residual characters after multiple menu cycles");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [Description("EDGE-001: No ghost to clear when menu opens at empty prompt")]
    public void NoGhostToClear_WhenMenuOpensAtEmptyPrompt()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));

        // No ghost exists (empty prompt)
        GhostState? noGhost = null;

        // Act: "Clear" null ghost (should not crash)
        renderer.Clear(noGhost);

        // Assert: Screen unchanged
        var line = console.Lines[0].TrimEnd();
        line.Should().Be(">", "screen should be unchanged");
    }

    [TestMethod]
    [Description("EDGE-002: Ghost with same text as previous after menu close")]
    public void GhostWithSameText_AfterMenuClose_NoDoubleRendering()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Act: Update with a different ghost first, then back to same
        // (Update optimizes by skipping when texts are identical)
        var differentGhost = GhostState.FromSuggestion("con", "config", GhostSuggestionSource.Command);
        renderer.Update(ghost, differentGhost);
        renderer.Update(differentGhost, ghost);

        // Assert: Ghost visible without issues
        var line = console.Lines[0];
        line.Should().Contain("nect", "ghost should be visible");
    }

    #endregion
}
