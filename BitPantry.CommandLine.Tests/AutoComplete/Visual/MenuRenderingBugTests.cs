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
        var console = new VirtualAnsiConsole().Interactive();
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

    private class SimplePrompt : IPrompt
    {
        public string Render() => "> ";
        public int GetPromptLength() => 2;
        public void Write(IAnsiConsole console)
        {
            console.Write(new Text("> "));
        }
    }

    #endregion

    #region MRB-001: Menu should update in place, not re-render

    [TestMethod]
    [TestDescription("MRB-001: Tab navigation should update menu in place, not duplicate it")]
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
        
        // Get the console output after first Tab
        var linesAfterFirstTab = runner.Console.Lines.ToList();
        
        // Count how many lines contain menu items (looking for directories like "bin/", "obj/", etc.)
        var menuLineCountBefore = CountMenuLines(linesAfterFirstTab);

        // ACT - Press Tab again to move to next item
        await runner.PressKey(ConsoleKey.Tab);

        // Get the console output after second Tab
        var linesAfterSecondTab = runner.Console.Lines.ToList();
        var menuLineCountAfter = CountMenuLines(linesAfterSecondTab);

        // ASSERT - Menu should still be only 1 line (or same count as before)
        // If it's duplicating, we'll see 2 lines with menu content
        menuLineCountAfter.Should().Be(menuLineCountBefore,
            "menu should update in place, not be duplicated when navigating");
    }

    [TestMethod]
    [TestDescription("MRB-002: DownArrow navigation should update menu in place")]
    public async Task DownArrowNavigation_ShouldUpdateMenuInPlace_NotDuplicate()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileSystem();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");

        var linesBeforeNav = runner.Console.Lines.ToList();
        var menuLineCountBefore = CountMenuLines(linesBeforeNav);

        // ACT - Press Down arrow to navigate
        await runner.PressKey(ConsoleKey.DownArrow);

        var linesAfterNav = runner.Console.Lines.ToList();
        var menuLineCountAfter = CountMenuLines(linesAfterNav);

        Debug.WriteLine("Lines after DownArrow:");
        for (int i = 0; i < linesAfterNav.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{linesAfterNav[i]}'");
        }

        // ASSERT
        menuLineCountAfter.Should().Be(menuLineCountBefore,
            "menu should update in place when using DownArrow, not be duplicated");
    }

    [TestMethod]
    [TestDescription("MRB-003: Multiple navigation steps should not accumulate menu lines")]
    public async Task MultipleNavigation_ShouldNotAccumulateMenuLines()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileSystem();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue();

        var initialMenuCount = CountMenuLines(runner.Console.Lines.ToList());

        // ACT - Navigate several times
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.Tab);
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.DownArrow);

        var finalLines = runner.Console.Lines.ToList();
        var finalMenuCount = CountMenuLines(finalLines);

        // ASSERT - Menu line count should remain constant
        finalMenuCount.Should().Be(initialMenuCount,
            "menu should not accumulate extra lines through navigation");
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
