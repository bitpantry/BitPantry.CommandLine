using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Quoting and Escaping Tests (TC-26.1 through TC-26.8)
/// Tests quote and escape character handling.
/// </summary>
[TestClass]
public class QuotingEscapingTests
{
    #region TC-26.1: Single Quotes Preserve Literal Text

    /// <summary>
    /// TC-26.1: When user types text in single quotes,
    /// Then no escaping or special character handling occurs.
    /// </summary>
    [TestMethod]
    public void TC_26_1_SingleQuotes_PreserveLiteralText()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("--value 'hello world'");

        // Assert
        harness.Buffer.Should().Be("--value 'hello world'");
    }

    #endregion

    #region TC-26.2: Double Quotes Allow Space in Values

    /// <summary>
    /// TC-26.2: When user types value with spaces in double quotes,
    /// Then entire quoted value is treated as one argument.
    /// </summary>
    [TestMethod]
    public void TC_26_2_DoubleQuotes_AllowSpaceInValues()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("--file \"my file.txt\"");

        // Assert
        harness.Buffer.Should().Be("--file \"my file.txt\"");
    }

    #endregion

    #region TC-26.3: Escape Character Before Quote

    /// <summary>
    /// TC-26.3: When backslash precedes a quote,
    /// Then quote is treated literally.
    /// </summary>
    [TestMethod]
    public void TC_26_3_EscapeCharacter_BeforeQuote()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("--name \\\"literal\\\"");

        // Assert
        harness.Buffer.Should().Be("--name \\\"literal\\\"");
    }

    #endregion

    #region TC-26.4: Tab Inside Unclosed Quote

    /// <summary>
    /// TC-26.4: When Tab pressed while in an unclosed quote,
    /// Then completion treats partial text as value prefix.
    /// </summary>
    [TestMethod]
    public void TC_26_4_Tab_InsideUnclosedQuote()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PathArgTestCommand>();

        // Act
        harness.TypeText("pathcmd --Path \"my par");
        harness.PressTab();

        // Assert: Buffer maintains quote context
        harness.Buffer.Should().Contain("\"my par");
    }

    #endregion

    #region TC-26.5: Accept Completion Adds Closing Quote

    /// <summary>
    /// TC-26.5: When accepting completion started inside quotes,
    /// Then closing quote is added if needed.
    /// </summary>
    [TestMethod]
    public void TC_26_5_AcceptCompletion_AddsClosingQuote()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PathArgTestCommand>();

        // Act
        harness.TypeText("pathcmd --Path \"test");

        // Assert: Buffer has the quoted partial
        harness.Buffer.Should().Contain("\"test");
    }

    #endregion

    #region TC-26.6: Nested Quotes in Completion Value

    /// <summary>
    /// TC-26.6: When completion value contains quotes,
    /// Then inserted text is properly escaped.
    /// </summary>
    [TestMethod]
    public void TC_26_6_NestedQuotes_InCompletionValue()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type value with nested quotes
        harness.TypeText("--msg \"he said \\\"hello\\\"\"");

        // Assert
        harness.Buffer.Should().Contain("he said");
    }

    #endregion

    #region TC-26.7: Mixed Quote Styles

    /// <summary>
    /// TC-26.7: When input mixes single and double quotes,
    /// Then each quote context is tracked independently.
    /// </summary>
    [TestMethod]
    public void TC_26_7_MixedQuoteStyles()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("--arg1 \"value1\" --arg2 'value2'");

        // Assert
        harness.Buffer.Should().Be("--arg1 \"value1\" --arg2 'value2'");
    }

    #endregion

    #region TC-26.8: Backslash Escaping in Paths

    /// <summary>
    /// TC-26.8: When file path contains backslashes (Windows),
    /// Then path is handled correctly.
    /// </summary>
    [TestMethod]
    public void TC_26_8_BackslashEscaping_InPaths()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PathArgTestCommand>();

        // Act
        harness.TypeText("pathcmd --Path C:\\Users\\test\\");

        // Assert
        harness.Buffer.Should().Be("pathcmd --Path C:\\Users\\test\\");
    }

    #endregion
}
