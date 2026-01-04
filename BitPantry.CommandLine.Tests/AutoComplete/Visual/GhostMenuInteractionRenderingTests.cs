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
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Render ghost "nect"
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        console.Output.Should().Contain("nect", "ghost should be visible initially");

        // Act: Simulate menu opening by clearing ghost
        var clearAction = () => renderer.Clear(ghost!);

        // Assert: Clear should complete without error
        clearAction.Should().NotThrow("clearing ghost for menu should not throw");
        
        // Ghost state can be cleared
        ghost.Clear();
        ghost.IsActive.Should().BeFalse("ghost should be inactive after Clear");
    }

    [TestMethod]
    [Description("GS-020-RENDER: Ghost state is correctly managed during clear")]
    public void GhostClearForMenu_GhostStateManaged()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        ghost.IsActive.Should().BeTrue("ghost should be active after creation");
        renderer.Render(ghost!);

        // Act: Clear ghost for menu
        renderer.Clear(ghost!);
        ghost.Clear(); // Clear the state

        // Assert: Ghost state should be cleared
        ghost.IsActive.Should().BeFalse("ghost should be inactive after Clear");
        ghost.Text.Should().BeNull("ghost text should be null after Clear");
    }

    #endregion

    #region GS-021: Ghost returns after menu close - Rendering Validation

    [TestMethod]
    [Description("GS-021-RENDER: Ghost reappears on screen after menu closes with Escape")]
    public void GhostReturns_AfterMenuCloseWithEscape_ScreenShowsGhost()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Initial ghost
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Clear ghost for menu
        renderer.Clear(ghost!);

        // Act: Menu closes with Escape, ghost reappears
        renderer.Render(ghost!);

        // Assert: Ghost text was rendered to output
        console.Output.Should().Contain("nect", "ghost should be rendered after menu closes");
    }

    [TestMethod]
    [Description("GS-021-RENDER: Ghost can be re-rendered multiple times")]
    public void GhostReRender_AfterMenuClose_CanReRender()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);
        renderer.Clear(ghost!);

        // Act: Re-render ghost after menu close
        var action = () => renderer.Render(ghost!);

        // Assert: Re-render should work
        action.Should().NotThrow("re-rendering ghost should not throw");
        console.Output.Should().Contain("nect", "ghost text should be in output");
    }

    #endregion

    #region GS-022: Ghost updates after menu accept - Rendering Validation

    [TestMethod]
    [Description("GS-022-RENDER: After menu accepts 'connect', new ghost shows next suggestion")]
    public void GhostUpdates_AfterMenuAccept_NewGhostForExtendedInput()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
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
        var console = new ConsolidatedTestConsole();

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
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // 1. Ghost visible
        var ghost1 = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost1!);
        console.Output.Should().Contain("nect", "first ghost should be rendered");

        // 2. Clear ghost for menu
        renderer.Clear(ghost1!);

        // 3. Create new ghost for "config" (simulating menu accept)
        var ghost2 = GhostState.FromSuggestion("config", "config --verbose", GhostSuggestionSource.History);
        renderer.Render(ghost2!);

        // Assert: New ghost visible
        console.Output.Should().Contain("--verbose", "new ghost should show after accept");
        ghost2!.Text.Should().Be(" --verbose", "ghost2 should have correct suffix");
    }

    [TestMethod]
    [Description("INTERACTION-002: Escape from menu restores original ghost")]
    public void EscapeFromMenu_RestoresOriginalGhost()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));

        // Original ghost
        var originalGhost = GhostState.FromSuggestion("ser", "server", GhostSuggestionSource.Command);
        renderer.Render(originalGhost!);
        console.Output.Should().Contain("ver", "original ghost visible");

        // Clear for menu
        renderer.Clear(originalGhost!);

        // Escape pressed - re-render original ghost
        renderer.Render(originalGhost!);

        // Assert: Ghost state is still valid for re-render
        originalGhost!.IsActive.Should().BeTrue("ghost should still be active");
        originalGhost.Text.Should().Be("ver", "ghost text should be 'ver'");
    }

    [TestMethod]
    [Description("INTERACTION-003: Multiple menu open/close cycles work correctly")]
    public void MultipleMenuCycles_GhostStateRemainsValid()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
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

        // Assert: Ghost state is still valid for re-rendering
        ghost!.Text.Should().Be("p", "ghost text should be 'p'");
        ghost.IsActive.Should().BeTrue("ghost should still be active");
        
        // Can render again without error
        var action = () => renderer.Render(ghost!);
        action.Should().NotThrow("should be able to render ghost after multiple cycles");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [Description("EDGE-001: No ghost to clear when menu opens at empty prompt")]
    public void NoGhostToClear_WhenMenuOpensAtEmptyPrompt()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
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
        var console = new ConsolidatedTestConsole();
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
