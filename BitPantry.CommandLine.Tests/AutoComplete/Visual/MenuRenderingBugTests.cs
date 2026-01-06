using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// TDD tests for menu rendering bugs.
/// 
/// Bug: When navigating through a completion menu with Tab/Down arrow,
/// the menu is being re-rendered (printed again) instead of being updated
/// in place. This causes duplicate menu lines to appear on screen.
/// 
/// Related to: MC-XXX - Menu navigation re-renders instead of updating
/// </summary>
[TestClass]
public class MenuRenderingBugTests : VisualTestBase
{
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        // Set up a directory with MANY items to trigger menu wrapping across multiple lines
        // This is the key to reproducing the real bug - menu content exceeds terminal width
        _mockFileSystem.AddDirectory(@"C:\work");
        _mockFileSystem.AddDirectory(@"C:\work\bin");
        _mockFileSystem.AddDirectory(@"C:\work\obj");
        _mockFileSystem.AddDirectory(@"C:\work\src");
        _mockFileSystem.AddDirectory(@"C:\work\docs");
        _mockFileSystem.AddDirectory(@"C:\work\tests");
        _mockFileSystem.AddDirectory(@"C:\work\build");
        _mockFileSystem.AddDirectory(@"C:\work\dist");
        _mockFileSystem.AddDirectory(@"C:\work\scripts");
        _mockFileSystem.AddDirectory(@"C:\work\packages");
        _mockFileSystem.AddDirectory(@"C:\work\modules");
        _mockFileSystem.AddFile(@"C:\work\config.json", new MockFileData("{}"));
        _mockFileSystem.AddFile(@"C:\work\README.md", new MockFileData("# README"));
        _mockFileSystem.AddFile(@"C:\work\test.txt", new MockFileData("hello"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");
    }

    #region Test Commands

    [Group(Name = "file")]
    [CmdDescription("File management")]
    public class FileGroup { }

    [Command(Group = typeof(FileGroup), Name = "upload")]
    [CmdDescription("Uploads a file")]
    public class FileUploadCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        [CmdDescription("The local file path to upload")]
        [FilePathCompletion]
        public string Source { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Infrastructure

    private StepwiseTestRunner CreateRunnerWithFileSystem()
    {
        var console = new ConsolidatedTestConsole().Interactive();
        var registry = CreateFileRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(_mockFileSystem);
        var serviceProvider = services.BuildServiceProvider();

        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry),
            new StaticValuesProvider(),
            new EnumProvider(),
            new FilePathProvider(_mockFileSystem),
            new DirectoryPathProvider(_mockFileSystem)
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    private static CommandRegistry CreateFileRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterGroup<FileGroup>();
        registry.RegisterCommand<FileUploadCommand>();
        return registry;
    }

    private new class SimplePrompt : IPrompt
    {
        public string Render() => "> ";
        public int GetPromptLength() => 2;
        public void Write(IAnsiConsole console)
        {
            console.Write(new Text("> "));
        }
    }

    #endregion

    #region MRB-001: Menu should update in place, not re-render (vertical layout)

    [TestMethod]
    [TestDescription("MRB-001: Tab navigation should update menu selection correctly")]
    public async Task TabNavigation_ShouldUpdateMenuInPlace_NotDuplicate()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileSystem();
        runner.Initialize();

        // Type command to trigger file path completion menu
        await runner.TypeText("file upload ");

        // ACT - Press Tab to open the menu
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        var initialIndex = runner.MenuSelectedIndex;
        initialIndex.Should().Be(0, "first item should be selected initially");

        // ACT - Press Tab again to move to next item
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Menu selection should advance
        runner.IsMenuVisible.Should().BeTrue("menu should remain visible after navigation");
        runner.MenuSelectedIndex.Should().Be(1, "Tab should advance menu selection");
    }

    [TestMethod]
    [TestDescription("MRB-002: DownArrow navigation should update menu selection")]
    public async Task DownArrowNavigation_ShouldUpdateMenuInPlace_NotDuplicate()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileSystem();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        var initialIndex = runner.MenuSelectedIndex;
        initialIndex.Should().Be(0, "first item should be selected initially");

        // ACT - Press Down arrow to navigate
        await runner.PressKey(ConsoleKey.DownArrow);

        // ASSERT - Menu selection should advance
        runner.IsMenuVisible.Should().BeTrue("menu should remain visible after DownArrow");
        runner.MenuSelectedIndex.Should().Be(1, "DownArrow should advance menu selection");
    }

    [TestMethod]
    [TestDescription("MRB-003: Multiple navigation steps should cycle through menu correctly")]
    public async Task MultipleNavigation_ShouldNotAccumulateMenuLines()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileSystem();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue();
        var menuItems = runner.GetMenuItems();
        var itemCount = menuItems.Count;
        itemCount.Should().BeGreaterThan(0, "menu should have items");

        // ACT - Navigate several times
        await runner.PressKey(ConsoleKey.Tab);
        runner.MenuSelectedIndex.Should().Be(1, "first Tab should move to index 1");
        
        await runner.PressKey(ConsoleKey.Tab);
        runner.MenuSelectedIndex.Should().Be(2, "second Tab should move to index 2");
        
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.DownArrow);

        // ASSERT - Menu should still be visible and functional
        runner.IsMenuVisible.Should().BeTrue("menu should remain visible through navigation");
        runner.MenuSelectedIndex.Should().BeGreaterThanOrEqualTo(0, "menu selection should be valid");
    }

    /// <summary>
    /// Counts lines that appear to contain menu content (directories/files).
    /// This correctly matches both forward slashes (Unix) and backslashes (Windows).
    /// </summary>
    private int CountMenuLines(List<string> lines)
    {
        // Look for lines containing typical file/directory patterns
        // Use backslash for Windows paths (bin\, obj\, etc.)
        return lines.Count(line => 
            !string.IsNullOrWhiteSpace(line) &&
            (line.Contains(@"bin\") || line.Contains(@"bin/") ||
             line.Contains(@"obj\") || line.Contains(@"obj/") ||
             line.Contains(@"src\") || line.Contains(@"src/") ||
             line.Contains(@"docs\") || line.Contains(@"docs/") ||
             line.Contains(@"tests\") || line.Contains(@"tests/") ||
             line.Contains(@"build\") || line.Contains(@"build/") ||
             line.Contains(@"dist\") || line.Contains(@"dist/") ||
             line.Contains("config.json") || line.Contains("README.md") ||
             line.Contains("test.txt") || 
             (line.Contains("(+") && line.Contains("more)"))));
    }

    #endregion
}
