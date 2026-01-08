using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.VirtualConsole.AnsiParser;
using System;

namespace BitPantry.VirtualConsole.Tests;

[TestClass]
public class SgrProcessorTests
{
    private ScreenBuffer CreateBuffer() => new ScreenBuffer(80, 25);

    // T044: Tests for SgrProcessor foreground colors (30-37, 90-97)
    [TestMethod]
    public void Process_ForegroundBlack_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 30 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.Black);
    }

    [TestMethod]
    public void Process_ForegroundRed_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 31 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void Process_ForegroundGreen_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 32 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkGreen);
    }

    [TestMethod]
    public void Process_ForegroundYellow_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 33 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkYellow);
    }

    [TestMethod]
    public void Process_ForegroundBlue_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 34 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
    }

    [TestMethod]
    public void Process_ForegroundMagenta_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 35 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkMagenta);
    }

    [TestMethod]
    public void Process_ForegroundCyan_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 36 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkCyan);
    }

    [TestMethod]
    public void Process_ForegroundWhite_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 37 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.Gray);
    }

    [TestMethod]
    public void Process_BrightForegroundBlack_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 90 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkGray);
    }

    [TestMethod]
    public void Process_BrightForegroundRed_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 91 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.Red);
    }

    [TestMethod]
    public void Process_BrightForegroundBlue_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 94 }, 'm'), buffer);
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.Blue);
    }

    // T045: Tests for SgrProcessor background colors (40-47, 100-107)
    [TestMethod]
    public void Process_BackgroundBlack_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 40 }, 'm'), buffer);
        
        buffer.CurrentStyle.BackgroundColor.Should().Be(ConsoleColor.Black);
    }

    [TestMethod]
    public void Process_BackgroundRed_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 41 }, 'm'), buffer);
        
        buffer.CurrentStyle.BackgroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void Process_BackgroundBlue_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 44 }, 'm'), buffer);
        
        buffer.CurrentStyle.BackgroundColor.Should().Be(ConsoleColor.DarkBlue);
    }

    [TestMethod]
    public void Process_BrightBackgroundRed_ShouldSetColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 101 }, 'm'), buffer);
        
        buffer.CurrentStyle.BackgroundColor.Should().Be(ConsoleColor.Red);
    }

    // T046: Tests for SgrProcessor attributes (bold, italic, underline, reverse, etc.)
    [TestMethod]
    public void Process_Bold_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 1 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Dim_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 2 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Dim).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Italic_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 3 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Italic).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Underline_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 4 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Blink_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 5 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Blink).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Reverse_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 7 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Reverse).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Hidden_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 8 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Hidden).Should().BeTrue();
    }

    [TestMethod]
    public void Process_Strikethrough_ShouldSetAttribute()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 9 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Strikethrough).Should().BeTrue();
    }

    // T047: Tests for SgrProcessor reset (code 0)
    [TestMethod]
    public void Process_Reset_ShouldClearAllAttributesAndColors()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // Set various attributes and colors
        processor.Process(new CsiSequence(new[] { 1, 34, 41 }, 'm'), buffer);
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        
        // Reset
        processor.Process(new CsiSequence(new[] { 0 }, 'm'), buffer);
        
        buffer.CurrentStyle.Should().Be(CellStyle.Default);
    }

    [TestMethod]
    public void Process_NoParameters_ShouldReset()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 1 }, 'm'), buffer);
        processor.Process(new CsiSequence(Array.Empty<int>(), 'm'), buffer);
        
        buffer.CurrentStyle.Should().Be(CellStyle.Default);
    }

    // T048: Tests for SgrProcessor default foreground (39) and background (49)
    [TestMethod]
    public void Process_DefaultForeground_ShouldResetForegroundOnly()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 34, 41 }, 'm'), buffer); // Blue FG, Red BG
        processor.Process(new CsiSequence(new[] { 39 }, 'm'), buffer); // Reset FG
        
        buffer.CurrentStyle.ForegroundColor.Should().BeNull();
        buffer.CurrentStyle.BackgroundColor.Should().Be(ConsoleColor.DarkRed);
    }

    [TestMethod]
    public void Process_DefaultBackground_ShouldResetBackgroundOnly()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 34, 41 }, 'm'), buffer); // Blue FG, Red BG
        processor.Process(new CsiSequence(new[] { 49 }, 'm'), buffer); // Reset BG
        
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
        buffer.CurrentStyle.BackgroundColor.Should().BeNull();
    }

    // T049: Tests for SgrProcessor attribute removal codes (22-29)
    [TestMethod]
    public void Process_NormalIntensity_ShouldRemoveBoldAndDim()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 1, 2 }, 'm'), buffer); // Bold and Dim
        processor.Process(new CsiSequence(new[] { 22 }, 'm'), buffer); // Normal intensity
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Bold).Should().BeFalse();
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Dim).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotItalic_ShouldRemoveItalic()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 3 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 23 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Italic).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotUnderline_ShouldRemoveUnderline()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 4 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 24 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Underline).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotBlink_ShouldRemoveBlink()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 5 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 25 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Blink).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotReverse_ShouldRemoveReverse()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 7 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 27 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Reverse).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotHidden_ShouldRemoveHidden()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 8 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 28 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Hidden).Should().BeFalse();
    }

    [TestMethod]
    public void Process_NotStrikethrough_ShouldRemoveStrikethrough()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        processor.Process(new CsiSequence(new[] { 9 }, 'm'), buffer);
        processor.Process(new CsiSequence(new[] { 29 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Strikethrough).Should().BeFalse();
    }

    // T050: Tests for 256-color mode (38;5;n and 48;5;n)
    [TestMethod]
    public void Process_256ColorForeground_ShouldSetExtendedColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // 38;5;196 = bright red in 256-color palette
        processor.Process(new CsiSequence(new[] { 38, 5, 196 }, 'm'), buffer);
        
        buffer.CurrentStyle.Foreground256.Should().Be(196);
    }

    [TestMethod]
    public void Process_256ColorBackground_ShouldSetExtendedColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // 48;5;21 = blue in 256-color palette
        processor.Process(new CsiSequence(new[] { 48, 5, 21 }, 'm'), buffer);
        
        buffer.CurrentStyle.Background256.Should().Be(21);
    }

    // T050a: Tests for 24-bit TrueColor mode (38;2;r;g;b and 48;2;r;g;b)
    [TestMethod]
    public void Process_TrueColorForeground_ShouldSetRgbColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // 38;2;255;128;64 = RGB(255, 128, 64)
        processor.Process(new CsiSequence(new[] { 38, 2, 255, 128, 64 }, 'm'), buffer);
        
        var rgb = buffer.CurrentStyle.ForegroundRgb;
        rgb.Should().NotBeNull();
        rgb!.Value.R.Should().Be(255);
        rgb!.Value.G.Should().Be(128);
        rgb!.Value.B.Should().Be(64);
    }

    [TestMethod]
    public void Process_TrueColorBackground_ShouldSetRgbColor()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // 48;2;0;128;255 = RGB(0, 128, 255)
        processor.Process(new CsiSequence(new[] { 48, 2, 0, 128, 255 }, 'm'), buffer);
        
        var rgb = buffer.CurrentStyle.BackgroundRgb;
        rgb.Should().NotBeNull();
        rgb!.Value.R.Should().Be(0);
        rgb!.Value.G.Should().Be(128);
        rgb!.Value.B.Should().Be(255);
    }

    [TestMethod]
    public void Process_MultipleParameters_ShouldApplyAll()
    {
        var buffer = CreateBuffer();
        var processor = new SgrProcessor();
        
        // 1;4;34 = Bold, Underline, Blue
        processor.Process(new CsiSequence(new[] { 1, 4, 34 }, 'm'), buffer);
        
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Bold).Should().BeTrue();
        buffer.CurrentStyle.Attributes.HasFlag(CellAttributes.Underline).Should().BeTrue();
        buffer.CurrentStyle.ForegroundColor.Should().Be(ConsoleColor.DarkBlue);
    }

    [TestMethod]
    public void CanProcess_ShouldReturnTrueForSgrCommand()
    {
        var processor = new SgrProcessor();
        
        processor.CanProcess('m').Should().BeTrue();
    }

    [TestMethod]
    public void CanProcess_ShouldReturnFalseForNonSgrCommands()
    {
        var processor = new SgrProcessor();
        
        processor.CanProcess('A').Should().BeFalse();
        processor.CanProcess('H').Should().BeFalse();
        processor.CanProcess('J').Should().BeFalse();
    }
}
