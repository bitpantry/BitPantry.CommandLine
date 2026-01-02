using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for argument completion including:
/// - Ghost text for argument names (--) and aliases (-)
/// - Boolean/flag argument handling (no value expected)
/// - Value completion suppression for flags
/// - Used argument exclusion
/// 
/// These tests use a VersionCommand-like structure with boolean flags
/// to reproduce real-world scenarios.
/// </summary>
[TestClass]
public class ArgumentCompletionTests
{
    #region Test Commands

    /// <summary>
    /// Version command with a boolean flag (-f/--Full) matching the real version command.
    /// </summary>
    [Command(Name = "version")]
    [CmdDescription("Display version information")]
    public class TestVersionCommand : CommandBase
    {
        [Argument]
        [Alias('f')]
        [CmdDescription("Include full details")]
        public Option Full { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with multiple argument types for comprehensive testing.
    /// </summary>
    [Command(Name = "deploy")]
    [CmdDescription("Deploy application")]
    public class TestDeployCommand : CommandBase
    {
        [Argument(Name = "target")]
        [Alias('t')]
        [CmdDescription("Deployment target")]
        public string Target { get; set; }

        [Argument(Name = "force")]
        [Alias('F')]
        [CmdDescription("Force deployment")]
        public Option Force { get; set; }

        [Argument(Name = "verbose")]
        [Alias('v')]
        [CmdDescription("Verbose output")]
        public Option Verbose { get; set; }

        [Argument(Name = "port")]
        [Alias('p')]
        [CmdDescription("Port number")]
        public int Port { get; set; } = 8080;

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with no arguments for edge case testing.
    /// </summary>
    [Command(Name = "ping")]
    [CmdDescription("Ping server")]
    public class TestPingCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Test Infrastructure

    private class SimplePrompt : IPrompt
    {
        public string Render() => "> ";
        public int GetPromptLength() => 2;
        public void Write(IAnsiConsole console) => console.Write(new Text("> "));
    }

    private CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterCommand<TestVersionCommand>();
        registry.RegisterCommand<TestDeployCommand>();
        registry.RegisterCommand<TestPingCommand>();
        return registry;
    }

    private StepwiseTestRunner CreateRunner()
    {
        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var fileSystem = new System.IO.Abstractions.FileSystem();
        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry),
            new FilePathProvider(fileSystem)  // Include to test directory completion bug
        };
        var orchestrator = new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    #endregion

    #region BUG REPRO: Ghost Text Shows Full Prefix Instead of Remainder

    [TestMethod]
    [TestDescription("BUG REPRO: Ghost after single dash should show 'f' not '-f'")]
    public async Task Ghost_AfterSingleDash_ShouldShowAliasOnly()
    {
        // ARRANGE: Set up with version command and single dash
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version ");
        runner.Buffer.Should().Be("version ");
        runner.BufferPosition.Should().Be(8);
        
        // ACT: Type single dash
        await runner.TypeText("-");

        // ASSERT: Ghost should show just the alias letter 'f', NOT '-f'
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");
        Debug.WriteLine($"DisplayedLine: '{runner.DisplayedLine}'");

        runner.Buffer.Should().Be("version -");
        
        // THIS IS THE BUG: Ghost currently shows "-f" but should show just "f"
        runner.GhostText.Should().Be("f", 
            "ghost should show only the remainder 'f', not the full '-f'");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Ghost after double dash should show 'Full' not '--Full'")]
    public async Task Ghost_AfterDoubleDash_ShouldShowNameOnly()
    {
        // ARRANGE: Set up with version command and double dash
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version ");
        runner.Buffer.Should().Be("version ");
        runner.BufferPosition.Should().Be(8);
        
        // ACT: Type double dash
        await runner.TypeText("--");

        // ASSERT: Ghost should show just 'Full', NOT '--Full'
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");
        Debug.WriteLine($"DisplayedLine: '{runner.DisplayedLine}'");

        runner.Buffer.Should().Be("version --");
        
        // THIS IS THE BUG: Ghost currently shows "--Full" but should show just "Full"
        runner.GhostText.Should().Be("Full", 
            "ghost should show only the remainder 'Full', not the full '--Full'");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Tab after boolean flag should NOT show directory completion")]
    public async Task Tab_AfterBooleanFlag_ShouldNotShowDirectoryCompletion()
    {
        // ARRANGE: Type version command with --Full flag completed
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --Full ");
        runner.Buffer.Should().Be("version --Full ");
        runner.BufferPosition.Should().Be(15);
        
        // Verify starting state - no menu visible
        runner.Should().HaveMenuHidden();
        
        // ACT: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT: Should NOT show directory completion menu
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"After Tab - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"After Tab - SelectedMenuItem: '{runner.SelectedMenuItem}'");

        // BUG: Menu currently shows directory paths like "bin\", "obj\" 
        // Expected: No menu (--Full is a boolean flag, no value expected)
        runner.Should().HaveMenuHidden(
            "boolean flag --Full takes no value, so Tab should not show any completion menu");
        
        // Buffer should remain unchanged - no directory path inserted
        runner.Buffer.Should().Be("version --Full ",
            "buffer should not change since --Full is a flag with no value");
    }

    #endregion

    #region Ghost Text - Partial Argument Name

    [TestMethod]
    [TestDescription("Ghost after partial argument name should show remainder")]
    public async Task Ghost_AfterPartialArgName_ShouldShowRemainder()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --F");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("version --F");
        runner.GhostText.Should().Be("ull", 
            "ghost should show 'ull' to complete '--Full'");
    }

    [TestMethod]
    [TestDescription("Ghost clears when exact argument match")]
    public async Task Ghost_ExactArgMatch_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --Full");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("version --Full");
        runner.GhostText.Should().BeNullOrEmpty(
            "no ghost when argument name is complete");
    }

    [TestMethod]
    [TestDescription("Ghost after partial alias should be empty (single char aliases)")]
    public async Task Ghost_AfterExactAlias_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version -f");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("version -f");
        runner.GhostText.Should().BeNullOrEmpty(
            "no ghost when alias is complete");
    }

    #endregion

    #region Ghost Text Updates While Typing

    [TestMethod]
    [TestDescription("Typing matching character shrinks argument ghost")]
    public async Task Ghost_TypeMatchingChar_ShrinksGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --");
        runner.GhostText.Should().Be("Full");

        // Type 'F' - should shrink ghost
        await runner.TypeText("F");
        
        runner.Buffer.Should().Be("version --F");
        runner.GhostText.Should().Be("ull");
    }

    [TestMethod]
    [TestDescription("Typing non-matching character clears ghost")]
    public async Task Ghost_TypeNonMatchingChar_ClearsGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --");
        runner.GhostText.Should().Be("Full");

        // Type 'x' - doesn't match any argument
        await runner.TypeText("x");
        
        runner.Buffer.Should().Be("version --x");
        runner.GhostText.Should().BeNullOrEmpty(
            "ghost should clear when no argument matches");
    }

    [TestMethod]
    [TestDescription("Backspace to -- should restore argument ghost")]
    public async Task Ghost_BackspaceToDoubleDash_RestoresGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("version --F");
        runner.GhostText.Should().Be("ull");

        // Backspace to remove 'F'
        await runner.PressKey(ConsoleKey.Backspace);
        
        runner.Buffer.Should().Be("version --");
        runner.GhostText.Should().Be("Full",
            "ghost should restore to full argument name after backspace");
    }

    #endregion

    #region Command With No Arguments

    [TestMethod]
    [TestDescription("No ghost for command with no arguments")]
    public async Task Ghost_CommandWithNoArgs_NoGhost()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("ping --");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("ping --");
        runner.GhostText.Should().BeNullOrEmpty(
            "no ghost when command has no arguments");
    }

    [TestMethod]
    [TestDescription("Tab after command with no args should do nothing")]
    public async Task Tab_CommandWithNoArgs_NoCompletion()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("ping ");
        await runner.PressKey(ConsoleKey.Tab);
        
        // Should not show argument completions
        if (runner.IsMenuVisible)
        {
            runner.SelectedMenuItem.Should().NotStartWith("--",
                "should not offer argument completions for command with no args");
        }
    }

    #endregion

    #region Multiple Arguments - Used Argument Exclusion

    [TestMethod]
    [TestDescription("Ghost excludes already-used argument name")]
    public async Task Ghost_ExcludesUsedArgument()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Use --force flag, then type -- for more args
        await runner.TypeText("deploy --force --");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("deploy --force --");
        
        // Ghost should NOT show 'force' again - should show next available arg
        runner.GhostText.Should().NotBe("force",
            "ghost should not suggest already-used argument");
    }

    [TestMethod]
    [TestDescription("Ghost excludes argument when alias was used")]
    public async Task Ghost_ExcludesArgWhenAliasUsed()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Use -F alias, then type -- for argument names
        await runner.TypeText("deploy -F --");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("deploy -F --");
        
        // Ghost should NOT show 'force' since -F (its alias) was already used
        runner.GhostText.Should().NotBe("force",
            "ghost should not suggest argument when its alias was already used");
    }

    [TestMethod]
    [TestDescription("BUG REPRO: Menu should exclude already-used argument name")]
    public async Task Menu_ExcludesUsedArgument()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Type command with --force already used, then space for next arg
        await runner.TypeText("deploy --force ");
        runner.Buffer.Should().Be("deploy --force ");
        
        // ACT: press Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        
        // ASSERT: Menu should be visible and should NOT contain --force
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"MenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"MenuItems: {string.Join(", ", menuItemTexts)}");
        }
        
        runner.Should().HaveMenuVisible("Tab should open menu for remaining arguments");
        
        // Get all menu items
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        // --force should NOT be in the menu since it's already used
        menuItems.Should().NotContain("--force",
            "menu should NOT show already-used argument --force");
        
        // Other arguments should still be available
        menuItems.Should().Contain("--target",
            "menu should show --target which is not yet used");
    }

    [TestMethod]
    [TestDescription("Menu should exclude argument when its alias was used")]
    public async Task Menu_ExcludesArgWhenAliasUsed()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Use -F alias (for force), then space for next arg
        await runner.TypeText("deploy -F ");
        runner.Buffer.Should().Be("deploy -F ");
        
        // ACT: press Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        
        // ASSERT: Menu should NOT contain --force since its alias -F was used
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"MenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"MenuItems: {string.Join(", ", menuItemTexts)}");
        }
        
        runner.Should().HaveMenuVisible("Tab should open menu for remaining arguments");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        menuItems.Should().NotContain("--force",
            "menu should NOT show --force when alias -F was already used");
    }

    [TestMethod]
    [TestDescription("Menu should exclude alias when its full name was used")]
    public async Task Menu_ExcludesAliasWhenNameUsed()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Use --force (full name), then type single dash for aliases
        await runner.TypeText("deploy --force -");
        runner.Buffer.Should().Be("deploy --force -");
        
        // ACT: press Tab to open menu for aliases
        await runner.PressKey(ConsoleKey.Tab);
        
        // ASSERT: Menu should NOT contain -F since --force was used
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"MenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"MenuItems: {string.Join(", ", menuItemTexts)}");
        }
        
        runner.Should().HaveMenuVisible("Tab should open menu for remaining aliases");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        menuItems.Should().NotContain("-F",
            "menu should NOT show alias -F when --force was already used");
        
        // Other aliases should still be available
        menuItems.Should().Contain("-t",
            "menu should show -t (target) which is not yet used");
    }

    [TestMethod]
    [TestDescription("Menu should exclude multiple used arguments")]
    public async Task Menu_ExcludesMultipleUsedArgs()
    {
        // ARRANGE
        using var runner = CreateRunner();
        runner.Initialize();

        // Use two arguments: --force and --target
        await runner.TypeText("deploy --force --target prod ");
        runner.Buffer.Should().Be("deploy --force --target prod ");
        
        // ACT: press Tab to open menu
        await runner.PressKey(ConsoleKey.Tab);
        
        // ASSERT: Menu should NOT contain --force or --target
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"MenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            var menuItemTexts = runner.Controller.MenuItems.Select(i => i.InsertText).ToList();
            Debug.WriteLine($"MenuItems: {string.Join(", ", menuItemTexts)}");
        }
        
        runner.Should().HaveMenuVisible("Tab should open menu for remaining arguments");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.InsertText).ToList() ?? new List<string>();
        
        menuItems.Should().NotContain("--force",
            "menu should NOT show --force (already used)");
        menuItems.Should().NotContain("--target",
            "menu should NOT show --target (already used)");
        
        // Remaining args should still be available
        menuItems.Should().Contain("--verbose",
            "menu should show --verbose which is not yet used");
        menuItems.Should().Contain("--port",
            "menu should show --port which is not yet used");
    }

    #endregion

    #region Boolean Flag - No Value Completion

    [TestMethod]
    [TestDescription("Ghost after completed flag shows next argument, not value")]
    public async Task Ghost_AfterFlag_ShowsNextArg()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type deploy with --force flag and space
        await runner.TypeText("deploy --force ");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("deploy --force ");
        
        // Ghost should show next available argument (not a value for --force)
        if (!string.IsNullOrEmpty(runner.GhostText))
        {
            // If there's a ghost, it should be an argument indicator (-- or -)
            // or the next argument name, NOT a random value
            (runner.GhostText.StartsWith("--") || 
             runner.GhostText.StartsWith("-") ||
             runner.GhostText == "target" || // might suggest next arg without --
             runner.GhostText == "verbose" ||
             runner.GhostText == "port").Should().BeTrue(
                $"ghost '{runner.GhostText}' should indicate next argument, not a value");
        }
    }

    [TestMethod]
    [TestDescription("Tab after flag space shows remaining arguments")]
    public async Task Tab_AfterFlagSpace_ShowsRemainingArgs()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        await runner.TypeText("deploy --force ");
        await runner.PressKey(ConsoleKey.Tab);
        
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"After Tab - MenuVisible: {runner.IsMenuVisible}");
        Debug.WriteLine($"After Tab - SelectedMenuItem: '{runner.SelectedMenuItem}'");

        // Should show remaining arguments, not directory completion
        if (runner.IsMenuVisible)
        {
            runner.SelectedMenuItem.Should().NotContain("\\");
            runner.SelectedMenuItem.Should().NotContain("/");
        }
    }

    #endregion

    #region Case Sensitivity

    [TestMethod]
    [TestDescription("Ghost for argument is case-insensitive")]
    public async Task Ghost_CaseInsensitive_Matching()
    {
        using var runner = CreateRunner();
        runner.Initialize();

        // Type lowercase 'f' - should still match 'Full'
        await runner.TypeText("version --f");
        
        Debug.WriteLine($"Buffer: '{runner.Buffer}'");
        Debug.WriteLine($"GhostText: '{runner.GhostText}'");

        runner.Buffer.Should().Be("version --f");
        // Ghost should show remainder with proper casing
        runner.GhostText.Should().BeOneOf("ull", "ULL", "Ull",
            "ghost should match case-insensitively");
    }

    #endregion
}
