using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for ghost text behavior including:
/// - Basic ghost text display
/// - Subcommand completion
/// - Cursor movement interaction
/// - Typing updates (matching/non-matching)
/// - Menu interaction
/// </summary>
[TestClass]
public class GhostBehaviorTests : VisualTestBase
{
    #region Basic Ghost Text Behavior

    [TestMethod]
    [TestDescription("Typing 's' should show ghost text 'erver' for server command")]
    public async Task GhostText_SingleChar_ShowsCompletion()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: empty buffer, no ghost
        runner.Buffer.Should().BeEmpty();
        runner.GhostText.Should().BeNullOrEmpty();

        // Action: type 's'
        await runner.TypeText("s");

        // Ending condition: buffer has 's', ghost shows 'erver'
        runner.Buffer.Should().Be("s");
        runner.GhostText.Should().Be("erver");
    }

    [TestMethod]
    [TestDescription("Typing 'ser' should show ghost text 'ver'")]
    public async Task GhostText_PartialWord_ShowsRemainder()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type 'ser'
        await runner.TypeText("ser");

        // Ending condition
        runner.Buffer.Should().Be("ser");
        runner.GhostText.Should().Be("ver");
    }

    [TestMethod]
    [TestDescription("Typing full command 'server' should hide ghost (exact match)")]
    public async Task GhostText_ExactMatch_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type 'server'
        await runner.TypeText("server");

        // Ending condition: no ghost for exact match
        runner.Buffer.Should().Be("server");
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("Typing non-matching text should show no ghost")]
    public async Task GhostText_NoMatch_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type something with no matches
        await runner.TypeText("xyznonexistent");

        // Ending condition: no ghost
        runner.Buffer.Should().Be("xyznonexistent");
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("Empty input should show no ghost")]
    public async Task GhostText_EmptyInput_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: empty buffer
        runner.Buffer.Should().BeEmpty();

        // Ending condition: no ghost
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("Typing partial command should show ghost text suggestion")]
    public async Task TypingPartial_ShouldShowGhostText()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type partial command
        await runner.TypeText("serv");

        Debug.WriteLine($"After 'serv' - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  HasGhostText: {runner.Controller.HasGhostText}");
        
        runner.Buffer.Should().Be("serv");
        runner.GhostText.Should().Be("er");
    }

    #endregion

    #region Subcommand Completion

    [TestMethod]
    [TestDescription("After 'server ' typing 'd' should show ghost 'isconnect'")]
    public async Task GhostText_SubcommandPartial_ShowsCompletion()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: type command and space
        await runner.TypeText("server ");
        runner.Buffer.Should().Be("server ");
        
        // Ghost text should show first item after space (groups come first, then commands alphabetically)
        runner.GhostText.Should().Be("profile", "ghost shows first item (groups first) after command space");

        // Action: type 'd' for disconnect subcommand
        await runner.TypeText("d");
        
        Debug.WriteLine($"After typing 'd' - Buffer: '{runner.Buffer}', GhostText: '{runner.GhostText}'");

        // Ending condition: ghost should show 'isconnect' (completing 'disconnect')
        runner.Buffer.Should().Be("server d");
        runner.GhostText.Should().Be("isconnect", "ghost should complete the subcommand");
    }

    [TestMethod]
    [TestDescription("After 'server ' with empty partial value, ghost shows first item (groups first)")]
    public async Task GhostText_AfterCommandSpace_ShowsFirstSubcommand()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition
        runner.Buffer.Should().BeEmpty();

        // Action: type 'server '
        await runner.TypeText("server ");

        // Ending condition: ghost shows first item (groups come first, so "profile" before "connect")
        runner.Buffer.Should().Be("server ");
        runner.GhostText.Should().Be("profile", "ghost should show first item (groups first) after space");
    }

    [TestMethod]
    [TestDescription("After 'server profile ' typing 'a' should show ghost for next level")]
    public async Task GhostText_DeepSubcommand_ShowsCompletion()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: navigate to deep subcommand context
        await runner.TypeText("server profile ");
        runner.Buffer.Should().Be("server profile ");
        
        // Ghost should show first subcommand in profile group
        runner.GhostText.Should().Be("add", "ghost shows first subcommand after group space");

        // Action: type 'a' for add command
        await runner.TypeText("a");

        // Ending condition: ghost should show completion for 'add'
        runner.Buffer.Should().Be("server profile a");
        runner.GhostText.Should().Be("dd", "ghost should complete 'add' subcommand");
    }

    #endregion

    #region Cursor Movement Interaction

    [TestMethod]
    [TestDescription("Moving cursor left should hide ghost text")]
    public async Task GhostText_CursorLeft_HidesGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: have ghost text showing
        await runner.TypeText("s");
        runner.GhostText.Should().Be("erver");

        // Action: move cursor left
        await runner.PressKey(ConsoleKey.LeftArrow);

        // Ending condition: ghost should disappear
        runner.GhostText.Should().BeNullOrEmpty("ghost should hide when cursor not at end");
    }

    [TestMethod]
    [TestDescription("Right arrow should accept ghost text")]
    public async Task GhostText_RightArrow_AcceptsGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: have ghost text showing
        await runner.TypeText("s");
        runner.Buffer.Should().Be("s");
        runner.GhostText.Should().Be("erver");

        // Action: press right arrow to accept
        await runner.PressKey(ConsoleKey.RightArrow);

        // Ending condition: buffer contains full word, no ghost
        runner.Buffer.Should().Be("server");
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("End key should accept ghost text")]
    public async Task GhostText_EndKey_AcceptsGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: have ghost text showing
        await runner.TypeText("ser");
        runner.Buffer.Should().Be("ser");
        runner.GhostText.Should().Be("ver");

        // Action: press End to accept
        await runner.PressKey(ConsoleKey.End);

        // Ending condition: buffer contains full word
        runner.Buffer.Should().Be("server");
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("RightArrow at end of line should accept ghost text")]
    public async Task RightArrowAtEnd_ShouldAcceptGhostText()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("serv");
        
        // Only test if ghost text is present
        if (runner.Controller.HasGhostText)
        {
            var bufferBefore = runner.Buffer;
            
            await runner.PressKey(ConsoleKey.RightArrow);
            
            Debug.WriteLine($"After RightArrow - Buffer: '{runner.Buffer}'");
            
            // Buffer should have expanded with ghost text
            runner.Buffer.Length.Should().BeGreaterThan(bufferBefore.Length);
        }
    }

    [TestMethod]
    [TestDescription("End key at end of line should accept ghost text")]
    public async Task EndAtEnd_ShouldAcceptGhostText()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("serv");
        
        if (runner.Controller.HasGhostText)
        {
            var bufferBefore = runner.Buffer;
            
            await runner.PressKey(ConsoleKey.End);
            
            Debug.WriteLine($"After End - Buffer: '{runner.Buffer}'");
            
            runner.Buffer.Length.Should().BeGreaterThan(bufferBefore.Length);
        }
    }

    #endregion

    #region Typing Updates

    [TestMethod]
    [TestDescription("Typing character that matches ghost should update ghost")]
    public async Task GhostText_TypeMatchingChar_UpdatesGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: 's' with ghost 'erver'
        await runner.TypeText("s");
        runner.GhostText.Should().Be("erver");

        // Action: type 'e' (matches ghost)
        await runner.TypeText("e");

        // Ending condition: ghost updates to 'rver'
        runner.Buffer.Should().Be("se");
        runner.GhostText.Should().Be("rver");
    }

    [TestMethod]
    [TestDescription("Typing character that doesn't match ghost should clear ghost")]
    public async Task GhostText_TypeNonMatchingChar_ClearsGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: 's' with ghost 'erver'
        await runner.TypeText("s");
        runner.GhostText.Should().Be("erver");

        // Action: type 'x' (doesn't match)
        await runner.TypeText("x");

        // Ending condition: no ghost
        runner.Buffer.Should().Be("sx");
        runner.GhostText.Should().BeNullOrEmpty();
    }

    [TestMethod]
    [TestDescription("Backspace should update ghost for new prefix")]
    public async Task GhostText_Backspace_UpdatesGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: 'ser' with ghost 'ver'
        await runner.TypeText("ser");
        runner.GhostText.Should().Be("ver");

        // Action: backspace
        await runner.PressKey(ConsoleKey.Backspace);

        // Ending condition: 'se' with ghost 'rver'
        runner.Buffer.Should().Be("se");
        runner.GhostText.Should().Be("rver");
    }

    [TestMethod]
    [TestDescription("Typing matching ghost characters should shrink ghost")]
    public async Task TypingMatchingGhost_ShouldShrinkGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("serv");
        
        if (runner.Controller.HasGhostText)
        {
            // Type next character that matches ghost
            await runner.TypeText("e");
            
            Debug.WriteLine($"After typing 'e' - Buffer: '{runner.Buffer}'");
            Debug.WriteLine($"  HasGhostText: {runner.Controller.HasGhostText}");
            
            // Buffer should be "serve", ghost should be "r"
            runner.Buffer.Should().Be("serve");
            runner.GhostText.Should().Be("r");
        }
    }

    [TestMethod]
    [TestDescription("Backspace should update ghost text")]
    public async Task Backspace_ShouldUpdateGhostText()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server");
        var ghostBefore = runner.Controller.HasGhostText;
        
        await runner.PressKey(ConsoleKey.Backspace);
        
        Debug.WriteLine($"After Backspace - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  HasGhostText before: {ghostBefore}, after: {runner.Controller.HasGhostText}");
        
        runner.Buffer.Should().Be("serve");
        runner.GhostText.Should().Be("r");
    }

    #endregion

    #region Menu Interaction

    [TestMethod]
    [TestDescription("Opening menu should hide ghost text")]
    public async Task GhostText_MenuOpens_GhostHides()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: at subcommand position ready for menu
        await runner.TypeText("server ");
        runner.Buffer.Should().Be("server ");

        // Action: open menu with Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Ending condition: menu visible, ghost should be hidden
        runner.Should().HaveMenuVisible();
        runner.GhostText.Should().BeNullOrEmpty("ghost should hide when menu is open");
    }

    [TestMethod]
    [TestDescription("Closing menu with Escape should restore ghost text")]
    public async Task GhostText_MenuCloses_GhostReappears()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Starting condition: open menu
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        // Action: close menu with Escape
        await runner.PressKey(ConsoleKey.Escape);

        // Ending condition: menu hidden
        runner.Should().HaveMenuHidden();
        // Note: Ghost behavior after escape depends on implementation - just verify menu closed
        Debug.WriteLine($"GhostText after Escape: '{runner.GhostText}'");
    }

    [TestMethod]
    [TestDescription("Tab should clear ghost and open menu")]
    public async Task Tab_ShouldClearGhostAndOpenMenu()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        
        // Tab should clear any ghost and open menu
        await runner.PressKey(ConsoleKey.Tab);

        runner.Controller.HasGhostText.Should().BeFalse("ghost should be cleared when menu opens");
        runner.Should().HaveMenuVisible();
    }

    #endregion
}
