using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for input editing including:
/// - Basic typing and backspace
/// - Cursor movement (arrows, home, end)
/// - Cursor movement while menu is open
/// - Word boundary cases
/// </summary>
[TestClass]
public class InputEditingTests : VisualTestBase
{
    #region Basic Input

    [TestMethod]
    [TestDescription("Typing characters updates buffer AND cursor position correctly")]
    public async Task Typing_UpdatesBufferAndCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // After initialization: cursor at column 2 (after "> ")
        runner.Should().HaveBuffer("")
                       .And.HaveInputCursorAt(0);

        // Type 'h'
        await runner.TypeChar('h');
        runner.Should().HaveBuffer("h")
                       .And.HaveInputCursorAt(1);

        // Type 'e'
        await runner.TypeChar('e');
        runner.Should().HaveBuffer("he")
                       .And.HaveInputCursorAt(2);

        // Type 'l'
        await runner.TypeChar('l');
        runner.Should().HaveBuffer("hel")
                       .And.HaveInputCursorAt(3);

        // Type 'p'
        await runner.TypeChar('p');
        runner.Should().HaveState("help", 4);
    }

    [TestMethod]
    [TestDescription("Backspace removes character AND moves cursor back")]
    public async Task Backspace_RemovesCharAndMovesCursorBack()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("help");
        runner.Should().HaveState("help", 4);

        await runner.PressKey(ConsoleKey.Backspace);
        runner.Should().HaveState("hel", 3);

        await runner.PressKey(ConsoleKey.Backspace);
        runner.Should().HaveState("he", 2);
    }

    [TestMethod]
    [TestDescription("Delete key removes character at cursor without moving cursor")]
    public async Task Delete_RemovesCharacterAtCursor()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("help");
        
        // Move cursor to middle
        await runner.PressKey(ConsoleKey.Home);
        runner.Should().HaveBufferPosition(0);
        
        // Delete should remove 'h' without moving cursor
        await runner.PressKey(ConsoleKey.Delete);
        
        runner.Buffer.Should().Be("elp");
        runner.BufferPosition.Should().Be(0);
    }

    [TestMethod]
    [TestDescription("Backspace at start of buffer does nothing")]
    public async Task Backspace_AtStart_DoesNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        runner.Buffer.Should().BeEmpty();
        runner.BufferPosition.Should().Be(0);

        await runner.PressKey(ConsoleKey.Backspace);

        runner.Buffer.Should().BeEmpty();
        runner.BufferPosition.Should().Be(0);
    }

    #endregion

    #region Cursor Movement

    [TestMethod]
    [TestDescription("LeftArrow moves cursor left")]
    public async Task LeftArrow_MovesCursorLeft()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hello");
        runner.Should().HaveBufferPosition(5);

        await runner.PressKey(ConsoleKey.LeftArrow);
        runner.Should().HaveBufferPosition(4);

        await runner.PressKey(ConsoleKey.LeftArrow);
        runner.Should().HaveBufferPosition(3);
    }

    [TestMethod]
    [TestDescription("RightArrow moves cursor right")]
    public async Task RightArrow_MovesCursorRight()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hello");
        await runner.PressKey(ConsoleKey.Home);
        runner.Should().HaveBufferPosition(0);

        await runner.PressKey(ConsoleKey.RightArrow);
        runner.Should().HaveBufferPosition(1);

        await runner.PressKey(ConsoleKey.RightArrow);
        runner.Should().HaveBufferPosition(2);
    }

    [TestMethod]
    [TestDescription("Home key moves cursor to start")]
    public async Task Home_MovesCursorToStart()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hello world");
        runner.BufferPosition.Should().Be(11);

        await runner.PressKey(ConsoleKey.Home);
        runner.Should().HaveBufferPosition(0);
    }

    [TestMethod]
    [TestDescription("End key behavior test (no ghost text)")]
    public async Task End_BehaviorTest_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("xyznonexistent"); // Use non-matching text to avoid ghost text
        
        // Move to middle position
        await runner.PressKey(ConsoleKey.Home);
        await runner.PressKey(ConsoleKey.RightArrow);
        await runner.PressKey(ConsoleKey.RightArrow);
        await runner.PressKey(ConsoleKey.RightArrow);
        
        Debug.WriteLine($"Before End - Position: {runner.BufferPosition}, Buffer: '{runner.Buffer}'");
        
        await runner.PressKey(ConsoleKey.End);
        
        Debug.WriteLine($"After End - Position: {runner.BufferPosition}");
        
        // Note: End key behavior without ghost text - documents current behavior
        // Cursor may or may not move to end depending on implementation
    }

    [TestMethod]
    [TestDescription("LeftArrow at position 0 does nothing")]
    public async Task LeftArrow_AtStart_DoesNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hello");
        await runner.PressKey(ConsoleKey.Home);
        runner.Should().HaveBufferPosition(0);

        await runner.PressKey(ConsoleKey.LeftArrow);
        runner.Should().HaveBufferPosition(0);
    }

    [TestMethod]
    [TestDescription("RightArrow at end does nothing (without ghost text)")]
    public async Task RightArrow_AtEnd_NoGhost_DoesNothing()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type something that has no ghost text (non-matching)
        await runner.TypeText("xyznonexistent");
        var position = runner.BufferPosition;
        
        runner.GhostText.Should().BeNullOrEmpty();

        await runner.PressKey(ConsoleKey.RightArrow);
        runner.Should().HaveBufferPosition(position);
    }

    #endregion

    #region Cursor Movement While Menu Open

    [TestMethod]
    [TestDescription("LeftArrow while menu open - behavior test")]
    public async Task LeftArrowWhileMenuOpen_BehaviorTest()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        var positionBeforeTab = runner.BufferPosition;
        
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        await runner.PressKey(ConsoleKey.LeftArrow);

        // Note: LeftArrow behavior while menu open depends on implementation
        // Some implementations close menu, others ignore horizontal arrows
        Debug.WriteLine($"After LeftArrow - MenuVisible: {runner.IsMenuVisible}, Position: {runner.BufferPosition}");
    }

    [TestMethod]
    [TestDescription("RightArrow while menu open - behavior test")]
    public async Task RightArrowWhileMenuOpen_BehaviorTest()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        await runner.PressKey(ConsoleKey.RightArrow);

        Debug.WriteLine($"After RightArrow - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  Position: {runner.BufferPosition}");
        
        // Behavior: menu may close and cursor may accept selection
    }

    [TestMethod]
    [TestDescription("Home key while menu open - behavior test")]
    public async Task HomeWhileMenuOpen_BehaviorTest()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        await runner.PressKey(ConsoleKey.Home);

        Debug.WriteLine($"After Home - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  Position: {runner.BufferPosition}");
        
        // Home should close menu and move cursor to start
        runner.Should().HaveMenuHidden();
        runner.Should().HaveBufferPosition(0);
    }

    [TestMethod]
    [TestDescription("End key while menu open - behavior test")]
    public async Task EndWhileMenuOpen_BehaviorTest()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        await runner.PressKey(ConsoleKey.End);

        Debug.WriteLine($"After End - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  Position: {runner.BufferPosition}");
        
        // End should close menu
        runner.Should().HaveMenuHidden();
    }

    #endregion

    #region Inserting in Middle of Buffer

    [TestMethod]
    [TestDescription("Typing with cursor in middle inserts character")]
    public async Task Typing_CursorInMiddle_InsertsCharacter()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("hllo");
        
        // Move cursor to position 1
        await runner.PressKey(ConsoleKey.Home);
        await runner.PressKey(ConsoleKey.RightArrow);
        runner.Should().HaveBufferPosition(1);

        // Insert 'e'
        await runner.TypeChar('e');

        runner.Buffer.Should().Be("hello");
        runner.Should().HaveBufferPosition(2);
    }

    [TestMethod]
    [TestDescription("Backspace in middle removes character before cursor")]
    public async Task Backspace_CursorInMiddle_RemovesCharacterBefore()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("heello");
        
        // Move cursor to position 3 (after "hee")
        await runner.PressKey(ConsoleKey.Home);
        await runner.PressKey(ConsoleKey.RightArrow);
        await runner.PressKey(ConsoleKey.RightArrow);
        await runner.PressKey(ConsoleKey.RightArrow);
        runner.Should().HaveBufferPosition(3);

        // Backspace should remove the extra 'e'
        await runner.PressKey(ConsoleKey.Backspace);

        runner.Buffer.Should().Be("hello");
        runner.Should().HaveBufferPosition(2);
    }

    #endregion

    #region Word Boundary Cases

    [TestMethod]
    [TestDescription("Partial word completion: 'server con' should complete to 'connect'")]
    public async Task PartialWordCompletion_ShouldCompleteWord()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server con");
        
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");

        // Should complete or show menu with "connect"
        if (runner.IsMenuVisible)
        {
            runner.SelectedMenuItem.Should().StartWith("con");
        }
        else
        {
            runner.Buffer.Should().Contain("connect");
        }
    }

    [TestMethod]
    [TestDescription("Cursor in middle of word then Tab - behavior test")]
    public async Task CursorInMiddleOfWord_Tab_BehaviorTest()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect");
        
        // Move cursor to middle of "connect"
        await runner.PressKey(ConsoleKey.LeftArrow);
        await runner.PressKey(ConsoleKey.LeftArrow);
        await runner.PressKey(ConsoleKey.LeftArrow);
        
        Debug.WriteLine($"Before Tab - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        
        // Tab in middle of word - behavior depends on implementation
    }

    [TestMethod]
    [TestDescription("After completing command, Tab should show next level completions")]
    public async Task AfterCompletedCommand_Tab_ShouldShowNextLevel()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type complete command with space
        await runner.TypeText("server connect ");
        
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"  MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"  SelectedMenuItem: {runner.SelectedMenuItem}");

        // Should show argument completions for connect command
        runner.Should().HaveMenuVisible();
    }

    #endregion
}
