using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.VirtualConsole;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Tests that validate actual console rendering for ghost text scenarios (GS-001 to GS-012).
/// These complement the GhostDisplayTests which test GhostState properties.
/// These tests verify what actually appears on the console screen.
/// </summary>
[TestClass]
public class GhostRenderingTests
{
    #region GS-001: Ghost appears on typing - Rendering Validation

    [TestMethod]
    [Description("GS-001-RENDER: Ghost text 'nect' visually appears after typing 'con' when 'connect' command exists")]
    public void GhostText_AppearsOnScreen_AfterTypingPartialCommand()
    {
        // Arrange: Create console and renderer
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Create ghost for "connect" -> "nect" suffix
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);

        // Act: Render the ghost
        renderer.Render(ghost!);

        // Assert: Console line should show "con" + ghost text
        var line = console.Lines[0];
        line.Should().Contain("con", "input text should be visible");
        line.Should().Contain("nect", "ghost suffix should be rendered after input");
    }

    [TestMethod]
    [Description("GS-001-RENDER: Cursor returns to input position after ghost render")]
    public void GhostText_CursorReturnsToInputPosition_AfterRender()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2; // "> "

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Create and render ghost
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Assert: Cursor should be at end of input (prompt + "con" = 2 + 3 = 5)
        var (column, line) = console.GetCursorPosition();
        column.Should().Be(promptLength + 3, "cursor should be at end of input, not end of ghost");
        line.Should().Be(0);
    }

    #endregion

    #region GS-002/GS-003: Right Arrow / End key accepts ghost - Rendering Validation

    [TestMethod]
    [Description("GS-002-RENDER: After accepting ghost with RightArrow, full text appears and cursor is at end")]
    public void GhostAccept_RightArrow_FullTextAppearsOnScreen()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2; // "> "

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Act: Accept ghost (simulated by clearing ghost and writing the suffix)
        renderer.Clear(ghost!);
        console.Write(new Text("nect"));

        // Assert: Full text visible, cursor at end
        var line = console.Lines[0].TrimEnd();
        line.Should().Be("> connect", "full command should be visible after accept");

        var (column, _) = console.GetCursorPosition();
        column.Should().Be(promptLength + 7, "cursor should be at end of 'connect'");
    }

    #endregion

    #region GS-004: Typing updates ghost - Rendering Validation

    [TestMethod]
    [Description("GS-004-RENDER: Ghost updates on screen when user types more characters")]
    public void GhostUpdate_WhenTypingContinues_NewGhostAppearsOnScreen()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input "con" with ghost "nect"
        console.Write(new Text("> "));
        console.Write(new Text("con"));
        var ghost1 = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost1!);

        var lineBeforeUpdate = console.Lines[0];
        lineBeforeUpdate.Should().Contain("nect", "initial ghost should be visible");

        // Act: Clear old ghost, type 'f', render new ghost "ig" (completing "config")
        renderer.Clear(ghost1!);
        console.Write(new Text("f")); // Now buffer is "conf"
        var ghost2 = GhostState.FromSuggestion("conf", "config", GhostSuggestionSource.Command);
        renderer.Render(ghost2!);

        // Assert: New ghost visible, old ghost cleared
        var lineAfterUpdate = console.Lines[0].TrimEnd();
        lineAfterUpdate.Should().Be("> config", "should show 'conf' + ghost 'ig'");
    }

    #endregion

    #region GS-005: Typing removes ghost when no match - Rendering Validation

    [TestMethod]
    [Description("GS-005-RENDER: Ghost disappears from screen when no commands match")]
    public void GhostDisappears_WhenNoMatch_ScreenShowsOnlyInput()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input with ghost
        console.Write(new Text("> "));
        console.Write(new Text("con"));
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        var lineWithGhost = console.Lines[0];
        lineWithGhost.Should().Contain("nect", "ghost should be visible initially");

        // Act: Clear ghost and type 'x' -> "conx" has no matches
        renderer.Clear(ghost!);
        console.Write(new Text("x"));

        // Assert: Only input visible, no ghost residue
        var lineAfterClear = console.Lines[0].TrimEnd();
        lineAfterClear.Should().Be("> conx", "only input should be visible, no ghost");
    }

    [TestMethod]
    [Description("GS-005-RENDER: No residual ghost characters after ghost is removed")]
    public void GhostClear_NoResidualCharacters_CleanScreen()
    {
        // Arrange: Start with a long ghost
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));
        var ghost = GhostState.FromSuggestion("ser", "serveradmin --verbose", GhostSuggestionSource.History);
        renderer.Render(ghost!);

        var lineWithGhost = console.Lines[0];
        lineWithGhost.Should().Contain("veradmin", "ghost should show suffix");

        // Act: Clear the ghost completely
        renderer.Clear(ghost!);

        // Assert: All ghost characters removed
        var lineAfterClear = console.Lines[0].TrimEnd();
        lineAfterClear.Should().Be("> ser", "all ghost text should be cleared");
    }

    #endregion

    #region GS-006: Backspace updates ghost - Rendering Validation

    [TestMethod]
    [Description("GS-006-RENDER: Ghost updates correctly after backspace")]
    public void GhostUpdates_AfterBackspace_ShowsCorrectNewGhost()
    {
        // Arrange: "conf" showing ghost "ig" (config)
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2;

        console.Write(new Text("> "));
        console.Write(new Text("conf"));
        var ghost1 = GhostState.FromSuggestion("conf", "config", GhostSuggestionSource.Command);
        renderer.Render(ghost1!);

        console.Lines[0].Should().Contain("ig", "config ghost should be visible");

        // Act: Backspace -> "con" should show ghost "nect" (connect)
        renderer.Clear(ghost1!);

        // Simulate backspace: move cursor back and clear character
        var (currentCol, currentLine) = console.GetCursorPosition();
        console.SetCursorPosition(currentCol - 1, currentLine);
        console.Write(new Text(" ")); // Clear the character
        console.SetCursorPosition(currentCol - 1, currentLine);

        var ghost2 = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost2!);

        // Assert: New ghost visible
        var lineAfter = console.Lines[0];
        lineAfter.Should().Contain("con", "input should be 'con'");
        lineAfter.Should().Contain("nect", "ghost should show 'nect' for connect");
    }

    #endregion

    #region GS-008: No ghost when no matches - Rendering Validation

    [TestMethod]
    [Description("GS-008-RENDER: Screen shows only input when no commands match")]
    public void NoGhost_WhenNoMatches_ScreenShowsOnlyInput()
    {
        // Arrange
        var console = new VirtualAnsiConsole();

        // Write prompt and input with no matching commands
        console.Write(new Text("> "));
        console.Write(new Text("xyz123"));

        // Assert: Only input visible (no ghost rendered)
        var line = console.Lines[0].TrimEnd();
        line.Should().Be("> xyz123", "only input should be visible when no ghost");
    }

    #endregion

    #region GS-010/GS-011: Ghost source priority - Rendering Validation

    [TestMethod]
    [Description("GS-010-RENDER: History ghost appears on screen")]
    public void HistoryGhost_AppearsOnScreen()
    {
        // Arrange: Create ghost from history source
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Ghost from history (includes arguments)
        var ghost = GhostState.FromSuggestion("con", "connect --server prod", GhostSuggestionSource.History);
        renderer.Render(ghost!);

        // Assert: Ghost should show history completion
        var line = console.Lines[0];
        line.Should().Contain("con", "input visible");
        line.Should().Contain("--server", "history ghost should include argument part");
    }

    [TestMethod]
    [Description("GS-011-RENDER: Command ghost appears when no history matches")]
    public void CommandGhost_AppearsOnScreen_WhenNoHistoryMatch()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("hel"));

        // Ghost from command source (no history match)
        var ghost = GhostState.FromSuggestion("hel", "help", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Assert: Command ghost visible
        var line = console.Lines[0];
        line.Should().Contain("hel", "input visible");
        line.Should().Contain("p", "ghost 'p' for 'help' should be visible");
    }

    #endregion

    #region Residual Character Bug Tests

    [TestMethod]
    [Description("RESIDUAL-001: Long ghost to short ghost clears all residual characters")]
    public void LongToShortGhost_ClearsAllResidual()
    {
        // Arrange: Start with a long ghost
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));

        var longGhost = GhostState.FromSuggestion("ser", "serverstatus --verbose --format=json", GhostSuggestionSource.History);
        renderer.Render(longGhost!);

        var lineWithLongGhost = console.Lines[0];
        lineWithLongGhost.Should().Contain("--verbose", "long ghost should be rendered");

        // Act: Update to a short ghost
        renderer.Clear(longGhost!);
        console.Write(new Text("v")); // Now "serv"
        var shortGhost = GhostState.FromSuggestion("serv", "server", GhostSuggestionSource.Command);
        renderer.Render(shortGhost!);

        // Assert: No residual characters from the long ghost
        var lineAfter = console.Lines[0].TrimEnd();
        lineAfter.Should().NotContain("--verbose", "long ghost text should be cleared");
        lineAfter.Should().NotContain("format", "long ghost text should be cleared");
    }

    [TestMethod]
    [Description("RESIDUAL-002: Backspace scenario doesn't leave 'r' residue")]
    public void BackspaceScenario_NoResidualR()
    {
        // This tests the specific bug where "server" ghost backspaced to "serve" leaves 'r'
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("server"));

        // Render ghost " connect"
        var ghost1 = GhostState.FromSuggestion("server", "server connect", GhostSuggestionSource.History);
        renderer.Render(ghost1!);

        // Clear and simulate backspace
        renderer.Clear(ghost1!);
        var (col, line) = console.GetCursorPosition();
        console.SetCursorPosition(col - 1, line);
        console.Write(new Text(" "));
        console.SetCursorPosition(col - 1, line);

        // Render new ghost for "serve"
        var ghost2 = GhostState.FromSuggestion("serve", "server", GhostSuggestionSource.Command);
        renderer.Render(ghost2!);

        // Assert: Line should be clean (note: "serve" + ghost "r" = "server")
        var visibleText = console.Lines[0].TrimEnd();
        // The text should be "> server" (serve + ghost r)
        visibleText.Should().Be("> server");
    }

    #endregion

    #region Cursor Position Tests

    [TestMethod]
    [Description("CURSOR-001: Multiple ghost updates maintain correct cursor position")]
    public void MultipleGhostUpdates_CursorRemainsAtInputEnd()
    {
        // Arrange
        var console = new VirtualAnsiConsole();
        var renderer = new GhostTextRenderer(console);
        var promptLength = 2;

        console.Write(new Text("> "));

        // Type and check cursor after each ghost render
        var inputs = new[] { "c", "co", "con", "conn", "conne", "connec" };
        GhostState? prevGhost = null;

        foreach (var input in inputs)
        {
            if (prevGhost != null)
            {
                renderer.Clear(prevGhost);
            }

            // Write just the new character
            console.Write(new Text(input[^1].ToString()));

            var ghost = GhostState.FromSuggestion(input, "connect", GhostSuggestionSource.Command);
            renderer.Render(ghost!);
            prevGhost = ghost;

            // Assert cursor is at end of input
            var (col, _) = console.GetCursorPosition();
            col.Should().Be(promptLength + input.Length, $"cursor should be at end of '{input}'");
        }
    }

    #endregion
}
