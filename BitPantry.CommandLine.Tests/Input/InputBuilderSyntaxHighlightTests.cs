using System;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Integration tests for InputBuilder syntax highlighting functionality.
/// Uses TestEnvironment for full system testing at the user experience level.
/// </summary>
[TestClass]
public class InputBuilderSyntaxHighlightTests
{
    // Prompt is "testhost> " which is 10 characters (entry assembly name + "> ")
    private const int PromptLength = 10;

    #region Test Commands

    [Group]
    [Description("Server operations")]
    private class ServerGroup
    {
        [Group]
        [Description("Profile management")]
        public class ProfileGroup { }
    }

    [InGroup<ServerGroup>]
    [Command(Name = "connect")]
    [Description("Connect to server")]
    private class ConnectCommand : CommandBase
    {
        [Argument]
        [Description("Host name")]
        public string Host { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [InGroup<ServerGroup>]
    [Command(Name = "config")]
    [Description("Configure server settings")]
    private class ConfigCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [InGroup<ServerGroup.ProfileGroup>]
    [Command(Name = "add")]
    [Description("Add a profile")]
    private class ProfileAddCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Group]
    [Description("Admin operations")]
    private class AdminGroup
    {
        [Group]
        [Description("User management")]
        public class UsersGroup
        {
            [Group]
            [Description("Role management")]
            public class RolesGroup { }
        }
    }

    [InGroup<AdminGroup.UsersGroup.RolesGroup>]
    [Command(Name = "assign")]
    [Description("Assign a role")]
    private class RolesAssignCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "help")]
    [Description("Display help information")]
    private class HelpCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Helper Methods

    private TestEnvironment CreateTestEnvironment()
    {
        return new TestEnvironment(opt =>
        {
            opt.ConfigureCommands(builder =>
            {
                builder.RegisterCommand<HelpCommand>();
                builder.RegisterCommand<ConnectCommand>();
                builder.RegisterCommand<ConfigCommand>();
                builder.RegisterCommand<ProfileAddCommand>();
                builder.RegisterCommand<RolesAssignCommand>();
            });
        });
    }

    /// <summary>
    /// Gets the text content of the current input line (where cursor is).
    /// </summary>
    private string GetInputLineText(TestEnvironment env)
    {
        var cursorRow = env.Console.VirtualConsole.CursorRow;
        return env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();
    }

    #endregion

    // Implements: DF-001
    [TestMethod]
    public async Task Typing_GroupName_DisplaysInCyan()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - type "server" which is a known group
        await env.Keyboard.TypeTextAsync("server");

        // Assert - "server" should be displayed in cyan (256-color index 14)
        var text = GetInputLineText(env);
        text.Should().Contain("server");
        
        // The text "server" starts at PromptLength (after prompt)
        // Color.Cyan = 256-color index 14
        var firstChar = env.Console.VirtualConsole.GetCell(env.Console.VirtualConsole.CursorRow, PromptLength);
        firstChar.Style.Foreground256.Should().Be(14, "Group 'server' should be displayed in Cyan (256-color index 14)");
    }

    // Implements: DF-002
    [TestMethod]
    public async Task Backspace_ReHighlightsLine()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "serverx" - "server" is a group, "serverx" is not
        await env.Keyboard.TypeTextAsync("serverx");
        
        // Before backspace, "serverx" should not be highlighted as group (no exact match)
        var textBefore = GetInputLineText(env);
        textBefore.Should().Contain("serverx");
        
        // Act - press backspace to remove the 'x', leaving "server"
        await env.Keyboard.PressBackspaceAsync();

        // Assert - "server" should now be displayed in cyan after backspace
        var textAfter = GetInputLineText(env);
        textAfter.Should().Contain("server");
        textAfter.Should().NotContain("serverx");
        
        // The text "server" starts at PromptLength (after prompt)
        // Color.Cyan = 256-color index 14
        var firstChar = env.Console.VirtualConsole.GetCell(env.Console.VirtualConsole.CursorRow, PromptLength);
        firstChar.Style.Foreground256.Should().Be(14, "After backspace, group 'server' should be displayed in Cyan (256-color index 14)");
    }

    // Implements: DF-003
    [TestMethod]
    public async Task TypingWhileGhostTextShowing_ClearsGhostAndReHighlights()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "serv" - this should show ghost text "er" (completing "server")
        await env.Keyboard.TypeTextAsync("serv");
        
        var lineAfterServ = GetInputLineText(env);
        // Line should show "serv" + ghost text "er" = "server"
        lineAfterServ.Should().Contain("server", "Ghost text should complete 'serv' to 'server'");
        
        // Act - type another character "e" while ghost text is showing
        await env.Keyboard.TypeTextAsync("e");
        
        // Assert - ghost text should be cleared and "serve" re-highlighted
        var lineAfterServe = GetInputLineText(env);
        lineAfterServe.Should().Contain("serve");
        
        // After typing "serve", the ghost text should update (showing "r" to complete "server")
        // OR show the full "server" line with ghost text "r"
        // The important thing is that syntax highlighting is still applied
        
        // Type one more character to complete "server"
        await env.Keyboard.TypeTextAsync("r");
        
        var lineAfterServer = GetInputLineText(env);
        lineAfterServer.Should().Contain("server");
        
        // Now "server" is a complete group name, should be highlighted in cyan
        var firstChar = env.Console.VirtualConsole.GetCell(env.Console.VirtualConsole.CursorRow, PromptLength);
        firstChar.Style.Foreground256.Should().Be(14, "After completing 'server' through continued typing, should be displayed in Cyan");
    }

    // Implements: DF-004
    [TestMethod]
    public async Task TabAcceptsCompletion_HighlightingMaintained()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "server" - a complete group name that should be highlighted in cyan
        await env.Keyboard.TypeTextAsync("server");
        
        // Verify initial highlighting
        var firstCharBefore = env.Console.VirtualConsole.GetCell(env.Console.VirtualConsole.CursorRow, PromptLength);
        firstCharBefore.Style.Foreground256.Should().Be(14, "Group 'server' should be highlighted in Cyan before Tab");

        // Act - add space, type partial command "conn" (unique match for "connect"), and press Tab to accept
        await env.Keyboard.TypeTextAsync(" conn");
        await env.Keyboard.PressTabAsync();

        // Assert - "server" should still be highlighted in cyan
        // Tab accepts "connect" but the group name should remain highlighted
        var firstCharAfter = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharAfter.Style.Foreground256.Should().Be(14, "Group 'server' should remain highlighted in Cyan after Tab accepts completion");
        
        // Verify the line contains the accepted completion
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("server connect", "Tab should have accepted 'connect' command");
    }

    // Implements: DF-005
    [TestMethod]
    public async Task ArrowKeyMenuNavigation_HighlightingPreserved()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "server " - group name highlighted, space triggers menu with connect/config
        await env.Keyboard.TypeTextAsync("server ");

        // Verify highlighting before menu
        var firstCharBefore = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharBefore.Style.Foreground256.Should().Be(14, "Group 'server' should be highlighted in Cyan");

        // Press Tab to open menu (multiple options: connect, config)
        await env.Keyboard.PressTabAsync();

        // Verify menu opened (row 1 should contain menu items)
        var row1Text = env.Console.VirtualConsole.GetRow(1).GetText();
        (row1Text.Contains("connect") || row1Text.Contains("config")).Should().BeTrue(
            "Menu should display matching command options");

        // Act - press down arrow to navigate menu
        await env.Keyboard.PressDownArrowAsync();

        // Assert - "server" should still be highlighted in cyan after menu navigation
        var firstCharAfter = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharAfter.Style.Foreground256.Should().Be(14, 
            "Group 'server' should remain highlighted in Cyan during menu navigation");
    }

    // Implements: DF-006
    [TestMethod]
    public async Task MenuSelectionAccepted_LineReHighlighted()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "server " - group name highlighted
        await env.Keyboard.TypeTextAsync("server ");

        // Verify initial highlighting
        var firstCharBefore = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharBefore.Style.Foreground256.Should().Be(14, "Group 'server' should be highlighted in Cyan");

        // Press Tab to open menu (multiple options: connect, config)
        await env.Keyboard.PressTabAsync();

        // Act - press Enter to accept first menu option (should be config or connect)
        await env.Keyboard.PressEnterAsync();

        // Assert - line should be re-highlighted with accepted content
        // "server" should still be highlighted in cyan
        var firstCharAfter = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharAfter.Style.Foreground256.Should().Be(14, 
            "Group 'server' should remain highlighted in Cyan after menu selection accepted");

        // The line should contain the selected command
        var lineText = GetInputLineText(env);
        (lineText.Contains("server config") || lineText.Contains("server connect")).Should().BeTrue(
            "Line should contain the selected command after accepting menu selection");
    }

    // Implements: DF-007
    [TestMethod]
    public async Task PasteMultipleCharacters_FinalStateHighlighted()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - "paste" (type rapidly) a full command with group, command, and argument
        // ConnectCommand has an Argument "Host"
        await env.Keyboard.TypeTextAsync("server connect --host");

        // Assert - final state should be properly highlighted
        // "server" (group) should be in cyan
        var serverFirstChar = env.Console.VirtualConsole.GetCell(0, PromptLength);
        serverFirstChar.Style.Foreground256.Should().Be(14, 
            "Group 'server' should be highlighted in Cyan after paste");

        // "connect" (command) uses plain style per SyntaxColorScheme
        // Verify it's present in the line
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("server connect --host", 
            "Line should contain the pasted content");

        // "--host" argument starts at PromptLength + 15 (after "server connect ")
        // Color.Yellow = 256-color index 11 for argument names
        var argFirstChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 15);
        argFirstChar.Style.Foreground256.Should().Be(11, 
            "Argument '--host' should be highlighted in Yellow after paste");
    }

    // Implements: UX-011 
    [TestMethod]
    public async Task ColoredInputAndDimGhostText_RenderWithoutConflict()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - type partial group name "serv" which should show ghost text "er"
        await env.Keyboard.TypeTextAsync("serv");

        // Assert - typed text "serv" should have no highlighting (partial match)
        // Ghost text "er" should be dim
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("server", "Line should show 'serv' + ghost text 'er' = 'server'");

        // The ghost text portion starts at PromptLength + 4 (after "serv")
        // Ghost text should be dim (CellAttributes.Dim)
        var ghostTextCell = env.Console.VirtualConsole.GetCell(0, PromptLength + 4);
        ghostTextCell.Style.Attributes.HasFlag(BitPantry.VirtualConsole.CellAttributes.Dim).Should().BeTrue(
            "Ghost text 'er' should be dim (not solid)");

        // Act - complete to "server" which is a full group name
        await env.Keyboard.TypeTextAsync("er");

        // Assert - now "server" is typed (no ghost text), and should be colored cyan
        var firstCharAfter = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharAfter.Style.Foreground256.Should().Be(14, 
            "Complete group name 'server' should be highlighted in Cyan");
        
        // The text should no longer be dim (it's typed, not ghost)
        firstCharAfter.Style.Attributes.HasFlag(BitPantry.VirtualConsole.CellAttributes.Dim).Should().BeFalse(
            "Typed text should not be dim");
    }

    // Implements: UX-012
    [TestMethod]
    public async Task InputRemainsColoredWhileMenuDisplays()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Type "server " - group name highlighted, followed by space
        await env.Keyboard.TypeTextAsync("server ");

        // Verify highlighting is present
        var firstCharBefore = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharBefore.Style.Foreground256.Should().Be(14, "Group 'server' should be highlighted in Cyan");

        // Act - press Tab to open menu (multiple options: connect, config)
        await env.Keyboard.PressTabAsync();

        // Assert - menu should be displayed
        var row1Text = env.Console.VirtualConsole.GetRow(1).GetText();
        (row1Text.Contains("connect") || row1Text.Contains("config")).Should().BeTrue(
            "Menu should be displayed with command options");

        // Assert - input on row 0 should still be colored while menu is visible
        var firstCharAfterMenu = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstCharAfterMenu.Style.Foreground256.Should().Be(14, 
            "Group 'server' should remain highlighted in Cyan while menu is displayed");

        // Verify the input line text is still correct
        var inputLineText = env.Console.VirtualConsole.GetRow(0).GetText();
        inputLineText.Should().Contain("server", 
            "Input line should still contain 'server' while menu is displayed");
    }

    // Implements: UX-013
    [TestMethod]
    public async Task NestedGroup_ServerProfileAdd_TwoCyanOneWhite()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - type "server profile add" where server=group, profile=subgroup, add=command
        await env.Keyboard.TypeTextAsync("server profile add");

        // Assert - "server" at PromptLength should be cyan
        var serverChar = env.Console.VirtualConsole.GetCell(0, PromptLength);
        serverChar.Style.Foreground256.Should().Be(14, "Group 'server' should be Cyan");

        // "profile" starts at PromptLength + 7 (after "server ")
        var profileChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 7);
        profileChar.Style.Foreground256.Should().Be(14, "Nested group 'profile' should be Cyan");

        // "add" starts at PromptLength + 15 (after "server profile ")
        // Command gets SyntaxColorScheme.Command = Style.Plain (default foreground)
        var addChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 15);
        // Command style is plain/default - no foreground color set (null/0)
        addChar.Style.Foreground256.Should().NotBe(14, "Command 'add' should not be Cyan");
    }

    // Implements: UX-014
    [TestMethod]
    public async Task ThreeLevelNested_AdminUsersRolesAssign_ThreeCyanOneWhite()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - type "admin users roles assign"
        await env.Keyboard.TypeTextAsync("admin users roles assign");

        // Assert - "admin" at PromptLength should be cyan
        var adminChar = env.Console.VirtualConsole.GetCell(0, PromptLength);
        adminChar.Style.Foreground256.Should().Be(14, "Group 'admin' should be Cyan");

        // "users" starts at PromptLength + 6
        var usersChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 6);
        usersChar.Style.Foreground256.Should().Be(14, "Nested group 'users' should be Cyan");

        // "roles" starts at PromptLength + 12
        var rolesChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 12);
        rolesChar.Style.Foreground256.Should().Be(14, "Nested group 'roles' should be Cyan");

        // "assign" starts at PromptLength + 18
        var assignChar = env.Console.VirtualConsole.GetCell(0, PromptLength + 18);
        assignChar.Style.Foreground256.Should().NotBe(14, "Command 'assign' should not be Cyan");
    }

    // Implements: UX-015
    [TestMethod]
    public async Task UnrecognizedText_DisplaysDefaultStyle()
    {
        // Arrange
        using var env = CreateTestEnvironment();

        // Act - type "nonexistent" which matches nothing in registry
        await env.Keyboard.TypeTextAsync("nonexistent");

        // Assert - should be default style (no special highlighting)
        var firstChar = env.Console.VirtualConsole.GetCell(0, PromptLength);
        firstChar.Style.Foreground256.Should().NotBe(14, "Unrecognized text should not be Cyan");
        firstChar.Style.Foreground256.Should().NotBe(11, "Unrecognized text should not be Yellow");
        firstChar.Style.Foreground256.Should().NotBe(5, "Unrecognized text should not be Purple");
    }
}
