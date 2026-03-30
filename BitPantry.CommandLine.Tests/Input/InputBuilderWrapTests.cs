using System;
using System.Threading.Tasks;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Description = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Input;

/// <summary>
/// Integration tests for line-wrapping behavior through the FULL InputBuilder pipeline.
/// These tests simulate the exact flow: ConsoleInputInterceptor → Write(char) → OnKeyPressed → RenderWithStyles.
/// 
/// Tests issue #36: cursor position desync when pasted text wraps past terminal width.
/// </summary>
[TestClass]
public class InputBuilderWrapTests
{
    #region Test Commands

    [Command(Name = "test")]
    [Description("Test command")]
    private class TestCommand : CommandBase
    {
        [Argument]
        [Description("Input value")]
        public string Value { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    private TestEnvironment CreateNarrowEnvironment(int width = 80)
    {
        return new TestEnvironment(opt =>
        {
            opt.ConsoleWidth = width;
            opt.ConsoleHeight = 24;
            opt.ConfigureCommands(builder =>
            {
                builder.RegisterCommand<TestCommand>();
            });
        });
    }

    /// <summary>
    /// Simulates the exact sandbox paste scenario: type "test " then paste enough text
    /// to wrap past the terminal width. Verifies cursor position after each character.
    /// 
    /// Bug: In real terminal, pasted text "never goes to the next line, pushes the
    /// input left off the screen."
    /// 
    /// This test goes through the full InputBuilder pipeline.
    /// </summary>
    [TestMethod]
    public async Task TypeLongText_ThroughFullPipeline_CursorWrapsCorrectly()
    {
        // Arrange — 80-col terminal like a real terminal
        using var env = CreateNarrowEnvironment(80);
        var vc = env.Console.VirtualConsole;
        var promptLen = 10; // "testhost> " = 10 chars

        // Act — type "test " then a long value. Prompt(10) + "test "(5) = 15, then 65 more
        // to hit exactly 80 (wrap boundary), then 10 more to go onto row 1
        await env.Keyboard.TypeTextAsync("test ");
        vc.CursorColumn.Should().Be(promptLen + 5, "after typing 'test ': cursor at col 15");

        // Type enough to wrap. Chars 15+1...15+65 = offsets 16...80
        var pasteText = new string('A', 75); // 15 + 75 = 90, wraps to row 1
        await env.Keyboard.TypeTextAsync(pasteText);

        // Assert — cursor should be on row 1 after wrapping
        int totalOffset = promptLen + 5 + pasteText.Length; // 10 + 5 + 75 = 90
        int expectedRow = totalOffset / 80; // 90/80 = 1
        int expectedCol = totalOffset % 80; // 90%80 = 10

        vc.CursorRow.Should().Be(expectedRow,
            $"after typing 80 chars of content: cursor should wrap to row {expectedRow}");
        vc.CursorColumn.Should().Be(expectedCol,
            $"after typing: cursor should be at col {expectedCol}");
    }

    /// <summary>
    /// Type enough text to wrap, then backspace back across the row boundary.
    /// Verifies cursor never goes above the prompt row.
    /// 
    /// Bug: In real terminal, "backspace deletes back to column 1, then jumps to
    /// last column row-1" and eventually escapes above the prompt.
    /// </summary>
    [TestMethod]
    public async Task BackspaceAcrossWrap_ThroughFullPipeline_CursorCorrect()
    {
        // Arrange — 80-col terminal
        using var env = CreateNarrowEnvironment(80);
        var vc = env.Console.VirtualConsole;
        var promptLen = 10;

        // Type "test " + 70 chars of value = 10 + 75 = 85 total offset
        await env.Keyboard.TypeTextAsync("test ");
        var valueText = new string('B', 70);
        await env.Keyboard.TypeTextAsync(valueText);

        // Verify we're on row 1
        int totalOffset = promptLen + 5 + valueText.Length; // 85
        vc.CursorRow.Should().Be(totalOffset / 80, "should be on row 1 (85/80=1)");

        // Act — backspace 10 times (should cross from row 1 back to row 0)
        for (int i = 0; i < 10; i++)
        {
            await env.Keyboard.PressBackspaceAsync();

            int newOffset = totalOffset - (i + 1);
            int expRow = newOffset / 80;
            int expCol = newOffset % 80;

            vc.CursorRow.Should().Be(expRow,
                $"after backspace {i + 1}: offset={newOffset}, expected row={expRow}");
            vc.CursorColumn.Should().Be(expCol,
                $"after backspace {i + 1}: offset={newOffset}, expected col={expCol}");
        }
    }

    /// <summary>
    /// Type text that spans 3 rows (like the user's reported scenario with the sandbox prompt).
    /// Verifies wrapping works across multiple row boundaries.
    /// </summary>
    [TestMethod]
    public async Task TypeText_ThreeRows_CursorTracksCorrectly()
    {
        // 40-col terminal for easier testing
        using var env = CreateNarrowEnvironment(40);
        var vc = env.Console.VirtualConsole;
        var promptLen = 10; // "testhost> " = 10 chars

        // Type "test " (5 chars) + value text
        await env.Keyboard.TypeTextAsync("test ");

        // Need to fill: row 0 (40 - 15 = 25 more), row 1 (40), row 2 (partial)
        // Total content = 25 + 40 + 5 = 70 chars of value 
        var valueText = new string('C', 70);
        await env.Keyboard.TypeTextAsync(valueText);

        // Verify final cursor position
        int totalOffset = promptLen + 5 + 70; // 85
        int expRow = totalOffset / 40; // 2
        int expCol = totalOffset % 40; // 5

        vc.CursorRow.Should().Be(expRow, "should be on row 2");
        vc.CursorColumn.Should().Be(expCol, "should be at col 5");

        // Now backspace back to the start
        for (int i = 0; i < 70; i++)
        {
            await env.Keyboard.PressBackspaceAsync();
        }

        // After backspacing all value, cursor should be after "test "
        int finalOffset = promptLen + 5; // 15
        vc.CursorRow.Should().Be(0, "after backspacing all value: row 0");
        vc.CursorColumn.Should().Be(15, "after backspacing all value: col 15");
    }
}
