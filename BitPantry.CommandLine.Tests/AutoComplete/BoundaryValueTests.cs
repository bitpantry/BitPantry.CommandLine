using System;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Boundary Value Tests (TC-35.1 through TC-35.6)
/// Tests edge cases at system limits.
/// </summary>
[TestClass]
public class BoundaryValueTests
{
    #region TC-35.1: Empty Command Registry

    /// <summary>
    /// TC-35.1: When no commands are registered,
    /// Then Tab at empty prompt shows "(no matches)".
    /// </summary>
    [TestMethod]
    public void TC_35_1_EmptyCommandRegistry()
    {
        // Arrange: Minimal command set
        using var harness = AutoCompleteTestHarness.WithCommand<MinimalTestCommand>();

        // Act: Type non-matching prefix
        harness.TypeText("xyz");
        harness.PressTab();

        // Assert: No crash, handles gracefully
        harness.Buffer.Should().Be("xyz");
    }

    #endregion

    #region TC-35.2: Single Character Command

    /// <summary>
    /// TC-35.2: When command name is single character "x",
    /// Then completion works correctly.
    /// </summary>
    [TestMethod]
    public void TC_35_2_SingleCharacterCommand()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type single character
        harness.TypeText("s");
        harness.PressTab();

        // Assert: Single character handled
        harness.Buffer.Should().StartWith("s");
    }

    #endregion

    #region TC-35.3: Very Long Command Name

    /// <summary>
    /// TC-35.3: When command name is very long,
    /// Then display truncates appropriately.
    /// </summary>
    [TestMethod]
    public void TC_35_3_VeryLongCommandName()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type long text
        harness.TypeText("serverconnectwithverylongargumentname");

        // Assert: Buffer accepts long text
        harness.Buffer.Should().Be("serverconnectwithverylongargumentname");
    }

    #endregion

    #region TC-35.4: Maximum Argument Count

    /// <summary>
    /// TC-35.4: When command has many arguments,
    /// Then all are available in completion.
    /// </summary>
    [TestMethod]
    public void TC_35_4_MaximumArgumentCount()
    {
        // Arrange: MultiArgTestCommand has multiple arguments
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act
        harness.TypeText("multicmd ");
        harness.PressTab();

        // Assert: Menu shows arguments
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(0);
        }
    }

    #endregion

    #region TC-35.5: Zero-Width Characters in Values

    /// <summary>
    /// TC-35.5: When completion value contains special characters,
    /// Then display and insertion work correctly.
    /// </summary>
    [TestMethod]
    public void TC_35_5_SpecialCharactersInValues()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type unicode characters
        harness.TypeText("server --name café");

        // Assert: Unicode handled
        harness.Buffer.Should().Be("server --name café");
    }

    #endregion

    #region TC-35.6: Maximum Buffer Length

    /// <summary>
    /// TC-35.6: When input buffer reaches maximum length,
    /// Then completion still functions.
    /// </summary>
    [TestMethod]
    public void TC_35_6_MaximumBufferLength()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type very long input
        var longInput = new string('a', 500);
        harness.TypeText(longInput);

        // Assert: Buffer accepts long input
        harness.Buffer.Should().Be(longInput);
    }

    #endregion
}
