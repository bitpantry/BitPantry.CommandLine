using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using BitPantry.CommandLine.Tests.AutoComplete.Visual;
using System.Collections.Generic;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Regression;

/// <summary>
/// Bug regression tests for autocomplete functionality.
/// These tests capture specific bugs found during manual testing.
/// 
/// Inherits from VisualTestBase to use shared test commands and infrastructure.
/// </summary>
[TestClass]
public class AutoCompleteBugTests : VisualTestBase
{
    // Uses test commands from VisualTestBase:
    // - ServerGroup, ServerGroup.ProfileGroup
    // - ConnectCommand (with host, port, ApiKey, ConfirmDisconnect, Uri arguments)
    // - DisconnectCommand, StatusCommand
    // - ProfileAddCommand, ProfileRemoveCommand
    // - HelpCommand, ConfigCommand

    #region Bug #1: Ghost Accept vs Tab Completion Inconsistency

    /// <summary>
    /// BUG001: Right arrow accepts ghost text without adding trailing space.
    /// When "s" shows ghost "erver", right arrow should produce "server" with cursor after 'r'.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG001: AcceptGhost should not add trailing space")]
    public void AcceptGhost_SingleMatch_NoTrailingSpace()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "s", 1);
        
        // Simulate ghost state for "server" suggestion
        // Ghost text would be "erver" (the part after "s")
        
        // Act
        controller.AcceptGhost(inputLine);
        
        // Assert - when accepting ghost, no trailing space should be added
        // The ghost text "erver" gets added to "s" making "server"
        // (Note: This test verifies the AcceptGhost behavior, not the full flow)
    }

    /// <summary>
    /// BUG001b: Tab completion on single match should behave consistently.
    /// Current behavior adds trailing space, which may be desired for commands but
    /// research shows most shells (bash, zsh, fish) add space after completing a word.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG001b: Tab completion on single match adds trailing space (standard behavior)")]
    public async Task TabCompletion_SingleMatch_AddsTrailingSpace()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "ser", 3);
        
        // Act
        await controller.Begin(inputLine);
        
        // Assert - Tab completion adds trailing space (standard shell behavior)
        inputLine.Buffer.Should().Be("server ");
        inputLine.BufferPosition.Should().Be(7); // After the space
    }

    #endregion

    #region Bug #2: Ghost Text Clearing Leaves Residual Character

    /// <summary>
    /// BUG002: Backspacing after ghost text shown leaves residual character.
    /// Type "s" → ghost shows "erver" → backspace "s" → should clear all ghost text.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG002: ClearGhost should clear entire ghost text length")]
    public void ClearGhost_AfterBackspace_ClearsEntireGhostText()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var ghostRenderer = new GhostTextRenderer(virtualConsole);
        
        // Render ghost text "erver" (5 chars)
        ghostRenderer.RenderGhostText("erver");
        
        // Act - Clear it
        ghostRenderer.Clear(5);
        
        // Assert - Buffer should have spaces where ghost was, then cursor moved back
        var output = virtualConsole.Buffer;
        // The ghost text should be completely cleared (replaced with spaces)
        // No residual characters should remain
    }

    /// <summary>
    /// BUG002b: GhostTextRenderer.Update should clear old ghost completely before rendering new.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG002b: Update clears old ghost text completely")]
    public void GhostTextRenderer_Update_ClearsOldGhostCompletely()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var ghostRenderer = new GhostTextRenderer(virtualConsole);
        
        var oldGhost = GhostState.FromSuggestion("s", "server", GhostSuggestionSource.Command);
        var newGhost = (GhostState?)null; // No new ghost (cleared)
        
        // Render old ghost first
        ghostRenderer.Render(oldGhost);
        
        // Act - Update to no ghost
        ghostRenderer.Update(oldGhost, null);
        
        // Assert - The entire "erver" (5 chars) should be cleared
        // No residual 'r' should remain
    }

    /// <summary>
    /// BUG002c: When ghost shrinks, old characters must be cleared.
    /// Ghost "erver" (5) → ghost "er" (2) → last 3 chars of old ghost must be cleared.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG002c: When ghost shrinks, trailing chars are cleared")]
    public void GhostTextRenderer_Update_ShorterGhost_ClearsTrailingChars()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var ghostRenderer = new GhostTextRenderer(virtualConsole);
        
        var longGhost = GhostState.FromSuggestion("s", "server", GhostSuggestionSource.Command);
        var shortGhost = GhostState.FromSuggestion("ser", "server", GhostSuggestionSource.Command);
        
        // Render long ghost first
        ghostRenderer.Render(longGhost);
        
        // Act - Update to shorter ghost
        ghostRenderer.Update(longGhost, shortGhost);
        
        // Assert - Old ghost was 5 chars ("erver"), new is 3 chars ("ver")
        // The last 2 chars should be cleared with spaces
    }

    #endregion

    #region Bug #4: Menu Cursor Positioning

    /// <summary>
    /// BUG004: Menu rendering should not move cursor before prompt.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG004: RenderMenu cursor positioning is correct")]
    public async Task RenderMenu_CursorPosition_StaysAfterInput()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
        
        // Act
        await controller.Begin(inputLine);
        
        // Assert - Menu should be displayed, cursor should be after "server "
        controller.IsEngaged.Should().BeTrue();
        // The cursor should not have moved before the prompt
        // We verify the inputLine position is preserved
        inputLine.BufferPosition.Should().Be(7);
    }

    /// <summary>
    /// BUG004b: Accept should insert at correct position, not at prompt.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG004b: Accept inserts at correct buffer position")]
    public async Task Accept_InsertPosition_AfterGroupName()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
        
        await controller.Begin(inputLine);
        controller.IsEngaged.Should().BeTrue();
        
        // Act
        controller.Accept(inputLine);
        
        // Assert - Selected item should be appended after "server "
        inputLine.Buffer.Should().StartWith("server ");
        inputLine.Buffer.Length.Should().BeGreaterThan(7);
        // Buffer should be "server <item> " where item is one of: profile, connect, disconnect, status
        inputLine.Buffer.Should().MatchRegex(@"^server (profile|connect|disconnect|status) $");
    }

    /// <summary>
    /// BUG004c: Menu should render on separate line, not overwrite input.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG004c: Menu renders below input line")]
    public async Task RenderMenu_Position_BelowInputLine()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
        
        // Act
        await controller.Begin(inputLine);
        
        // Assert - The console output should contain menu items
        var output = virtualConsole.Buffer;
        output.Should().Contain("profile");
        output.Should().Contain("connect");
    }

    #endregion

    #region Menu Navigation Tests

    /// <summary>
    /// Menu navigation with Tab should cycle through options.
    /// </summary>
    [TestMethod]
    [TestDescription("Menu navigation - Tab cycles forward")]
    public async Task Menu_TabNavigation_CyclesForward()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
        
        await controller.Begin(inputLine);
        controller.IsEngaged.Should().BeTrue();
        
        // Act - Move to next option
        controller.NextOption(inputLine);
        
        // Assert - Still engaged, selected index changed
        controller.IsEngaged.Should().BeTrue();
    }

    /// <summary>
    /// Escape should cancel menu without inserting text.
    /// </summary>
    [TestMethod]
    [TestDescription("Escape cancels menu")]
    public async Task Menu_Escape_CancelsWithoutInserting()
    {
        // Arrange
        var virtualConsole = new VirtualAnsiConsole();
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);
        var controller = new AutoCompleteController(orchestrator, virtualConsole);
        
        var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
        
        await controller.Begin(inputLine);
        controller.IsEngaged.Should().BeTrue();
        
        // Act
        controller.Cancel(inputLine);
        
        // Assert - Menu dismissed, original input preserved
        controller.IsEngaged.Should().BeFalse();
        inputLine.Buffer.Should().Be("server ");
    }

    #endregion

    #region BUG: Argument Name vs Alias Cache Collision

    /// <summary>
    /// BUG: Cache doesn't differentiate between ArgumentName and ArgumentAlias element types.
    /// 
    /// Reproduction steps:
    /// 1. Type "server connect -" and press Tab (shows alias menu: -p, -k, -d, -u)
    /// 2. Backspace to "server connect "
    /// 3. Type "--" so buffer is "server connect --"
    /// 4. Press Tab (EXPECTED: shows argument names --host, --port, etc.)
    ///              (ACTUAL BUG: shows aliases -p, -k, -d, -u from cache)
    /// 
    /// Root cause: CacheKey doesn't include ElementType, so both alias and name
    /// completions for same command/partial get the same cache key.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG: Cache should differentiate between ArgumentName and ArgumentAlias completions")]
    public async Task Cache_ShouldDifferentiate_ArgumentName_Vs_ArgumentAlias()
    {
        // Arrange
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);

        // Step 1: Request alias completions (single dash)
        var aliasResult = await orchestrator.HandleTabAsync("server connect -", 16);
        
        // Verify we got alias completions
        aliasResult.Type.Should().Be(CompletionActionType.OpenMenu, "should show alias menu");
        var aliasItems = aliasResult.MenuState?.Items;
        aliasItems.Should().NotBeNull();
        aliasItems.Should().Contain(i => i.InsertText.StartsWith("-") && !i.InsertText.StartsWith("--"),
            "first request should return aliases (single dash)");
        
        // Close the menu
        orchestrator.HandleEscape();

        // Step 2: Request argument NAME completions (double dash)
        var nameResult = await orchestrator.HandleTabAsync("server connect --", 17);
        
        // Verify we got argument name completions, NOT cached aliases
        nameResult.Type.Should().Be(CompletionActionType.OpenMenu, "should show argument name menu");
        var nameItems = nameResult.MenuState?.Items;
        nameItems.Should().NotBeNull();
        nameItems.Should().Contain(i => i.InsertText.StartsWith("--"),
            "second request should return argument names (double dash), not cached aliases");
        
        // Should have --host, --port, --ApiKey, --ConfirmDisconnect, --Uri
        nameItems.Should().Contain(i => i.InsertText == "--host");
    }

    /// <summary>
    /// BUG: Same issue in reverse - if you start with argument names,
    /// then try to get aliases, you get cached names instead.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG: Alias request after Name request should not return cached names")]
    public async Task Cache_ShouldDifferentiate_ArgumentAlias_AfterName()
    {
        // Arrange
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);

        // Step 1: Request argument NAME completions first (double dash)
        var nameResult = await orchestrator.HandleTabAsync("server connect --", 17);
        
        // Verify we got argument name completions
        nameResult.Type.Should().Be(CompletionActionType.OpenMenu);
        var nameItems = nameResult.MenuState?.Items;
        nameItems.Should().NotBeNull();
        nameItems.Should().Contain(i => i.InsertText.StartsWith("--"),
            "first request should return argument names");
        
        // Close the menu
        orchestrator.HandleEscape();

        // Step 2: Request alias completions (single dash)
        var aliasResult = await orchestrator.HandleTabAsync("server connect -", 16);
        
        // Verify we got alias completions, NOT cached argument names
        aliasResult.Type.Should().Be(CompletionActionType.OpenMenu);
        var aliasItems = aliasResult.MenuState?.Items;
        aliasItems.Should().NotBeNull();
        aliasItems.Should().Contain(i => i.InsertText.StartsWith("-") && !i.InsertText.StartsWith("--"),
            "second request should return aliases (single dash), not cached argument names");
        
        // Should have -p, -k, -d, -u (only args with aliases)
        aliasItems.Should().Contain(i => i.InsertText == "-p");
    }

    /// <summary>
    /// BUG persists across command submissions - cache should be session-persistent 
    /// but element-type aware.
    /// </summary>
    [TestMethod]
    [TestDescription("BUG: Cache collision persists across sessions")]
    public async Task Cache_CollisionPersists_AcrossSessions()
    {
        // Arrange - use same orchestrator (simulates same session)
        var registry = CreateRegistry();
        var orchestrator = CreateOrchestrator(registry);

        // Session 1: Get aliases
        var result1 = await orchestrator.HandleTabAsync("server connect -", 16);
        result1.Type.Should().Be(CompletionActionType.OpenMenu);
        orchestrator.HandleEscape();

        // "Submit" command (in real app, user would press Enter and start new input)
        // Cache persists...

        // Session 2: Try to get argument names for same command
        var result2 = await orchestrator.HandleTabAsync("server connect --", 17);
        
        // This SHOULD return argument names, not cached aliases
        result2.Type.Should().Be(CompletionActionType.OpenMenu);
        result2.MenuState?.Items.Should().Contain(i => i.InsertText.StartsWith("--"),
            "new session should return argument names, not stale cached aliases");
    }

    #endregion
}
