using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for command and group completion behavior after complete tokens.
/// 
/// These tests verify that Tab completion correctly identifies what level
/// of completion is needed based on the current input context:
/// - After a group name: show subcommands
/// - After a subcommand with args: show arguments
/// - After a subcommand with no args: no menu
/// - After nested groups: show nested commands
/// 
/// Uses the shared VisualTestBase infrastructure with:
/// - server (group)
///   - connect (command with args: --host, --port)
///   - disconnect (command with NO args)
///   - status (command with NO args)
///   - profile (nested group)
///     - add (command with NO args)
///     - remove (command with NO args)
/// - help (root command)
/// - config (root command)
/// </summary>
[TestClass]
public class CommandGroupCompletionTests : VisualTestBase
{
    #region After Subcommand With Arguments

    [TestMethod]
    [TestDescription("BUG REPRO: Tab after 'server connect ' should show arguments, NOT parent group commands")]
    public async Task Tab_AfterSubcommandWithArgs_ShowsArguments()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect ");
        
        // Validate setup
        runner.Buffer.Should().Be("server connect ");
        runner.BufferPosition.Should().Be(15);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");
        if (runner.IsMenuVisible)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems ?? Array.Empty<CompletionItem>())}");
        }

        // BUG: Currently shows "profile, connect, disconnect, status" (parent group commands)
        // Expected: Should show "--host, --port" (connect's arguments)
        runner.Should().HaveMenuVisible("connect command has arguments to complete");
        
        // Menu should contain argument names, not sibling commands
        runner.SelectedMenuItem.Should().StartWith("--", 
            "menu should show arguments (--host, --port), not sibling commands");
        runner.SelectedMenuItem.Should().NotBe("profile",
            "menu should NOT show parent group commands");
        runner.SelectedMenuItem.Should().NotBe("disconnect",
            "menu should NOT show sibling commands");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Menu after 'server connect --ConfirmDisconnect ' should exclude ConfirmDisconnect")]
    public async Task Tab_AfterUsedFlag_ExcludesFromMenu()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type: server connect --ConfirmDisconnect (space at end)
        await runner.TypeText("server connect --ConfirmDisconnect ");
        
        // Validate setup
        runner.Buffer.Should().Be("server connect --ConfirmDisconnect ");
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT: Press Tab to open completion menu
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"  MenuItems: {string.Join(", ", menuItemTexts)}");
        }

        runner.Should().HaveMenuVisible("connect command has more arguments to complete");
        
        // Get all menu items
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        // --ConfirmDisconnect should NOT be in the menu since it's already used
        menuItems.Should().NotContain("--ConfirmDisconnect",
            "menu should NOT show --ConfirmDisconnect because it's already used");
        
        // Other arguments should still be available
        menuItems.Should().Contain("--host",
            "menu should show --host which is not yet used");
        menuItems.Should().Contain("--ApiKey",
            "menu should show --ApiKey which is not yet used");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Menu after 'server connect --ApiKey value ' should exclude ApiKey")]
    public async Task Tab_AfterUsedArgWithValue_ExcludesFromMenu()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type: server connect --ApiKey asdf (argument with a value, then space)
        // This is the exact scenario user reported
        await runner.TypeText("server connect --ApiKey asdf ");
        
        // Validate setup
        runner.Buffer.Should().Be("server connect --ApiKey asdf ");
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT: Press Tab to open completion menu
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"  MenuItems: {string.Join(", ", menuItemTexts)}");
        }

        runner.Should().HaveMenuVisible("connect command has more arguments to complete");
        
        // Get all menu items
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        // --ApiKey should NOT be in the menu since it's already used (with value "asdf")
        menuItems.Should().NotContain("--ApiKey",
            "menu should NOT show --ApiKey because it's already used with value 'asdf'");
        
        // Other arguments should still be available
        menuItems.Should().Contain("--host",
            "menu should show --host which is not yet used");
        menuItems.Should().Contain("--ConfirmDisconnect",
            "menu should show --ConfirmDisconnect which is not yet used");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Multi-step Tab->Select->Type->Tab should exclude used argument from second menu")]
    public async Task Tab_MultiStepInteraction_ExcludesUsedArgFromSecondMenu()
    {
        // This test reproduces the EXACT user scenario:
        // 1. Type "server connect " and press Tab (cache populated)
        // 2. Select --ApiKey from menu and accept
        // 3. Type value and space
        // 4. Press Tab again (should NOT show --ApiKey)
        
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Step 1: Type command and press Tab to open menu (this populates the cache!)
        await runner.TypeText("server connect ");
        await runner.PressKey(ConsoleKey.Tab);
        
        runner.Should().HaveMenuVisible("menu should open after Tab");
        var firstMenuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        Debug.WriteLine($"First menu items: {string.Join(", ", firstMenuItems)}");
        firstMenuItems.Should().Contain("--ApiKey", "first menu should include --ApiKey");
        
        // Step 2: Navigate to --ApiKey and accept it
        // Menu items are sorted, find position of --ApiKey
        while (runner.SelectedMenuItem != "--ApiKey" && runner.IsMenuVisible)
        {
            await runner.PressKey(ConsoleKey.DownArrow);
        }
        runner.SelectedMenuItem.Should().Be("--ApiKey", "should have navigated to --ApiKey");
        
        await runner.PressKey(ConsoleKey.Enter); // Accept selection
        runner.Buffer.Should().Contain("--ApiKey", "buffer should contain selected argument");
        
        // Step 3: Type value and space
        await runner.TypeText("mySecretKey ");
        Debug.WriteLine($"After typing value - Buffer: '{runner.Buffer}'");
        
        // Step 4: Press Tab again - THIS IS WHERE THE BUG MANIFESTED
        // Before fix: Cache returned stale results including --ApiKey
        // After fix: Cache key includes UsedArguments, so fresh results are fetched
        await runner.PressKey(ConsoleKey.Tab);
        
        // ASSERT
        runner.Should().HaveMenuVisible("menu should open for remaining arguments");
        
        var secondMenuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        Debug.WriteLine($"Second menu items: {string.Join(", ", secondMenuItems)}");
        
        // --ApiKey should NOT appear in the second menu
        secondMenuItems.Should().NotContain("--ApiKey",
            "BUG: Second menu should NOT show --ApiKey because it's already used");
        
        // Other arguments should still be available
        secondMenuItems.Should().Contain("--host",
            "second menu should show --host which is not yet used");
    }

    #endregion

    #region After Subcommand Without Arguments

    [TestMethod]
    [TestDescription("Tab after 'server disconnect ' (no args) should show no menu")]
    public async Task Tab_AfterSubcommandNoArgs_NoMenu()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server disconnect ");
        
        // Validate setup
        runner.Buffer.Should().Be("server disconnect ");
        runner.BufferPosition.Should().Be(18);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");

        // disconnect has no arguments, so Tab should do nothing
        runner.Should().HaveMenuHidden("disconnect command has no arguments");
        runner.Buffer.Should().Be("server disconnect ", 
            "buffer should not change when no completions available");
    }

    #endregion

    #region After Nested Group Commands

    [TestMethod]
    [TestDescription("Tab after 'server profile add ' (nested, no args) should show no menu")]
    public async Task Tab_AfterNestedSubcommand_NoMenu()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server profile add ");
        
        // Validate setup
        runner.Buffer.Should().Be("server profile add ");
        runner.BufferPosition.Should().Be(19);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");

        // 'add' has no arguments
        runner.Should().HaveMenuHidden("'add' command has no arguments");
        runner.Buffer.Should().Be("server profile add ",
            "buffer should not change when no completions available");
    }

    #endregion

    #region After Group Name (Show Subcommands)

    [TestMethod]
    [TestDescription("Tab after 'server ' should show subcommands (connect, disconnect, profile, status)")]
    public async Task Tab_AfterGroupName_ShowsSubcommands()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        
        // Validate setup
        runner.Buffer.Should().Be("server ");
        runner.BufferPosition.Should().Be(7);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");

        runner.Should().HaveMenuVisible("server group has subcommands");
        
        // Should be one of the server subcommands
        var validSubcommands = new[] { "connect", "disconnect", "profile", "status" };
        runner.SelectedMenuItem.Should().BeOneOf(validSubcommands,
            "menu should show server's subcommands");
    }

    [TestMethod]
    [TestDescription("Tab after 'server profile ' should show nested commands (add, remove)")]
    public async Task Tab_AfterNestedGroup_ShowsNestedCommands()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server profile ");
        
        // Validate setup
        runner.Buffer.Should().Be("server profile ");
        runner.BufferPosition.Should().Be(15);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");

        runner.Should().HaveMenuVisible("profile group has subcommands");
        
        // Should be one of the profile subcommands
        var validSubcommands = new[] { "add", "remove" };
        runner.SelectedMenuItem.Should().BeOneOf(validSubcommands,
            "menu should show profile's subcommands (add, remove)");
    }

    #endregion

    #region Ghost Text After Complete Command

    [TestMethod]
    [TestDescription("BUG REPRO: After complete command with args, ghost text should include -- prefix")]
    public async Task Ghost_AfterCommandWithMultipleArgs_ShowsFullArgumentName()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // ACT: type complete command with space
        await runner.TypeText("server connect ");

        // ASSERT: Ghost should show full argument name including "--" prefix
        runner.Buffer.Should().Be("server connect ");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}', Ghost: '{runner.GhostText}'");
        
        // Bug: Ghost shows "host" but should show "--host" 
        // The user hasn't typed "--" yet, so ghost should include the full argument format
        runner.GhostText.Should().StartWith("--",
            "ghost text should include '--' prefix when suggesting argument names");
    }

    [TestMethod]
    [TestDescription("After command with args, Tab opens menu to choose from multiple arguments")]
    public async Task Tab_AfterCommand_OpensMenuForMultipleArgs()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect ");
        var ghostBefore = runner.GhostText;
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}', Ghost before Tab: '{ghostBefore}'");
        
        // Verify ghost shows first argument with prefix
        runner.GhostText.Should().StartWith("--", 
            "ghost should show argument with prefix");

        // ACT: press Tab when ghost is visible
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT: Tab should open menu to show all available arguments
        // Even though ghost shows one suggestion, Tab opens menu when multiple args exist
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");
        
        runner.Should().HaveMenuVisible("Tab should open menu when multiple arguments available");
        runner.SelectedMenuItem.Should().StartWith("--",
            "menu should show argument options");
    }

    #endregion

    #region Empty Input

    [TestMethod]
    [TestDescription("Tab on empty input should show root commands/groups")]
    public async Task Tab_EmptyInput_ShowsRootCommands()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Validate setup - empty buffer
        runner.Buffer.Should().BeEmpty();
        runner.BufferPosition.Should().Be(0);
        runner.Should().HaveMenuHidden();

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: '{runner.SelectedMenuItem}'");

        runner.Should().HaveMenuVisible("should show available commands");
        
        // Should be one of the root-level items
        var validRootItems = new[] { "server", "help", "config" };
        runner.SelectedMenuItem.Should().BeOneOf(validRootItems,
            "menu should show root-level commands and groups");
    }

    #endregion
}
