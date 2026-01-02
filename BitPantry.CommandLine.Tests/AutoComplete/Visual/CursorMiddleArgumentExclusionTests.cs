using BitPantry.CommandLine.Tests.VirtualConsole;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for excluding already-used arguments when cursor is in the middle of input.
/// 
/// BUG: When cursor is positioned before an existing argument in the input line,
/// that argument is not excluded from autocomplete suggestions because the
/// UsedArgumentTracker only looks at text BEFORE the cursor.
/// 
/// Example scenario:
/// 1. User types "server connect --ApiKey "
/// 2. User moves cursor back to after "connect": "server connect| --ApiKey "
/// 3. User presses space and tab: "server connect | --ApiKey "
/// 4. BUG: Menu shows --ApiKey even though it's already in the input
/// 5. User can add duplicate: "server connect --ApiKey --ApiKey "
/// 
/// FIX: UsedArgumentTracker should scan the ENTIRE input line, not just before cursor.
/// </summary>
[TestClass]
public class CursorMiddleArgumentExclusionTests : VisualTestBase
{
    #region BUG REPRO: Argument after cursor not excluded

    [TestMethod]
    [TestDescription("BUG REPRO: Menu should exclude argument that appears AFTER cursor position")]
    public async Task Menu_ShouldExcludeArgumentAfterCursor()
    {
        // ARRANGE: Create test environment
        using var runner = CreateRunner();
        runner.Initialize();

        // Step 1: Type "server connect --ApiKey "
        await runner.TypeText("server connect --ApiKey ");
        runner.Buffer.Should().Be("server connect --ApiKey ");
        runner.BufferPosition.Should().Be(24);

        Debug.WriteLine($"After typing: Buffer='{runner.Buffer}', Position={runner.BufferPosition}");

        // Step 2: Move cursor back to after "connect" (position 14)
        // "server connect --ApiKey "
        //               ^-- position 14
        // Need to move left 10 times (from position 24 to position 14)
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.LeftArrow);
        }

        runner.BufferPosition.Should().Be(14);
        Debug.WriteLine($"After moving: Buffer='{runner.Buffer}', Position={runner.BufferPosition}");

        // Step 3: Type a space to separate from existing args
        await runner.TypeText(" ");
        runner.Buffer.Should().Be("server connect  --ApiKey ");
        runner.BufferPosition.Should().Be(15);
        Debug.WriteLine($"After space: Buffer='{runner.Buffer}', Position={runner.BufferPosition}");

        // Step 4: Press Tab to open the autocomplete menu
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After Tab: MenuVisible={runner.IsMenuVisible}");
        if (runner.IsMenuVisible)
        {
            var menuItems = runner.GetMenuItems();
            Debug.WriteLine($"Menu items: {string.Join(", ", menuItems)}");
        }

        // ASSERT: Menu should be visible
        runner.Should().HaveMenuVisible("Tab should open menu for argument suggestions");

        // ASSERT: Menu should NOT contain --ApiKey since it's already in the input
        var items = runner.GetMenuItems();
        items.Should().NotContain("--ApiKey",
            "autocomplete should NOT suggest --ApiKey because it already exists after the cursor");
        
        // Verify other arguments ARE available
        items.Should().Contain("--host", "other arguments should still be available");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Ghost should not suggest argument that appears AFTER cursor")]
    public async Task Ghost_ShouldNotSuggestArgumentAfterCursor()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server connect --host localhost " (host is used)
        await runner.TypeText("server connect --host localhost ");
        runner.Buffer.Should().Be("server connect --host localhost ");

        // Move cursor back to after "connect"
        // "server connect --host localhost "
        //               ^-- position 14, move back 18 positions
        for (int i = 0; i < 18; i++)
        {
            await runner.PressKey(ConsoleKey.LeftArrow);
        }

        runner.BufferPosition.Should().Be(14);

        // Type space and start typing "--h" 
        await runner.TypeText(" --h");
        
        Debug.WriteLine($"Buffer='{runner.Buffer}', Position={runner.BufferPosition}, Ghost='{runner.GhostText}'");

        // Ghost should NOT complete to "host" since it's already used after cursor
        runner.GhostText.Should().NotBe("ost",
            "ghost should not suggest 'host' because --host already exists after cursor");
    }

    [TestMethod]
    [TestDescription("Argument BEFORE cursor should still be excluded")]
    public async Task Menu_ShouldStillExcludeArgumentBeforeCursor()
    {
        // ARRANGE: Verify backward compatibility - args before cursor still excluded
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server connect --host localhost "
        await runner.TypeText("server connect --host localhost ");
        runner.Buffer.Should().Be("server connect --host localhost ");

        // Press Tab to open menu (cursor is at end)
        await runner.PressKey(ConsoleKey.Tab);

        runner.Should().HaveMenuVisible("Tab should open menu");

        var items = runner.GetMenuItems();
        items.Should().NotContain("--host",
            "autocomplete should exclude --host which appears before cursor");
    }

    [TestMethod]
    [TestDescription("Multiple arguments both before and after cursor should be excluded")]
    public async Task Menu_ShouldExcludeArgumentsBothBeforeAndAfterCursor()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server connect --host localhost --port 8080 "
        // Length = 44, cursor at end = 44
        await runner.TypeText("server connect --host localhost --port 8080 ");
        runner.Buffer.Should().Be("server connect --host localhost --port 8080 ");
        runner.BufferPosition.Should().Be(44);

        // Move cursor back to between "localhost " and "--port"
        // "server connect --host localhost --port 8080 "
        //                                  ^-- position 33
        // But we want to be AT the space, position 32 (one char before --port)
        // Moving 12 from 44 = 32
        for (int i = 0; i < 12; i++)
        {
            await runner.PressKey(ConsoleKey.LeftArrow);
        }

        runner.BufferPosition.Should().Be(32);

        // Type space and press Tab  
        // This inserts space at position 32, cursor moves to 33
        // Buffer becomes "server connect --host localhost  --port 8080 " (45 chars)
        // Now --port starts at position 34
        await runner.TypeText(" ");
        await runner.PressKey(ConsoleKey.Tab);

        Debug.WriteLine($"Buffer='{runner.Buffer}', Position={runner.BufferPosition}");
        
        runner.Should().HaveMenuVisible("Tab should open menu");

        var items = runner.GetMenuItems();
        
        // Both --host (before cursor) and --port (after cursor) should be excluded
        items.Should().NotContain("--host",
            "--host before cursor should be excluded");
        items.Should().NotContain("--port",
            "--port after cursor should be excluded");
        
        // Other args should still be available
        items.Should().Contain("--ApiKey",
            "--ApiKey should still be available");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    [TestDescription("Alias used after cursor should exclude corresponding argument name")]
    public async Task Menu_ShouldExcludeArgumentWhenAliasUsedAfterCursor()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server connect -p 8080 " (-p is alias for --port)
        // Length = 23, cursor at end = 23
        await runner.TypeText("server connect -p 8080 ");
        runner.Buffer.Should().Be("server connect -p 8080 ");
        runner.BufferPosition.Should().Be(23);
        
        // Move cursor back to after "connect" (position 14)
        // "server connect -p 8080 "
        //               ^-- position 14
        // Need to move left 9 times (from 23 to 14)
        for (int i = 0; i < 9; i++)
        {
            await runner.PressKey(ConsoleKey.LeftArrow);
        }

        runner.BufferPosition.Should().Be(14);

        // Type space and press Tab
        await runner.TypeText(" ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.Should().HaveMenuVisible("Tab should open menu");

        var items = runner.GetMenuItems();
        
        // --port should be excluded because its alias -p is used after cursor
        items.Should().NotContain("--port",
            "--port should be excluded because -p alias is used after cursor");
    }

    [TestMethod]
    [TestDescription("Cursor at very beginning should still see all used args")]
    public async Task Menu_CursorAtBeginning_ShouldExcludeAllUsedArgs()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server connect --host localhost"
        await runner.TypeText("server connect --host localhost");

        // Move cursor to very beginning (after command name at least)
        await runner.PressKey(ConsoleKey.Home);
        
        // Move to after "server connect"
        for (int i = 0; i < 14; i++)
        {
            await runner.PressKey(ConsoleKey.RightArrow);
        }

        runner.BufferPosition.Should().Be(14);

        // Type space and Tab
        await runner.TypeText(" ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.Should().HaveMenuVisible("Tab should open menu");

        var items = runner.GetMenuItems();
        items.Should().NotContain("--host",
            "--host should be excluded even when cursor is at beginning");
    }

    #endregion
}
