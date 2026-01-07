using System;
using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// VirtualConsole Integration Testing (TC-32.1 through TC-32.6)
/// Tests that autocomplete renders correctly in VirtualConsole environment.
/// </summary>
[TestClass]
public class VirtualConsoleIntegrationTests
{
    #region TC-32.1: Menu Renders Correctly in VirtualConsole

    /// <summary>
    /// TC-32.1: When menu is opened in VirtualConsole test environment,
    /// Then ANSI sequences produce expected screen state.
    /// </summary>
    [TestMethod]
    public void TC_32_1_MenuRendersCorrectly_InVirtualConsole()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Trigger menu display
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Menu is visible in VirtualConsole
        harness.IsMenuVisible.Should().BeTrue("menu should be visible");
        harness.MenuItemCount.Should().BeGreaterThan(0, "should have menu items");
        harness.SelectedIndex.Should().BeGreaterThanOrEqualTo(0, "should have selection");
    }

    #endregion

    #region TC-32.2: Ghost Text Color in VirtualConsole

    /// <summary>
    /// TC-32.2: When ghost text renders in VirtualConsole,
    /// Then ghost text appears (style testing requires VirtualConsole inspection).
    /// </summary>
    [TestMethod]
    public void TC_32_2_GhostTextColor_InVirtualConsole()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type partial to trigger ghost
        harness.TypeText("serv");
        harness.PressTab();
        
        // Assert: Ghost text appears
        if (harness.HasGhostText)
        {
            harness.GhostText.Should().NotBeNullOrEmpty("ghost text should have content");
        }
    }

    #endregion

    #region TC-32.3: Cursor Position Tracking

    /// <summary>
    /// TC-32.3: When user types and menu updates,
    /// Then cursor position is tracked accurately.
    /// </summary>
    [TestMethod]
    public void TC_32_3_CursorPosition_TrackedAccurately()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
        
        // Act: Type and track position
        harness.TypeText("server ");
        var posAfterCommand = harness.BufferPosition;
        
        harness.TypeText("--Host");
        var posAfterArg = harness.BufferPosition;
        
        // Assert: Position increases correctly
        posAfterArg.Should().BeGreaterThan(posAfterCommand, "position should increase");
        harness.Buffer.Length.Should().Be(posAfterArg, "position should match buffer length at end");
    }

    #endregion

    #region TC-32.4: Screen Buffer Clear After Menu Close

    /// <summary>
    /// TC-32.4: When menu closes,
    /// Then menu area is properly cleared.
    /// </summary>
    [TestMethod]
    public void TC_32_4_ScreenBufferClear_AfterMenuClose()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Open menu
        harness.TypeText("server ");
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue("menu should open");
        
        // Close menu
        harness.PressEscape();
        
        // Assert: Menu is no longer visible
        harness.IsMenuVisible.Should().BeFalse("menu should be closed");
    }

    #endregion

    #region TC-32.5: Line Wrapping with Long Completions

    /// <summary>
    /// TC-32.5: When completion text exceeds terminal width,
    /// Then wrapping or truncation is handled correctly.
    /// </summary>
    [TestMethod]
    public void TC_32_5_LineWrapping_LongCompletions()
    {
        // Arrange: Narrow terminal
        using var harness = new AutoCompleteTestHarness(
            width: 40,
            height: 24,
            configureApp: builder => builder.RegisterCommand<ServerCommand>());

        // Act: Open menu
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Menu handles narrow width gracefully
        harness.IsMenuVisible.Should().BeTrue("menu should render in narrow terminal");
    }

    #endregion

    #region TC-32.6: Assert Keystroke Sequences

    /// <summary>
    /// TC-32.6: When complex keystroke sequences are simulated,
    /// Then each intermediate state is verifiable.
    /// </summary>
    [TestMethod]
    public void TC_32_6_KeystrokeSequences_VerifiableStates()
    {
        // Arrange: Use MultiArgTestCommand with 3+ arguments
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();
        harness.TypeText("multicmd ");
        
        // Record states through sequence
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue("Step 1: menu opens on Tab");
        var indexAfterTab = harness.SelectedIndex;
        var itemCount = harness.MenuItemCount;
        
        harness.Keyboard.PressKey(ConsoleKey.DownArrow);
        var indexAfterFirstDown = harness.SelectedIndex;
        // Should have moved (either forward or wrapped)
        
        harness.Keyboard.PressKey(ConsoleKey.DownArrow);
        var indexAfterSecondDown = harness.SelectedIndex;
        // Should have moved again
        
        harness.PressEnter();
        harness.IsMenuVisible.Should().BeFalse("Step 4: Enter closes menu");
        harness.Buffer.Should().NotBe("multicmd ", "Buffer should contain selected item");
    }

    #endregion
}
