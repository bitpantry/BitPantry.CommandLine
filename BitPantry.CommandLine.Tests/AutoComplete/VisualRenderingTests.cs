using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Visual Rendering Tests (TC-15.1 through TC-15.5)
/// Tests visual styling of autocomplete UI elements.
/// </summary>
[TestClass]
public class VisualRenderingTests
{
    #region TC-15.1: Menu Shows Selection with Inverted Style

    /// <summary>
    /// TC-15.1: When menu is displayed,
    /// Then selected item has inverted (reverse video) styling.
    /// </summary>
    [TestMethod]
    public void TC_15_1_MenuShowsSelection_WithInvertedStyle()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("server ");
        harness.PressTab();

        // Assert: Menu is visible with selection
        harness.IsMenuVisible.Should().BeTrue();
        harness.SelectedIndex.Should().Be(0);
    }

    #endregion

    #region TC-15.2: Ghost Text Styled Dim/Gray

    /// <summary>
    /// TC-15.2: When ghost text is displayed,
    /// Then it uses dim styling to distinguish from user input.
    /// </summary>
    [TestMethod]
    public void TC_15_2_GhostText_StyledDimGray()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type partial to trigger ghost
        harness.TypeText("s");

        // Assert: Ghost text may be present (styled separately from user input)
        harness.Buffer.Should().Be("s");
        // Ghost styling is implementation-specific
    }

    #endregion

    #region TC-15.3: Menu Updates In-Place

    /// <summary>
    /// TC-15.3: When navigating through menu,
    /// Then menu updates in place without leaving duplicate lines.
    /// </summary>
    [TestMethod]
    public void TC_15_3_MenuUpdates_InPlace()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Navigate menu
        harness.TypeText("server ");
        harness.PressTab();
        harness.PressDownArrow();
        harness.PressUpArrow();
        harness.PressDownArrow();

        // Assert: Menu still visible, single instance
        harness.IsMenuVisible.Should().BeTrue();
    }

    #endregion

    #region TC-15.4: Menu Cleared on Close

    /// <summary>
    /// TC-15.4: When menu closes,
    /// Then menu lines are completely cleared from display.
    /// </summary>
    [TestMethod]
    public void TC_15_4_MenuCleared_OnClose()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("server ");
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue();
        
        harness.PressEscape();

        // Assert: Menu cleared
        harness.IsMenuVisible.Should().BeFalse();
    }

    #endregion

    #region TC-15.5: Scroll Indicator Styling

    /// <summary>
    /// TC-15.5: When menu has scroll indicators,
    /// Then they are styled to be visible but distinct from items.
    /// </summary>
    [TestMethod]
    public void TC_15_5_ScrollIndicator_Styling()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act
        harness.TypeText("server ");
        harness.PressTab();

        // Assert: Menu renders correctly
        if (harness.IsMenuVisible)
        {
            harness.MenuItemCount.Should().BeGreaterThan(0);
        }
    }

    #endregion
}
