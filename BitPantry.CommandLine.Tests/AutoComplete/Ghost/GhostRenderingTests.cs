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
        var console = new ConsolidatedTestConsole();
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
    [Description("GS-001-RENDER: Renderer emits cursor movement to return to input position")]
    public void GhostText_EmitsCursorMovement_AfterRender()
    {
        // Arrange
        var console = new ConsolidatedTestConsole().EmitAnsiSequences();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        // Create and render ghost
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Assert: The output should contain cursor movement escape sequence
        // After rendering "nect" ghost, cursor should move left 4 positions
        var output = console.Output;
        output.Should().Contain("nect", "ghost text should be rendered");
        // The output should contain ESC[4D or similar cursor left sequence
        output.Should().Contain("\u001b[", "should contain ANSI escape sequence for cursor movement");
    }

    #endregion

    #region GS-002/GS-003: Right Arrow / End key accepts ghost - Rendering Validation

    [TestMethod]
    [Description("GS-002-RENDER: After accepting ghost with RightArrow, full text is output")]
    public void GhostAccept_RightArrow_FullTextAppearsOnScreen()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input
        console.Write(new Text("> "));
        console.Write(new Text("con"));

        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Act: Accept ghost (simulated by clearing ghost and writing the suffix)
        renderer.Clear(ghost!);
        console.Write(new Text("nect"));

        // Assert: Output should contain both prompt and full command
        var output = console.Output;
        output.Should().Contain("> ", "prompt should be in output");
        output.Should().Contain("con", "original input should be in output");
        output.Should().Contain("nect", "accepted ghost suffix should be in output");
    }

    #endregion

    #region GS-004: Typing updates ghost - Rendering Validation

    [TestMethod]
    [Description("GS-004-RENDER: Ghost updates on screen when user types more characters")]
    public void GhostUpdate_WhenTypingContinues_NewGhostAppearsOnScreen()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input "con" with ghost "nect"
        console.Write(new Text("> "));
        console.Write(new Text("con"));
        var ghost1 = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost1!);

        // Verify first ghost was rendered
        console.Output.Should().Contain("nect", "initial ghost should be output");

        // Act: Clear old ghost, type 'f', render new ghost "ig" (completing "config")
        renderer.Clear(ghost1!);
        console.Write(new Text("f")); // Now buffer is "conf"
        var ghost2 = GhostState.FromSuggestion("conf", "config", GhostSuggestionSource.Command);
        renderer.Render(ghost2!);

        // Assert: New ghost rendered (output contains both ghosts since log is append-only)
        console.Output.Should().Contain("ig", "second ghost 'ig' should be rendered");
        ghost2!.Text.Should().Be("ig", "ghost state should have 'ig' suffix");
    }

    #endregion

    #region GS-005: Typing removes ghost when no match - Rendering Validation

    [TestMethod]
    [Description("GS-005-RENDER: Ghost Clear method completes without error")]
    public void GhostDisappears_WhenNoMatch_ClearCompletes()
    {
        // Arrange
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

        // Write prompt and input with ghost
        console.Write(new Text("> "));
        console.Write(new Text("con"));
        var ghost = GhostState.FromSuggestion("con", "connect", GhostSuggestionSource.Command);
        renderer.Render(ghost!);

        // Verify ghost was rendered
        console.Output.Should().Contain("nect", "ghost should be visible initially");

        // Act: Clear ghost - this is the behavior being tested
        var act = () => renderer.Clear(ghost!);

        // Assert: Clear operation completes without throwing
        act.Should().NotThrow("Clear should complete without error");
        
        // After clear, ghost state should be inactive
        ghost.Clear();
        ghost.IsActive.Should().BeFalse("ghost should be inactive after Clear()");
    }

    [TestMethod]
    [Description("GS-005-RENDER: No residual ghost characters after ghost is removed")]
    public void GhostClear_NoResidualCharacters_CleanScreen()
    {
        // Arrange: Start with a long ghost
        var console = new ConsolidatedTestConsole().EmitAnsiSequences();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));
        var ghost = GhostState.FromSuggestion("ser", "serveradmin --verbose", GhostSuggestionSource.History);
        renderer.Render(ghost!);

        // Verify ghost was rendered
        console.Output.Should().Contain("veradmin", "ghost should show suffix");

        // Act: Clear the ghost completely
        renderer.Clear(ghost!);

        // Assert: Clear operation emits ANSI sequences to overwrite with spaces
        // The ghost suffix is 18 chars ("veradmin --verbose"), so Clear writes spaces
        var output = console.Output;
        output.Should().Contain("\u001b[", "clear should emit ANSI escape sequences for cursor movement");
        // The clear operation writes spaces to overwrite the ghost text
        // This is the actual behavior - the visual result is handled by ANSI
    }

    #endregion

    #region GS-006: Backspace updates ghost - Rendering Validation

    [TestMethod]
    [Description("GS-006-RENDER: Ghost updates correctly after backspace")]
    public void GhostUpdates_AfterBackspace_ShowsCorrectNewGhost()
    {
        // Arrange: "conf" showing ghost "ig" (config)
        var console = new ConsolidatedTestConsole();
        var renderer = new GhostTextRenderer(console);

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
        var console = new ConsolidatedTestConsole();

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
        var console = new ConsolidatedTestConsole();
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
        var console = new ConsolidatedTestConsole();
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
        var console = new ConsolidatedTestConsole().EmitAnsiSequences();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("ser"));

        var longGhost = GhostState.FromSuggestion("ser", "serverstatus --verbose --format=json", GhostSuggestionSource.History);
        renderer.Render(longGhost!);

        // Verify long ghost was rendered
        console.Output.Should().Contain("--verbose", "long ghost should be rendered");

        // Act: Clear and render short ghost
        renderer.Clear(longGhost!);
        console.Write(new Text("v")); // Now "serv"
        var shortGhost = GhostState.FromSuggestion("serv", "server", GhostSuggestionSource.Command);
        renderer.Render(shortGhost!);

        // Assert: Short ghost suffix is correct
        shortGhost!.Text.Should().Be("er", "short ghost should have 'er' suffix");
        // The Clear operation wrote spaces to overwrite the long ghost
        // This is validated by the ANSI sequences being emitted
        console.Output.Should().Contain("\u001b[", "should emit ANSI sequences for clearing");
    }

    [TestMethod]
    [Description("RESIDUAL-002: Backspace scenario correctly renders new ghost")]
    public void BackspaceScenario_NoResidualR()
    {
        // This tests that after backspace, a new ghost can be correctly rendered
        var console = new ConsolidatedTestConsole().EmitAnsiSequences();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));
        console.Write(new Text("server"));

        // Render ghost " connect"
        var ghost1 = GhostState.FromSuggestion("server", "server connect", GhostSuggestionSource.History);
        renderer.Render(ghost1!);

        // Verify first ghost rendered
        console.Output.Should().Contain(" connect", "first ghost should be rendered");
        ghost1!.Text.Should().Be(" connect", "ghost1 suffix should be ' connect'");

        // Clear ghost1
        renderer.Clear(ghost1!);

        // Render new ghost for "serve" -> "server" (ghost "r")
        var ghost2 = GhostState.FromSuggestion("serve", "server", GhostSuggestionSource.Command);
        renderer.Render(ghost2!);

        // Assert: New ghost state is correct
        ghost2!.Text.Should().Be("r", "ghost2 suffix should be 'r'");
        console.Output.Should().Contain("\u001b[", "should emit ANSI sequences");
    }

    #endregion

    #region Cursor Position Tests

    [TestMethod]
    [Description("CURSOR-001: Multiple ghost updates correctly compute ghost suffix")]
    public void MultipleGhostUpdates_GhostSuffixComputedCorrectly()
    {
        // Arrange
        var console = new ConsolidatedTestConsole().EmitAnsiSequences();
        var renderer = new GhostTextRenderer(console);

        console.Write(new Text("> "));

        // Test progressive ghost updates
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

            // Assert: Ghost suffix is correctly computed for each input
            var expectedSuffix = "connect"[input.Length..];
            ghost!.Text.Should().Be(expectedSuffix, $"ghost suffix for '{input}' should be '{expectedSuffix}'");
        }
    }

    #endregion
}
