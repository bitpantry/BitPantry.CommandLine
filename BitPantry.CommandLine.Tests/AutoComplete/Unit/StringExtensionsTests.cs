using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete.Unit;

/// <summary>
/// Unit tests for StringExtensions.IsInsideQuotes helper method.
/// Tests quote counting logic for context-aware menu behavior.
/// </summary>
[TestClass]
public class StringExtensionsTests
{
    #region IsInsideQuotes Tests

    [TestMethod]
    public void IsInsideQuotes_EmptyString_ReturnsFalse()
    {
        // Arrange
        var buffer = "";
        
        // Act
        var result = buffer.IsInsideQuotes(0);
        
        // Assert
        result.Should().BeFalse("empty string has no quotes");
    }

    [TestMethod]
    public void IsInsideQuotes_NoQuotes_ReturnsFalse()
    {
        // Arrange
        var buffer = "server connect --host localhost";
        
        // Act
        var result = buffer.IsInsideQuotes(15);
        
        // Assert
        result.Should().BeFalse("string with no quotes should return false");
    }

    [TestMethod]
    public void IsInsideQuotes_PositionInsideQuotes_ReturnsTrue()
    {
        // Arrange: --path "Program Files"
        //          0123456789...
        //                  ^ position 8 is inside quotes
        var buffer = "--path \"Program Files\"";
        
        // Act - position 8 is after the opening quote, inside the quoted string
        var result = buffer.IsInsideQuotes(8);
        
        // Assert
        result.Should().BeTrue("position 8 is after the opening quote (at position 7)");
    }

    [TestMethod]
    public void IsInsideQuotes_PositionAfterClosingQuote_ReturnsFalse()
    {
        // Arrange: --path "test" --other
        //                      ^ position after closing quote
        var buffer = "--path \"test\" --other";
        
        // Act - position 14 is after the closing quote
        var result = buffer.IsInsideQuotes(14);
        
        // Assert
        result.Should().BeFalse("position after closing quote is outside quotes");
    }

    [TestMethod]
    public void IsInsideQuotes_MultiplePairs_TracksCorrectly()
    {
        // Arrange: "first" "second"
        //          01234567890123456
        //                ^ position 6 (at first closing quote) is inside (1 quote before)
        //                 ^ position 7 (after first closing quote) is outside (2 quotes before)
        //                  ^ position 8 (at second opening quote) is outside (2 quotes before)
        //                   ^ position 9 (inside second pair) is inside (3 quotes before)
        var buffer = "\"first\" \"second\"";
        
        // Act & Assert
        buffer.IsInsideQuotes(1).Should().BeTrue("position 1 is inside first quoted string");
        buffer.IsInsideQuotes(7).Should().BeFalse("position 7 is after first closing quote");
        buffer.IsInsideQuotes(8).Should().BeFalse("position 8 is at second opening quote");
        buffer.IsInsideQuotes(9).Should().BeTrue("position 9 is inside second quoted string");
    }

    [TestMethod]
    public void IsInsideQuotes_PositionAtOpeningQuote_ReturnsFalse()
    {
        // Arrange: --path "test"
        //                ^ position 7 is at the opening quote (0 quotes before)
        var buffer = "--path \"test\"";
        
        // Act - position 7 is AT the opening quote
        var result = buffer.IsInsideQuotes(7);
        
        // Assert
        result.Should().BeFalse("position at opening quote has 0 quotes before it");
    }

    [TestMethod]
    public void IsInsideQuotes_PositionBeyondBuffer_ReturnsFalse()
    {
        // Arrange
        var buffer = "\"test\"";
        
        // Act - position beyond buffer length
        var result = buffer.IsInsideQuotes(100);
        
        // Assert
        result.Should().BeFalse("position beyond buffer should count all quotes (even count)");
    }

    [TestMethod]
    public void IsInsideQuotes_UnmatchedOpeningQuote_ReturnsTrue()
    {
        // Arrange: --path "unclosed
        //                        ^ position is inside unclosed quote
        var buffer = "--path \"unclosed";
        
        // Act
        var result = buffer.IsInsideQuotes(15);
        
        // Assert
        result.Should().BeTrue("unclosed quote means position is still inside");
    }

    [TestMethod]
    public void IsInsideQuotes_MiddleOfQuotedPath_ReturnsTrue()
    {
        // Arrange: --path "C:\Program Files\App"
        var buffer = "--path \"C:\\Program Files\\App\"";
        
        // Act - position in middle of the path
        var result = buffer.IsInsideQuotes(20);
        
        // Assert
        result.Should().BeTrue("position 20 is inside the quoted path");
    }

    #endregion
}
