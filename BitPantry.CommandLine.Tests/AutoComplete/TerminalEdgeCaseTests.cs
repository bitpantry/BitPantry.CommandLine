using System;
using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Terminal & Environment Edge Cases Tests (TC-22.1 through TC-22.6)
/// Tests autocomplete behavior under various terminal conditions.
/// </summary>
[TestClass]
public class TerminalEdgeCaseTests
{
    #region TC-22.1: Terminal Resize During Menu

    /// <summary>
    /// TC-22.1: When terminal is resized while menu is open,
    /// Then menu handles gracefully (no crash/corruption).
    /// Note: VirtualConsole has fixed dimensions; test validates stability.
    /// </summary>
    [TestMethod]
    public void TC_22_1_TerminalResize_DuringMenu()
    {
        // Arrange: Standard harness
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Open menu
        harness.TypeText("server ");
        harness.PressTab();
        
        harness.IsMenuVisible.Should().BeTrue("menu should be open");
        
        // Simulate resize by continuing to interact
        // In VirtualConsole, size is fixed but this tests stability
        harness.Keyboard.PressKey(ConsoleKey.DownArrow);
        harness.Keyboard.PressKey(ConsoleKey.UpArrow);
        
        // Assert: No crash, menu still functional
        harness.IsMenuVisible.Should().BeTrue("menu should remain open");
    }

    #endregion

    #region TC-22.2: Very Narrow Terminal

    /// <summary>
    /// TC-22.2: When terminal is very narrow (40 columns),
    /// Then menu renders without breaking.
    /// </summary>
    [TestMethod]
    public void TC_22_2_NarrowTerminal_MenuRenders()
    {
        // Arrange: Create harness with narrow terminal (40 columns)
        using var harness = new AutoCompleteTestHarness(
            width: 40,
            height: 24,
            configureApp: builder => builder.RegisterCommand<ServerCommand>());

        // Act: Open menu
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Menu renders (may be truncated but functional)
        harness.IsMenuVisible.Should().BeTrue("menu should appear even in narrow terminal");
    }

    #endregion

    #region TC-22.3: Standard Terminal Width

    /// <summary>
    /// TC-22.3: When terminal is standard 80 columns,
    /// Then all content is readable.
    /// </summary>
    [TestMethod]
    public void TC_22_3_StandardTerminal_ContentReadable()
    {
        // Arrange: Standard 80-column terminal (default)
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Open menu
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Menu visible and navigable
        harness.IsMenuVisible.Should().BeTrue("menu should be visible");
        harness.MenuItemCount.Should().BeGreaterThan(0, "should have items");
    }

    #endregion

    #region TC-22.4: Tab During Command Execution

    /// <summary>
    /// TC-22.4: When command is running and Tab is pressed,
    /// Then autocomplete works or is gracefully ignored.
    /// Note: In test harness, we're not actually executing commands.
    /// </summary>
    [TestMethod]
    public void TC_22_4_TabDuringExecution_Graceful()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type command and Tab
        harness.TypeText("server ");
        harness.PressTab();
        
        // Assert: Autocomplete works normally
        harness.IsMenuVisible.Should().BeTrue("autocomplete should work");
    }

    #endregion

    #region TC-22.5: Non-ASCII Characters in Path

    /// <summary>
    /// TC-22.5: When path contains non-ASCII characters,
    /// Then completion handles correctly.
    /// Note: KeyboardSimulator only supports ASCII characters (0-255).
    /// </summary>
    [TestMethod]
    [Ignore("KeyboardSimulator does not support Unicode characters outside ASCII range")]
    public void TC_22_5_NonAsciiPath_Handled()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PathArgTestCommand>();

        // Act: Type path with Unicode characters
        harness.TypeText("patharg /home/用户/Documents/");
        
        // Assert: Buffer contains the Unicode path
        harness.Buffer.Should().Contain("用户", "Unicode characters should be preserved");
        
        // Tab for completion
        harness.PressTab();
        // Should not crash
    }

    #endregion

    #region TC-22.6: Very Long Path

    /// <summary>
    /// TC-22.6: When path is very long (near system limits),
    /// Then completion handles gracefully.
    /// </summary>
    [TestMethod]
    public void TC_22_6_VeryLongPath_Handled()
    {
        // Arrange
        using var harness = AutoCompleteTestHarness.WithCommand<PathArgTestCommand>();

        // Act: Type a long path
        var longPath = string.Join("/", Enumerable.Repeat("verylongdirectoryname", 10));
        harness.TypeText($"patharg {longPath}/");
        
        // Assert: Buffer contains the path
        harness.Buffer.Should().Contain("verylongdirectoryname", "long path should be preserved");
        
        // Tab should not crash
        harness.PressTab();
    }

    #endregion
}
