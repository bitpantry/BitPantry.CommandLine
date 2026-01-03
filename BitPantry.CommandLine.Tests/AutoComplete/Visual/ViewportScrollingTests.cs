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
/// TDD tests for viewport scrolling behavior.
/// 
/// When navigating past the visible viewport, the menu should scroll to keep
/// the selected item visible, rather than having the selection disappear.
/// </summary>
[TestClass]
public class ViewportScrollingTests : VisualTestBase
{
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        // Create 15 directories to ensure we have more items than viewport (10)
        _mockFileSystem.AddDirectory(@"C:\work");
        for (int i = 1; i <= 15; i++)
        {
            _mockFileSystem.AddDirectory($@"C:\work\dir{i:D2}");
        }
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

    private StepwiseTestRunner CreateRunnerWithManyFiles()
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
            new FilePathCompletionProvider(_mockFileSystem)
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    private CommandRegistry CreateFileRegistry()
    {
        var registry = new CommandRegistry();
        registry.RegisterCommand(typeof(FileUploadCommand));
        return registry;
    }

    #endregion

    #region VS-001: Scrolling viewport when navigating past end

    [TestMethod]
    [TestDescription("VS-001: When Tab navigates past viewport, menu scrolls to show selected item")]
    public async Task TabPastViewport_ShouldScrollToShowSelectedItem()
    {
        // ARRANGE
        using var runner = CreateRunnerWithManyFiles();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        
        // Get initial menu state - should show first 10 items with dir01 selected
        var initialLines = runner.Console.Lines.ToList();
        initialLines.Any(l => l.Contains("dir01")).Should().BeTrue("dir01 should be visible initially");
        
        // ACT - Navigate past the visible viewport (press Tab 10 times to reach item 11)
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - Item 11 (dir11) should now be visible
        var afterLines = runner.Console.Lines.ToList();
        
        Debug.WriteLine("Lines after navigating past viewport:");
        for (int i = 0; i < afterLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{afterLines[i]}'");
        }

        afterLines.Any(l => l.Contains("dir11")).Should().BeTrue(
            "dir11 should be visible after scrolling - the viewport should scroll to show the selected item");
    }

    [TestMethod]
    [TestDescription("VS-002: Selected item should always be highlighted, even after scrolling")]
    public async Task SelectedItem_ShouldBeHighlighted_AfterScrolling()
    {
        // ARRANGE
        using var runner = CreateRunnerWithManyFiles();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        // ACT - Navigate past viewport to item 11
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - The menu should show dir11 as selected
        var selectedItem = runner.SelectedMenuItem;
        selectedItem.Should().NotBeNullOrEmpty();
        selectedItem.Should().Contain("dir11");
    }

    [TestMethod]
    [TestDescription("VS-003: UpArrow from scrolled position should scroll back")]
    public async Task UpArrowFromScrolledPosition_ShouldScrollBack()
    {
        // ARRANGE
        using var runner = CreateRunnerWithManyFiles();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        // Navigate to item 11 (past viewport)
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ACT - Press UpArrow to go back
        await runner.PressKey(ConsoleKey.UpArrow);

        // ASSERT - dir10 should now be visible and selected
        var afterLines = runner.Console.Lines.ToList();
        afterLines.Any(l => l.Contains("dir10")).Should().BeTrue(
            "dir10 should be visible after UpArrow from scrolled position");
    }

    [TestMethod]
    [TestDescription("VS-004: Menu should not show items before viewport after scrolling")]
    public async Task ScrolledMenu_ShouldNotShowItemsBeforeViewport()
    {
        // ARRANGE
        using var runner = CreateRunnerWithManyFiles();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        // ACT - Navigate past viewport to item 11
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - dir01 should NOT be visible when viewport has scrolled past it
        var afterLines = runner.Console.Lines.ToList();
        var menuLine = string.Join(" ", afterLines.Skip(1)); // Skip prompt line
        
        Debug.WriteLine($"Menu content: {menuLine}");
        
        afterLines.Any(l => l.Contains("dir01")).Should().BeFalse(
            "dir01 should NOT be visible when viewport has scrolled past it");
    }

    #endregion

    #region VS-005 to VS-007: Wrap-around and back-tab bugs

    [TestMethod]
    [TestDescription("VS-005: Wrapping from last item to first should not add extra lines")]
    public async Task WrapFromLastToFirst_ShouldNotAddExtraLines()
    {
        // ARRANGE - Create runner with 18 items to match user's test scenario
        using var runner = CreateRunnerWith18Items();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        
        // Navigate to the last item (item 18) - need 17 more tabs after initial
        for (int i = 0; i < 17; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }
        
        // Get state before wrap
        var linesBeforeWrap = runner.Console.Lines.ToList();
        var menuLineCountBefore = CountNonEmptyLines(linesBeforeWrap);
        
        Debug.WriteLine("Lines BEFORE wrap (at last item):");
        for (int i = 0; i < linesBeforeWrap.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{linesBeforeWrap[i]}'");
        }

        // ACT - Press Tab one more time to wrap from last to first
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Should not have added extra lines
        var linesAfterWrap = runner.Console.Lines.ToList();
        var menuLineCountAfter = CountNonEmptyLines(linesAfterWrap);
        
        Debug.WriteLine("Lines AFTER wrap (back to first item):");
        for (int i = 0; i < linesAfterWrap.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{linesAfterWrap[i]}'");
        }
        Debug.WriteLine($"Menu lines before: {menuLineCountBefore}, after: {menuLineCountAfter}");

        menuLineCountAfter.Should().BeLessOrEqualTo(menuLineCountBefore,
            "wrapping from last to first item should not add extra lines");
    }

    [TestMethod]
    [TestDescription("VS-006: Back-tabbing through entire list should not add extra lines")]
    public async Task BackTabThroughList_ShouldNotAddExtraLines()
    {
        // ARRANGE - Create runner with 18 items
        using var runner = CreateRunnerWith18Items();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue();
        
        // Navigate forward past viewport first
        for (int i = 0; i < 12; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }
        
        // Get line count at this point
        var linesAtStart = runner.Console.Lines.ToList();
        var lineCountAtStart = CountNonEmptyLines(linesAtStart);
        
        Debug.WriteLine("Lines at start of back-tab test:");
        for (int i = 0; i < linesAtStart.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{linesAtStart[i]}'");
        }

        // ACT - Back-tab through the entire list
        for (int i = 0; i < 15; i++)
        {
            await runner.PressKey(ConsoleKey.UpArrow); // UpArrow = back navigation
        }

        // ASSERT
        var linesAfterBackTab = runner.Console.Lines.ToList();
        var lineCountAfterBackTab = CountNonEmptyLines(linesAfterBackTab);
        
        Debug.WriteLine("Lines AFTER back-tabbing:");
        for (int i = 0; i < linesAfterBackTab.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{linesAfterBackTab[i]}'");
        }
        Debug.WriteLine($"Line count at start: {lineCountAtStart}, after back-tab: {lineCountAfterBackTab}");

        lineCountAfterBackTab.Should().BeLessOrEqualTo(lineCountAtStart,
            "back-tabbing through the list should not add extra lines");
    }

    [TestMethod]
    [TestDescription("VS-007: Multiple wrap-arounds should not accumulate lines")]
    public async Task MultipleWrapArounds_ShouldNotAccumulateLines()
    {
        // ARRANGE
        using var runner = CreateRunnerWith18Items();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue();
        
        // Get initial line count
        var initialLines = runner.Console.Lines.ToList();
        var initialLineCount = CountNonEmptyLines(initialLines);

        // ACT - Navigate through entire list twice (wrap around twice)
        // 18 items * 2 = 36 tabs to go through list twice
        for (int i = 0; i < 36; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT
        var finalLines = runner.Console.Lines.ToList();
        var finalLineCount = CountNonEmptyLines(finalLines);
        
        Debug.WriteLine("Lines after 2 full wrap-arounds:");
        for (int i = 0; i < finalLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{finalLines[i]}'");
        }
        Debug.WriteLine($"Initial lines: {initialLineCount}, Final lines: {finalLineCount}");

        finalLineCount.Should().Be(initialLineCount,
            "multiple wrap-arounds should not accumulate extra lines");
    }

    private int CountNonEmptyLines(List<string> lines)
    {
        return lines.Count(l => !string.IsNullOrWhiteSpace(l));
    }

    private StepwiseTestRunner CreateRunnerWith18Items()
    {
        // Create a file system with exactly 18 items to match user's test scenario
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(@"C:\work");
        for (int i = 1; i <= 18; i++)
        {
            mockFs.AddDirectory($@"C:\work\item{i:D2}");
        }
        mockFs.Directory.SetCurrentDirectory(@"C:\work");

        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateFileRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(mockFs);
        var serviceProvider = services.BuildServiceProvider();

        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new FilePathCompletionProvider(mockFs)
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    /// <summary>
    /// Creates a runner with items of varying lengths - some long, some short.
    /// This simulates the real scenario where tabbing through items causes
    /// the menu to shrink from 3 lines to 2 lines when shorter items are in the viewport.
    /// 
    /// Items layout (at 80 char width):
    /// - First several items have LONG names (forcing 3 lines in viewport)
    /// - Later items have SHORT names (allowing 2 lines in viewport)
    /// </summary>
    private StepwiseTestRunner CreateRunnerWithVaryingItemLengths()
    {
        var mockFs = new MockFileSystem();
        mockFs.AddDirectory(@"C:\work");
        
        // First 8 items: LONG names (e.g., "c:\work\long_directory_name_01\")
        // These are ~32 chars each, so 10 items at 32 chars = 320 chars = 4 lines at 80 width
        for (int i = 1; i <= 8; i++)
        {
            mockFs.AddDirectory($@"C:\work\long_directory_name_{i:D2}");
        }
        
        // Next 10 items: SHORT names (e.g., "c:\work\d01\")
        // These are ~13 chars each, so 10 items at 13 chars = 130 chars = ~2 lines at 80 width
        for (int i = 9; i <= 18; i++)
        {
            mockFs.AddDirectory($@"C:\work\d{i:D2}");
        }
        
        mockFs.Directory.SetCurrentDirectory(@"C:\work");

        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateFileRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(mockFs);
        var serviceProvider = services.BuildServiceProvider();

        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new FilePathCompletionProvider(mockFs)
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    #endregion

    #region VS-008: Phantom line when menu shrinks from 3 to 2 lines

    [TestMethod]
    [TestDescription("VS-008: When menu shrinks from 3 lines to 2 lines during navigation, no phantom line should remain")]
    public async Task MenuShrinks_FromThreeLinesToTwoLines_ShouldNotLeavePhantomLine()
    {
        // ARRANGE - Create runner with varying item lengths
        // First items are long (3-line menu), later items are short (2-line menu)
        using var runner = CreateRunnerWithVaryingItemLengths();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        
        // Get initial menu state - with long items, should be 3+ lines
        var initialLines = runner.Console.Lines.ToList();
        Debug.WriteLine("Initial menu (long items visible):");
        for (int i = 0; i < initialLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{initialLines[i]}'");
        }
        
        // Count how many lines have "(+" prefix - should be exactly 0 or 1
        var initialPrefixCount = initialLines.Count(l => l.Contains("(+"));
        
        // ACT - Navigate forward until we get to short items
        // After 8+ tabs, the viewport should show short items and shrink to 2 lines
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - Check for phantom lines
        var afterLines = runner.Console.Lines.ToList();
        Debug.WriteLine("After navigating to short items:");
        for (int i = 0; i < afterLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{afterLines[i]}'");
        }
        
        // Count "(+" prefixes - should still be at most 1 (the current before/more indicator)
        var afterPrefixCount = afterLines.Count(l => l.Contains("(+"));
        
        // The key assertion: we should not have duplicate "(+" lines (phantom from old render)
        afterPrefixCount.Should().BeLessOrEqualTo(2, 
            "there should be at most 2 '(+' indicators (before and more), not phantom duplicates");
        
        // Also verify no line appears twice (phantom duplicate)
        var menuLines = afterLines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        var distinctMenuLines = menuLines.Distinct().ToList();
        menuLines.Count.Should().Be(distinctMenuLines.Count,
            "there should be no duplicate menu lines (phantom lines from previous render)");
    }

    [TestMethod]
    [TestDescription("VS-009: Menu line count should be accurately tracked when menu shrinks")]
    public async Task MenuLineCount_ShouldBeAccurate_WhenMenuShrinks()
    {
        // ARRANGE
        using var runner = CreateRunnerWithVaryingItemLengths();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        // Get initial non-empty line count
        var initialLines = runner.Console.Lines.ToList();
        var initialNonEmpty = CountNonEmptyLines(initialLines);
        Debug.WriteLine($"Initial non-empty lines: {initialNonEmpty}");
        
        // ACT - Navigate through enough items that menu should shrink
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
            
            // Log each step
            var stepLines = runner.Console.Lines.ToList();
            var stepNonEmpty = CountNonEmptyLines(stepLines);
            Debug.WriteLine($"After tab {i + 1}: {stepNonEmpty} non-empty lines");
        }

        // ASSERT - After navigation, we should still have consistent line count
        // (not accumulating phantom lines)
        var finalLines = runner.Console.Lines.ToList();
        var finalNonEmpty = CountNonEmptyLines(finalLines);
        
        Debug.WriteLine($"Final non-empty lines: {finalNonEmpty}");
        Debug.WriteLine("Final state:");
        for (int i = 0; i < finalLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{finalLines[i]}'");
        }
        
        // The line count shouldn't grow beyond what's reasonable
        // Initial: prompt + menu (2-4 lines max)
        // Final: should be similar, not growing
        finalNonEmpty.Should().BeLessOrEqualTo(initialNonEmpty + 1,
            "navigating through menu should not accumulate extra lines");
    }

    #endregion
}
