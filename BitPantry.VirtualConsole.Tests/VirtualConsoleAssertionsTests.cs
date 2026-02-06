using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace BitPantry.VirtualConsole.Tests;

/// <summary>
/// Tests for VirtualConsoleAssertions extension methods.
/// </summary>
[TestClass]
public class VirtualConsoleAssertionsTests
{
    #region HaveCellWithForegroundColor Tests

    // Implements: TI-001
    [TestMethod]
    public void HaveCellWithForegroundColor_CellHasCyanForeground_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[96mCyan");  // ANSI escape 96 = bright cyan (ConsoleColor.Cyan)

        // Act & Assert - should pass without throwing
        console.Should().HaveCellWithForegroundColor(0, 0, ConsoleColor.Cyan);
    }

    // Implements: TI-002
    [TestMethod]
    public void HaveCellWithForegroundColor_ColorMismatch_FailsWithClearMessage()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[93mY");  // ANSI escape 93 = bright yellow (ConsoleColor.Yellow)

        // Act
        Action act = () => console.Should().HaveCellWithForegroundColor(0, 0, ConsoleColor.Cyan);

        // Assert - should fail with message containing expected, actual, position, and character
        act.Should().Throw<Exception>()
            .WithMessage("*Cyan*")      // Expected color
            .WithMessage("*Yellow*")    // Actual color
            .WithMessage("*(0, 0)*")    // Position
            .WithMessage("*'Y'*");      // Character
    }

    #endregion

    #region HaveRangeWithForegroundColor Tests

    // Implements: TI-003
    [TestMethod]
    public void HaveRangeWithForegroundColor_AllCellsHaveCyanForeground_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[96mCYAN");  // 4 characters in cyan

        // Act & Assert - should pass without throwing
        console.Should().HaveRangeWithForegroundColor(0, 0, 4, ConsoleColor.Cyan);
    }

    // Implements: TI-004
    [TestMethod]
    public void HaveRangeWithForegroundColor_MixedColors_FailsIdentifyingFirstMismatch()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[96mCY\x1b[93mAN");  // First 2 cyan, last 2 yellow

        // Act
        Action act = () => console.Should().HaveRangeWithForegroundColor(0, 0, 4, ConsoleColor.Cyan);

        // Assert - should fail identifying the mismatched columns
        act.Should().Throw<Exception>()
            .WithMessage("*columns*2*")     // First mismatch at column 2
            .WithMessage("*Yellow*");       // Actual color at mismatch
    }

    #endregion

    #region HaveCellWithForeground256 Tests

    // Implements: TI-005
    [TestMethod]
    public void HaveCellWithForeground256_CellHas256Color_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;5;214mO");  // 256-color palette index 214 (orange)

        // Act & Assert - should pass without throwing
        console.Should().HaveCellWithForeground256(0, 0, 214);
    }

    #endregion

    #region HaveRangeWithForeground256 Tests

    // Implements: TI-006
    [TestMethod]
    public void HaveRangeWithForeground256_AllCellsHave256Color_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;5;214mORANGE");  // 6 chars in 256-color palette index 214

        // Act & Assert - should pass without throwing
        console.Should().HaveRangeWithForeground256(0, 0, 6, 214);
    }

    #endregion

    #region HaveCellWithFullStyle Tests

    // Implements: TI-007
    [TestMethod]
    public void HaveCellWithFullStyle_StyleMatches_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;5;214mO");  // 256-color palette index 214

        var expectedStyle = new CellStyle(null, null, CellAttributes.None, 214, null, null, null);

        // Act & Assert - should pass without throwing
        console.Should().HaveCellWithFullStyle(0, 0, expectedStyle);
    }

    // Implements: TI-008
    [TestMethod]
    public void HaveCellWithFullStyle_WrongColor_FailsWithSpecificDifference()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("\x1b[38;5;214mO");  // Actual: 256-color palette index 214

        var expectedStyle = new CellStyle(null, null, CellAttributes.None, 100, null, null, null);  // Expected: index 100

        // Act
        Action act = () => console.Should().HaveCellWithFullStyle(0, 0, expectedStyle);

        // Assert - failure message should identify specific difference
        act.Should().Throw<Exception>()
            .WithMessage("*FG256:100*")    // Expected 256-color
            .WithMessage("*FG256:214*")    // Actual 256-color
            .WithMessage("*(0, 0)*")       // Position
            .WithMessage("*'O'*");         // Character
    }

    #endregion

    #region Null/Default Color Handling Tests

    // Implements: TI-009
    [TestMethod]
    public void HaveCellWithForegroundColor_NullForeground_AssertionPasses()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);
        console.Write("X");  // No color escapes - uses default/null foreground

        // Act & Assert - should pass when expecting null (default) color
        console.Should().HaveCellWithForegroundColor(0, 0, null);
    }

    #endregion

    #region Bounds Validation Tests

    // Implements: TI-010
    [TestMethod]
    public void HaveRangeWithForegroundColor_RangeBeyondScreenWidth_ThrowsWithBoundsError()
    {
        // Arrange
        var console = new VirtualConsole(80, 25);  // Width = 80
        console.Write("\x1b[96mTest");

        // Act - try to check range from column 75 with length 10 (would go to column 85)
        Action act = () => console.Should().HaveRangeWithForegroundColor(0, 75, 10, ConsoleColor.Cyan);

        // Assert - should fail with bounds error message
        act.Should().Throw<Exception>()
            .WithMessage("*bounds*")       // Must mention bounds
            .WithMessage("*80*");          // Must mention actual width
    }

    #endregion
}
