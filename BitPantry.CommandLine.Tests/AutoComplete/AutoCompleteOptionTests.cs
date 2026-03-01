using BitPantry.CommandLine.AutoComplete;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Tests for AutoCompleteOption data object — verifies GetAcceptedValue,
/// GetMenuValue, and MenuStyle behavior.
/// </summary>
[TestClass]
public class AutoCompleteOptionTests
{
    #region GetAcceptedValue Tests

    [TestMethod]
    public void GetAcceptedValue_NullAcceptFormat_ReturnsValue()
    {
        // Arrange
        var option = new AutoCompleteOption("mycommand");

        // Act
        var result = option.GetAcceptedValue();

        // Assert
        result.Should().Be("mycommand");
    }

    [TestMethod]
    public void GetAcceptedValue_WithTrailingSpaceFormat_ReturnsValueWithSpace()
    {
        // Arrange — commands/groups use "{0} " to add trailing space on acceptance
        var option = new AutoCompleteOption("server", acceptFormat: "{0} ");

        // Act
        var result = option.GetAcceptedValue();

        // Assert
        result.Should().Be("server ");
    }

    [TestMethod]
    public void GetAcceptedValue_EmptyAcceptFormat_ReturnsValue()
    {
        // Arrange
        var option = new AutoCompleteOption("value", acceptFormat: "");

        // Act
        var result = option.GetAcceptedValue();

        // Assert — empty format treated as null (no formatting)
        result.Should().Be("value");
    }

    #endregion

    #region GetMenuValue Tests

    [TestMethod]
    public void GetMenuValue_NoMenuFormat_ReturnsValue()
    {
        // Arrange
        var option = new AutoCompleteOption("docs/");

        // Act
        var result = option.GetMenuValue();

        // Assert
        result.Should().Be("docs/");
    }

    [TestMethod]
    public void GetMenuValue_WithMenuFormat_AppliesFormat()
    {
        // Arrange — e.g., profile names use format to show metadata
        var option = new AutoCompleteOption("myprofile", menuFormat: "{0} (default)");

        // Act
        var result = option.GetMenuValue();

        // Assert
        result.Should().Be("myprofile (default)");
    }

    #endregion

    #region MenuStyle Tests

    [TestMethod]
    public void Constructor_WithMenuStyle_SetsMenuStyle()
    {
        // Arrange & Act
        var style = new Style(foreground: Color.Cyan);
        var option = new AutoCompleteOption("docs/", menuStyle: style);

        // Assert
        option.MenuStyle.Should().Be(style);
    }

    [TestMethod]
    public void Constructor_WithoutMenuStyle_MenuStyleIsNull()
    {
        // Arrange & Act
        var option = new AutoCompleteOption("file.txt");

        // Assert
        option.MenuStyle.Should().BeNull();
    }

    #endregion
}
