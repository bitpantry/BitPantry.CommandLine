using System.Linq;
using BitPantry.CommandLine.API;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BitPantry.CommandLine.Tests.AutoComplete;

/// <summary>
/// Multi-Step Workflow Tests (TC-12.1 through TC-12.4)
/// Tests complete user interaction workflows.
/// </summary>
[TestClass]
public class WorkflowTests
{
    #region TC-12.1: Complete Type-Tab-Navigate-Enter Flow

    /// <summary>
    /// TC-12.1: When user completes a full interaction workflow,
    /// Then each step behaves correctly and final command is correct.
    /// </summary>
    [TestMethod]
    public void TC_12_1_CompleteTypeTabNavigateEnterFlow()
    {
        // Arrange: Multiple commands starting with 's' so we get a menu
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Type "s", Tab to open menu
        harness.TypeText("s");
        harness.PressTab();
        
        // Step 2: Menu opens
        harness.IsMenuVisible.Should().BeTrue("Tab should open menu for multiple matches");
        var firstItem = harness.SelectedItem;
        harness.SelectedIndex.Should().Be(0, "first item should be selected");
        
        // Step 3: Navigate down
        harness.PressDownArrow();
        harness.SelectedIndex.Should().Be(1, "Down Arrow should select second item");
        var secondItem = harness.SelectedItem;
        
        // Step 4: Accept
        harness.PressEnter();
        
        // Assert: Menu closed, buffer contains second item, cursor at end
        harness.IsMenuVisible.Should().BeFalse("Enter should close menu");
        harness.Buffer.Should().Be(secondItem, "buffer should contain the accepted item");
        harness.BufferPosition.Should().Be(harness.Buffer.Length, "cursor should be at end of buffer");
    }

    #endregion

    #region TC-12.2: Chain of Completions

    /// <summary>
    /// TC-12.2: When completing multiple levels in sequence,
    /// Then each level properly transitions to the next.
    /// </summary>
    [TestMethod]
    public void TC_12_2_ChainOfCompletions()
    {
        // Arrange: One command so "serv" auto-completes to "server"
        using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

        // Act: Type "serv", Tab - should auto-complete to "server" (single match)
        harness.TypeText("serv");
        harness.PressTab();
        
        // Single match should auto-complete without menu
        harness.IsMenuVisible.Should().BeFalse("single match should auto-complete without menu");
        harness.Buffer.Should().Be("server", "should complete to 'server'");
        
        // Type space, Tab - should show arguments
        harness.TypeText(" ");
        harness.PressTab();
        
        // ServerCommand has --Host and --Port arguments
        harness.IsMenuVisible.Should().BeTrue("should show argument menu");
        harness.MenuItemCount.Should().BeGreaterThanOrEqualTo(1, "should have at least one argument");
        
        // Accept first argument
        var argName = harness.SelectedItem;
        harness.PressEnter();
        
        // Assert: Buffer should have command + argument
        harness.IsMenuVisible.Should().BeFalse("menu should close after accepting");
        harness.Buffer.Should().StartWith("server ", "buffer should start with 'server '");
        harness.Buffer.Should().Contain(argName, "buffer should contain the accepted argument");
    }

    #endregion

    #region TC-12.3: Cache Invalidation After Completion

    /// <summary>
    /// TC-12.3: When an argument is completed and used,
    /// Then subsequent completions properly exclude it.
    /// </summary>
    [TestMethod]
    public void TC_12_3_CacheInvalidation_AfterCompletion()
    {
        // Arrange: Use MultiArgTestCommand which has multiple arguments
        using var harness = AutoCompleteTestHarness.WithCommand<MultiArgTestCommand>();

        // Act: Type command and space, Tab to get argument menu
        harness.TypeText("multicmd ");
        harness.PressTab();
        
        // Should have menu with arguments
        harness.IsMenuVisible.Should().BeTrue("should show argument menu");
        var initialCount = harness.MenuItemCount;
        initialCount.Should().BeGreaterThanOrEqualTo(2, "MultiArgTestCommand should have multiple arguments");
        
        // Accept first argument
        var firstArg = harness.SelectedItem;
        harness.PressEnter();
        
        // Type a value and space
        harness.TypeText("value ");
        
        // Tab again to get remaining arguments
        harness.PressTab();
        
        // Assert: Menu should not contain the used argument
        harness.IsMenuVisible.Should().BeTrue("should show remaining arguments");
        harness.MenuItems!.Select(m => m.InsertText).Should().NotContain(firstArg, "used argument should be excluded from menu");
    }

    #endregion

    #region TC-12.4: Tab-Escape-Tab Reopens Menu

    /// <summary>
    /// TC-12.4: When menu is closed with Escape and Tab pressed again,
    /// Then menu reopens.
    /// </summary>
    [TestMethod]
    public void TC_12_4_TabEscapeTab_ReopensMenu()
    {
        // Arrange: Multiple commands for menu
        using var harness = AutoCompleteTestHarness.WithCommands(
            typeof(ServerCommand),
            typeof(ServiceCommand),
            typeof(SetupCommand));

        // Act: Type "s", Tab to open menu
        harness.TypeText("s");
        harness.PressTab();
        harness.IsMenuVisible.Should().BeTrue("Tab should open menu");
        
        // Escape closes menu
        harness.PressEscape();
        harness.IsMenuVisible.Should().BeFalse("Escape should close menu");
        
        // Tab again reopens menu
        harness.PressTab();

        // Assert: Menu reopened
        harness.IsMenuVisible.Should().BeTrue("Tab should reopen menu");
        harness.MenuItemCount.Should().BeGreaterThan(1, "menu should have same items");
    }

    #endregion
}
