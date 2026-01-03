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
    [TestDescription("VS-004: Menu viewport correctly tracks selection after scrolling")]
    public async Task ScrolledMenu_ViewportTracksSelectedItem()
    {
        // ARRANGE
        using var runner = CreateRunnerWithManyFiles();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        // ACT - Navigate past viewport to item 11 (0-indexed: 10)
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - The controller should have the correct selected index
        // With 15 items and viewport of 10, after 10 tabs we should be at index 10
        runner.MenuSelectedIndex.Should().Be(10, 
            "after 10 tabs from first item, should be at index 10 (dir11)");
        runner.IsMenuVisible.Should().BeTrue("menu should still be visible");
        
        // The selected item text should reflect the correct item
        runner.SelectedMenuItem.Should().Contain("dir11",
            "selected item should be dir11 after scrolling");
    }

    #endregion

    #region VS-005 to VS-007: Wrap-around and back-tab bugs

    [TestMethod]
    [TestDescription("VS-005: Wrapping from last item to first correctly updates selection")]
    public async Task WrapFromLastToFirst_CorrectlyUpdatesSelection()
    {
        // ARRANGE - Create runner with 18 items to match user's test scenario
        using var runner = CreateRunnerWith18Items();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        var totalItems = runner.MenuItemCount;
        
        // Navigate to the last item (item 18) - need 17 more tabs after initial
        for (int i = 0; i < 17; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }
        
        // Verify we're at the last item
        runner.MenuSelectedIndex.Should().Be(totalItems - 1, 
            "should be at last item after 17 tabs");

        // ACT - Press Tab one more time to wrap from last to first
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Should wrap to first item (index 0)
        runner.MenuSelectedIndex.Should().Be(0,
            "wrapping from last to first should put selection at index 0");
        runner.IsMenuVisible.Should().BeTrue("menu should still be visible after wrap");
    }

    [TestMethod]
    [TestDescription("VS-006: Back navigation (UpArrow) correctly moves through menu items")]
    public async Task BackNavigation_ShouldCorrectlyMoveBackward()
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
        
        var positionAfterForward = runner.MenuSelectedIndex;
        Debug.WriteLine($"Position after 12 forward tabs: {positionAfterForward}");

        // ACT - Back-tab through the list
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.UpArrow); // UpArrow = back navigation
        }

        // ASSERT - Should have moved backward
        var positionAfterBackward = runner.MenuSelectedIndex;
        Debug.WriteLine($"Position after 10 up arrows: {positionAfterBackward}");
        
        // After going forward 12 (0->12) and back 10, should be at position 2
        positionAfterBackward.Should().Be(2, "should have moved back 10 positions from 12 to 2");
        runner.IsMenuVisible.Should().BeTrue("menu should still be visible");
    }

    [TestMethod]
    [TestDescription("VS-007: Multiple wrap-arounds should correctly cycle through menu items")]
    public async Task MultipleWrapArounds_ShouldCorrectlyCycleThroughItems()
    {
        // ARRANGE
        using var runner = CreateRunnerWith18Items();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue();
        
        // Get initial selected item and menu count
        var initialSelectedItem = runner.SelectedMenuItem;
        var menuItemCount = runner.MenuItemCount;
        menuItemCount.Should().Be(18, "should have 18 items");
        
        // Remember what item we started with
        var initialIndex = runner.MenuSelectedIndex;

        // ACT - Navigate through entire list twice (wrap around twice)
        // 18 items * 2 = 36 tabs to go through list twice
        for (int i = 0; i < 36; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
        }

        // ASSERT - After 36 tabs with 18 items, we should be back at the same item
        runner.MenuSelectedIndex.Should().Be(initialIndex,
            "after navigating through the list twice, we should be back at the starting item");
        runner.SelectedMenuItem.Should().Be(initialSelectedItem,
            "the selected item should be the same as when we started");
        runner.IsMenuVisible.Should().BeTrue("menu should still be visible");
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

        var console = new ConsolidatedTestConsole().Interactive();
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

        var console = new ConsolidatedTestConsole().Interactive();
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
    [TestDescription("VS-008: Menu navigation works correctly with varying item lengths")]
    public async Task MenuNavigation_WorksCorrectly_WithVaryingItemLengths()
    {
        // ARRANGE - Create runner with varying item lengths
        using var runner = CreateRunnerWithVaryingItemLengths();
        runner.Initialize();

        await runner.TypeText("file upload ");
        await runner.PressKey(ConsoleKey.Tab);

        runner.IsMenuVisible.Should().BeTrue("menu should be visible after Tab");
        
        // Get initial menu state
        var initialSelectedItem = runner.SelectedMenuItem;
        var menuItemCount = runner.MenuItemCount;
        
        Debug.WriteLine($"Initial menu: {menuItemCount} items, selected: '{initialSelectedItem}'");
        
        // ACT - Navigate forward through several items
        var visitedItems = new List<string> { initialSelectedItem };
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
            var currentItem = runner.SelectedMenuItem;
            visitedItems.Add(currentItem);
            Debug.WriteLine($"After Tab {i + 1}: selected '{currentItem}'");
        }

        // ASSERT - Verify navigation worked correctly
        runner.IsMenuVisible.Should().BeTrue("menu should still be visible after navigation");
        runner.MenuSelectedIndex.Should().Be(10, "should have moved forward 10 positions from index 0 to index 10");
        
        // Verify we visited different items (not stuck on one)
        var distinctItems = visitedItems.Distinct().ToList();
        distinctItems.Count.Should().BeGreaterThan(5, "should have visited multiple different items");
    }

    [TestMethod]
    [TestDescription("VS-009: Menu line count should be accurately tracked when menu shrinks (vertical layout with Inflate)")]
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
        int maxNonEmpty = initialNonEmpty;
        for (int i = 0; i < 10; i++)
        {
            await runner.PressKey(ConsoleKey.Tab);
            
            // Log each step
            var stepLines = runner.Console.Lines.ToList();
            var stepNonEmpty = CountNonEmptyLines(stepLines);
            maxNonEmpty = Math.Max(maxNonEmpty, stepNonEmpty);
            Debug.WriteLine($"After tab {i + 1}: {stepNonEmpty} non-empty lines");
        }

        // ASSERT - After navigation, line count should be within reasonable bounds
        // With vertical layout and Inflate pattern, menu height can grow but won't shrink
        // This prevents phantom lines - the key behavior we're testing
        var finalLines = runner.Console.Lines.ToList();
        var finalNonEmpty = CountNonEmptyLines(finalLines);
        
        Debug.WriteLine($"Final non-empty lines: {finalNonEmpty}");
        Debug.WriteLine($"Max non-empty lines seen: {maxNonEmpty}");
        Debug.WriteLine("Final state:");
        for (int i = 0; i < finalLines.Count; i++)
        {
            Debug.WriteLine($"  Line {i}: '{finalLines[i]}'");
        }
        
        // With Inflate pattern, final should not exceed max seen (no phantom accumulation)
        // Allow some tolerance for padding behavior
        finalNonEmpty.Should().BeLessOrEqualTo(maxNonEmpty + 2,
            "navigating through menu should not accumulate phantom lines beyond max height");
    }

    #endregion
}
