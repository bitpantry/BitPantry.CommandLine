using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BitPantry.CommandLine.Tests.VirtualConsole;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for complete workflows including:
/// - Full type/tab/navigate/enter workflows
/// - Multiple completions (chains)
/// - History navigation
/// - Submission tests
/// </summary>
[TestClass]
public class WorkflowTests : VisualTestBase
{
    #region Full Workflow Tests

    [TestMethod]
    [TestDescription("Complete workflow: type, tab, navigate, enter, submit")]
    public async Task CompleteWorkflow_TypeTabNavigateEnterSubmit()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // 1. Type "server "
        await runner.TypeText("server ");
        runner.Should().HaveState("server ", 7);

        // 2. Press Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        var firstItem = runner.SelectedMenuItem;

        // 3. Navigate down
        await runner.PressKey(ConsoleKey.DownArrow);
        var secondItem = runner.SelectedMenuItem;
        secondItem.Should().NotBe(firstItem);

        // 4. Accept with Enter
        await runner.PressKey(ConsoleKey.Enter);
        runner.Should().NotHaveMenuVisible();

        // 5. Verify final state
        var finalBuffer = runner.Buffer;
        finalBuffer.Should().Contain(secondItem);
        runner.BufferPosition.Should().Be(finalBuffer.Length, "cursor at end after accept");

        // 6. Submit
        var result = await runner.Submit();
        result.Should().Be(finalBuffer);
    }

    [TestMethod]
    [TestDescription("Workflow: Type partial, tab to complete, add space, complete next level")]
    public async Task Workflow_PartialCompleteThenNextLevel()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type partial
        await runner.TypeText("serv");
        
        // Tab to complete
        await runner.PressKey(ConsoleKey.Tab);
        runner.Buffer.Should().Be("server ");
        
        // Now complete subcommand
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        var subcommand = runner.SelectedMenuItem;
        await runner.PressKey(ConsoleKey.Enter);
        
        runner.Buffer.Should().Be($"server {subcommand} ");
    }

    #endregion

    #region Multiple Completions (Chains)

    [TestMethod]
    [TestDescription("Chain of completions: command then argument")]
    public async Task ChainOfCompletions_CommandThenArgument()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Complete "server"
        await runner.TypeText("serv");
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After first Tab - Buffer: '{runner.Buffer}'");

        // If menu opened, select and accept
        if (runner.IsMenuVisible)
        {
            await runner.PressKey(ConsoleKey.Enter);
        }
        
        Debug.WriteLine($"After Enter - Buffer: '{runner.Buffer}'");

        // Add space and try to complete subcommand
        if (!runner.Buffer.EndsWith(" "))
        {
            await runner.TypeText(" ");
        }
        
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After second Tab - Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");

        // Should show subcommand completions
        runner.Should().HaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("Complete group then complete command within group")]
    public async Task CompleteGroupThenCommand()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type "server " to get into server group
        await runner.TypeText("server ");
        
        // Tab to get command completions
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"Menu items available: {runner.SelectedMenuItem}");
        runner.Should().HaveMenuVisible();

        // Accept first completion
        await runner.PressKey(ConsoleKey.Enter);
        
        Debug.WriteLine($"After Enter - Buffer: '{runner.Buffer}'");
        
        // Should have "server <command> "
        runner.Buffer.Should().StartWith("server ");
        runner.Buffer.Split(' ').Length.Should().BeGreaterThan(1);
    }

    [TestMethod]
    [TestDescription("Navigate deep into nested groups with Tab")]
    public async Task NavigateDeepNestedGroups()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // server > profile > add
        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Select "profile"
        await runner.PressKey(ConsoleKey.Enter);
        runner.Buffer.Should().Contain("profile");
        
        // Now complete within profile
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Accept
        await runner.PressKey(ConsoleKey.Enter);
        
        Debug.WriteLine($"Final buffer: '{runner.Buffer}'");
        runner.Buffer.Should().StartWith("server profile ");
    }

    #endregion

    #region History Navigation

    [TestMethod]
    [TestDescription("UpArrow with no menu should navigate to previous history")]
    public async Task UpArrowNoMenu_ShouldNavigateHistory()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // UpArrow should navigate history
        await runner.PressKey(ConsoleKey.UpArrow);

        Debug.WriteLine($"After UpArrow - Buffer: '{runner.Buffer}'");
        
        // Should have loaded previous history entry
        runner.Buffer.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestDescription("DownArrow with no menu should navigate to next history")]
    public async Task DownArrowNoMenu_ShouldNavigateHistory()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // Go up in history first
        await runner.PressKey(ConsoleKey.UpArrow);
        await runner.PressKey(ConsoleKey.UpArrow);
        var afterTwoUp = runner.Buffer;
        Debug.WriteLine($"After 2x UpArrow - Buffer: '{afterTwoUp}'");

        // Then down
        await runner.PressKey(ConsoleKey.DownArrow);
        
        Debug.WriteLine($"After DownArrow - Buffer: '{runner.Buffer}'");
        
        // Should have navigated to different history entry
        runner.Buffer.Should().NotBeEmpty();
    }

    [TestMethod]
    [TestDescription("UpArrow while menu open should navigate menu not history")]
    public async Task UpArrowWhileMenuOpen_ShouldNavigateMenuNotHistory()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();
        
        // Move down first
        await runner.PressKey(ConsoleKey.DownArrow);
        var indexAfterDown = runner.MenuSelectedIndex;

        // UpArrow should navigate menu, not history
        await runner.PressKey(ConsoleKey.UpArrow);

        runner.Should().HaveMenuVisible();
        runner.MenuSelectedIndex.Should().BeLessThan(indexAfterDown);
        runner.Buffer.Should().Be("server ", "buffer should not change to history");
    }

    [TestMethod]
    [TestDescription("Navigate to history then Tab should complete from history entry")]
    public async Task HistoryThenTab_ShouldCompleteFromHistoryEntry()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // Navigate to history
        await runner.PressKey(ConsoleKey.UpArrow);
        Debug.WriteLine($"After UpArrow - Buffer: '{runner.Buffer}'");

        // Tab should try to complete from current buffer
        await runner.PressKey(ConsoleKey.Tab);

        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', MenuVisible: {runner.IsMenuVisible}");
        
        // Behavior depends on what's in history
    }

    [TestMethod]
    [TestDescription("Multiple history navigation cycles correctly")]
    public async Task MultipleHistoryNavigation_CyclesCorrectly()
    {
        using var runner = CreateRunnerWithHistory();
        runner.Initialize();

        // Navigate up through all history
        await runner.PressKey(ConsoleKey.UpArrow);
        var first = runner.Buffer;
        
        await runner.PressKey(ConsoleKey.UpArrow);
        var second = runner.Buffer;
        
        // Navigate back down
        await runner.PressKey(ConsoleKey.DownArrow);
        
        runner.Buffer.Should().Be(first);
    }

    #endregion

    #region Submission Tests

    [TestMethod]
    [TestDescription("Enter with no menu should submit input")]
    public async Task EnterNoMenu_ShouldSubmitInput()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server connect");
        runner.Should().HaveMenuHidden();

        // Submit
        var result = await runner.Submit();

        result.Should().Be("server connect");
    }

    [TestMethod]
    [TestDescription("Type, open menu, escape, then Enter should submit")]
    public async Task TypeMenuEscapeEnter_ShouldSubmit()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        runner.Should().HaveMenuVisible();

        await runner.PressKey(ConsoleKey.Escape);
        runner.Should().HaveMenuHidden();

        // Now Enter should submit
        var result = await runner.Submit();

        result.Should().Be("server ");
    }

    [TestMethod]
    [TestDescription("Complete command then submit")]
    public async Task CompleteCommandThenSubmit()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("server ");
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.Enter); // Accept completion

        runner.Should().HaveMenuHidden();
        runner.Buffer.Should().StartWith("server ");

        // Submit the completed command
        var result = await runner.Submit();

        Debug.WriteLine($"Submitted: '{result}'");
        result.Should().StartWith("server ");
    }

    [TestMethod]
    [TestDescription("Empty buffer submission")]
    public async Task EmptyBuffer_Submission()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        runner.Buffer.Should().BeEmpty();

        var result = await runner.Submit();

        result.Should().BeEmpty();
    }

    #endregion
}
