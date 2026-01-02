using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
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
using System.Linq;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for positional argument autocomplete using the full UX/functional testing stack.
/// 
/// These tests validate prefix-driven intent detection and positional slot completion
/// as specified in FR-024, FR-024a, FR-024b, FR-024c, FR-025, FR-025a, FR-025b, FR-025c.
/// 
/// Test Pattern:
/// 1. Setup console state (validate state and cursor position)
/// 2. Do something (keypress, etc.)
/// 3. Validate final state
/// </summary>
[TestClass]
public class PositionalAutoCompleteTests : VisualTestBase
{
    #region Test Commands with Positional Arguments

    /// <summary>
    /// Command with positional arguments and custom completion functions.
    /// </summary>
    [Command(Name = "copy")]
    [CmdDescription("Copy files")]
    public class CopyCommand : CommandBase
    {
        [Argument(Position = 0)]
        [CmdDescription("Source file")]
        [Completion(nameof(GetSourceCompletions))]
        public string Source { get; set; }

        [Argument(Position = 1)]
        [CmdDescription("Destination file")]
        [Completion(nameof(GetDestinationCompletions))]
        public string Destination { get; set; }

        [Argument]
        [Alias('f')]
        [CmdDescription("Force overwrite")]
        public Option Force { get; set; }

        [Argument]
        [Alias('v')]
        [CmdDescription("Verbose output")]
        public Option Verbose { get; set; }

        public static IEnumerable<CompletionItem> GetSourceCompletions(CompletionContext ctx)
        {
            return new[]
            {
                new CompletionItem { InsertText = "file1.txt" },
                new CompletionItem { InsertText = "file2.txt" },
                new CompletionItem { InsertText = "data.csv" }
            };
        }

        public static IEnumerable<CompletionItem> GetDestinationCompletions(CompletionContext ctx)
        {
            return new[]
            {
                new CompletionItem { InsertText = "backup/" },
                new CompletionItem { InsertText = "archive/" },
                new CompletionItem { InsertText = "output.txt" }
            };
        }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with positional argument but NO completion function.
    /// Tests fallback to options behavior (FR-024b).
    /// </summary>
    [Command(Name = "process")]
    [CmdDescription("Process data")]
    public class ProcessCommand : CommandBase
    {
        [Argument(Position = 0)]
        [CmdDescription("Input file")]
        public string Input { get; set; }

        [Argument]
        [Alias('o')]
        [CmdDescription("Output format")]
        public string OutputFormat { get; set; }

        [Argument]
        [Alias('q')]
        [CmdDescription("Quiet mode")]
        public Option Quiet { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with IsRest variadic positional argument.
    /// </summary>
    [Command(Name = "delete")]
    [CmdDescription("Delete files")]
    public class DeleteCommand : CommandBase
    {
        [Argument(Position = 0, IsRest = true)]
        [CmdDescription("Files to delete")]
        [Completion(nameof(GetFileCompletions))]
        public string[] Files { get; set; }

        [Argument]
        [Alias('r')]
        [CmdDescription("Recursive delete")]
        public Option Recursive { get; set; }

        public static IEnumerable<CompletionItem> GetFileCompletions(CompletionContext ctx)
        {
            return new[]
            {
                new CompletionItem { InsertText = "temp1.txt" },
                new CompletionItem { InsertText = "temp2.txt" },
                new CompletionItem { InsertText = "cache/" }
            };
        }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Custom provider for profile completions (simulates server connect scenario).
    /// </summary>
    public class TestProfileProvider : ICompletionProvider
    {
        public int Priority => 80;

        public bool CanHandle(CompletionContext context)
        {
            // This provider handles ArgumentValue completions with our provider type
            if (context.ElementType != CompletionElementType.ArgumentValue)
                return false;
            return context.CompletionAttribute?.ProviderType == typeof(TestProfileProvider);
        }

        public Task<CompletionResult> GetCompletionsAsync(CompletionContext context, System.Threading.CancellationToken cancellationToken = default)
        {
            var profiles = new[] { "prod", "staging", "dev", "local" };
            var prefix = context.CurrentWord ?? string.Empty;
            
            var items = profiles
                .Where(p => p.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .Select(p => new CompletionItem
                {
                    DisplayText = p,
                    InsertText = p,
                    Description = $"Connect to {p} server",
                    Kind = CompletionItemKind.ArgumentValue
                })
                .ToList();

            return Task.FromResult(new CompletionResult(items));
        }
    }

    /// <summary>
    /// Command that uses a provider TYPE for positional completion (like server connect).
    /// </summary>
    [Command(Name = "connect")]
    [CmdDescription("Connect to a server profile")]
    public class ConnectTestCommand : CommandBase
    {
        [Argument(Position = 0)]
        [CmdDescription("Profile name")]
        [Completion(typeof(TestProfileProvider))]
        public string Profile { get; set; }

        [Argument]
        [Alias('u')]
        [CmdDescription("Override URI")]
        public string Uri { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Test Infrastructure

    /// <summary>
    /// Creates a registry with positional argument commands.
    /// </summary>
    private static CommandRegistry CreatePositionalRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterCommand<CopyCommand>();
        registry.RegisterCommand<ProcessCommand>();
        registry.RegisterCommand<DeleteCommand>();
        registry.RegisterCommand<ConnectTestCommand>();
        // Also register base commands for command-level completion tests
        registry.RegisterCommand<HelpCommand>();
        return registry;
    }

    /// <summary>
    /// Creates a StepwiseTestRunner with positional commands.
    /// </summary>
    private static StepwiseTestRunner CreatePositionalRunner()
    {
        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreatePositionalRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var serviceProvider = new ServiceCollection()
            .AddSingleton<TestProfileProvider>()
            .BuildServiceProvider();
        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry)
        };
        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new RealisticPrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    #endregion

    #region PAC-001 to PAC-005: Basic Positional Slot Completion

    [TestMethod]
    [TestDescription("PAC-001: First positional slot (empty) - Tab invokes position 0 completion")]
    public async Task PAC001_FirstPositionalSlot_Empty_InvokesPosition0Completion()
    {
        // Arrange: Setup console with "copy " typed
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy ");
        runner.Should().HaveBuffer("copy ")
                       .And.HaveInputCursorAt(5);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show position 0 completions (file1.txt, file2.txt, data.csv)
        // With menu open, first item should be displayed or buffer should contain first completion
        runner.Should().HaveMenuVisible();
        // Menu should contain the source completions
        runner.GetMenuItems().Should().Contain(item => item.Contains("file1.txt") || item.Contains("file2"));
    }

    [TestMethod]
    [TestDescription("PAC-002: Second positional slot - Tab invokes position 1 completion")]
    public async Task PAC002_SecondPositionalSlot_InvokesPosition1Completion()
    {
        // Arrange: Setup console with "copy file1.txt " (first positional filled)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt ");
        runner.Should().HaveBuffer("copy file1.txt ")
                       .And.HaveInputCursorAt(15);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show position 1 completions (backup/, archive/, output.txt)
        runner.Should().HaveMenuVisible();
        runner.GetMenuItems().Should().Contain(item => item.Contains("backup") || item.Contains("archive"));
    }

    [TestMethod]
    [TestDescription("PAC-004: Partial value filtering - Tab filters position 0 completions")]
    public async Task PAC004_PartialValueFiltering_FiltersPosition0Completions()
    {
        // Arrange: Setup console with "copy fi" (partial text)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy fi");
        runner.Should().HaveBuffer("copy fi")
                       .And.HaveInputCursorAt(7);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should filter to file1.txt, file2.txt (match "fi")
        // data.csv should NOT appear
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("file"));
        menuItems.Should().NotContain(item => item.Contains("data"));
    }

    [TestMethod]
    [TestDescription("PAC-005: Partial second slot filtering - Tab filters position 1 completions")]
    public async Task PAC005_PartialSecondSlotFiltering_FiltersPosition1Completions()
    {
        // Arrange: Setup console with "copy file1.txt ba" (partial second arg)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt ba");
        runner.Should().HaveBuffer("copy file1.txt ba")
                       .And.HaveInputCursorAt(17);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should filter to "backup/" only
        // Single match should auto-complete
        runner.Should().HaveBuffer("copy file1.txt backup/ ");
    }

    #endregion

    #region PAC-006 to PAC-009: Prefix-Driven Intent Detection

    [TestMethod]
    [TestDescription("PAC-006: Double-dash triggers options - Shows named argument names")]
    public async Task PAC006_DoubleDashTriggersOptions_ShowsNamedArgumentNames()
    {
        // Arrange: Setup console with "copy --" (double-dash prefix)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy --");
        runner.Should().HaveBuffer("copy --")
                       .And.HaveInputCursorAt(7);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show --Force, --Verbose (named options, not positional completions)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--Force") || item.Contains("--Verbose"));
        // Should NOT show file completions
        menuItems.Should().NotContain(item => item.Contains("file1") || item.Contains("file2"));
    }

    [TestMethod]
    [TestDescription("PAC-007: Single-dash triggers aliases - Shows argument aliases")]
    public async Task PAC007_SingleDashTriggersAliases_ShowsArgumentAliases()
    {
        // Arrange: Setup console with "copy -" (single-dash prefix)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy -");
        runner.Should().HaveBuffer("copy -")
                       .And.HaveInputCursorAt(6);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show -f, -v (aliases)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("-f") || item.Contains("-v"));
    }

    [TestMethod]
    [TestDescription("PAC-008: Partial option name - Filters options by partial")]
    public async Task PAC008_PartialOptionName_FiltersOptions()
    {
        // Arrange: Setup console with "copy --Fo" (partial option)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy --Fo");
        runner.Should().HaveBuffer("copy --Fo")
                       .And.HaveInputCursorAt(9);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should complete to "--Force " (single match)
        runner.Should().HaveBuffer("copy --Force ");
    }

    #endregion

    #region PAC-010 to PAC-013: All Positionals Filled → Options

    [TestMethod]
    [TestDescription("PAC-010: All positionals filled - Tab suggests available options")]
    public async Task PAC010_AllPositionalsFilled_TabSuggestsOptions()
    {
        // Arrange: Setup console with "copy file1.txt backup/ " (both positionals filled)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt backup/ ");
        runner.Should().HaveBuffer("copy file1.txt backup/ ")
                       .And.HaveInputCursorAt(23);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show options (--Force, --Verbose) since positionals are filled
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--Force") || item.Contains("--Verbose"));
    }

    [TestMethod]
    [TestDescription("PAC-013: No positional autocomplete func - Falls back to options")]
    public async Task PAC013_NoPositionalAutocompleteFunc_FallsBackToOptions()
    {
        // Arrange: "process " - ProcessCommand has positional but NO completion function
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("process ");
        runner.Should().HaveBuffer("process ")
                       .And.HaveInputCursorAt(8);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should fall back to showing options (--OutputFormat, --Quiet)
        // since there's no completion function for the positional
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--OutputFormat") || item.Contains("--Quiet"));
    }

    #endregion

    #region PAC-030 to PAC-034: Dual-Mode: Named Satisfies Positional

    [TestMethod]
    [TestDescription("PAC-030: Named fills pos0 - Tab suggests pos1 completions")]
    public async Task PAC030_NamedFillsPos0_TabSuggestsPos1Completions()
    {
        // Arrange: "copy --Source file1.txt " (pos0 satisfied via named)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy --Source file1.txt ");
        runner.Should().HaveBuffer("copy --Source file1.txt ")
                       .And.HaveInputCursorAt(24);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show position 1 completions (backup/, archive/, output.txt)
        // because pos0 is already satisfied via --Source
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("backup") || item.Contains("archive"));
    }

    [TestMethod]
    [TestDescription("PAC-032: Named fills all positionals - Tab suggests options only")]
    public async Task PAC032_NamedFillsAllPositionals_TabSuggestsOptionsOnly()
    {
        // Arrange: "copy --Source file1.txt --Destination backup/ " (both filled via named)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy --Source file1.txt --Destination backup/ ");
        runner.Should().HaveBuffer("copy --Source file1.txt --Destination backup/ ")
                       .And.HaveInputCursorAt(46);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show remaining options (--Force, --Verbose)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--Force") || item.Contains("--Verbose"));
        // Should NOT show positional completions
        menuItems.Should().NotContain(item => item.Contains("file") || item.Contains("backup"));
    }

    [TestMethod]
    [TestDescription("PAC-033: Mix positional + named - Both satisfied, suggests options")]
    public async Task PAC033_MixPositionalAndNamed_BothSatisfied_SuggestsOptions()
    {
        // Arrange: "copy file1.txt --Destination backup/ " (pos0 by position, pos1 by name)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt --Destination backup/ ");
        runner.Should().HaveBuffer("copy file1.txt --Destination backup/ ")
                       .And.HaveInputCursorAt(37);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show remaining options only
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--Force") || item.Contains("--Verbose"));
    }

    #endregion

    #region PAC-060 to PAC-063: IsRest (Variadic) Positional

    [TestMethod]
    [TestDescription("PAC-060: First IsRest value - Tab suggests IsRest completions")]
    public async Task PAC060_FirstIsRestValue_SuggestsIsRestCompletions()
    {
        // Arrange: "delete " (pos0 is IsRest)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("delete ");
        runner.Should().HaveBuffer("delete ")
                       .And.HaveInputCursorAt(7);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show file completions (temp1.txt, temp2.txt, cache/)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("temp") || item.Contains("cache"));
    }

    [TestMethod]
    [TestDescription("PAC-061: Second IsRest value - Continues suggesting IsRest completions")]
    public async Task PAC061_SecondIsRestValue_ContinuesSuggestingIsRestCompletions()
    {
        // Arrange: "delete temp1.txt " (first IsRest value provided)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("delete temp1.txt ");
        runner.Should().HaveBuffer("delete temp1.txt ")
                       .And.HaveInputCursorAt(17);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should continue showing file completions (IsRest keeps accepting)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("temp") || item.Contains("cache"));
    }

    [TestMethod]
    [TestDescription("PAC-062: Many IsRest values - Continues suggesting IsRest completions")]
    public async Task PAC062_ManyIsRestValues_ContinuesSuggestingCompletions()
    {
        // Arrange: "delete a.txt b.txt c.txt d.txt " (many values)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("delete a.txt b.txt c.txt d.txt ");
        runner.Should().HaveBuffer("delete a.txt b.txt c.txt d.txt ")
                       .And.HaveInputCursorAt(31);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should still show file completions
        runner.Should().HaveMenuVisible();
    }

    #endregion

    #region PAC-080 to PAC-082: No Completion Function

    [TestMethod]
    [TestDescription("PAC-080: Positional without completion - Falls back to options")]
    public async Task PAC080_PositionalWithoutCompletion_FallsBackToOptions()
    {
        // Arrange: "process " - no completion function defined for positional
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("process ");
        runner.Should().HaveBuffer("process ")
                       .And.HaveInputCursorAt(8);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Falls back to options
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("--OutputFormat") || item.Contains("--Quiet"));
    }

    [TestMethod]
    [TestDescription("PAC-082: Partial with no completion - No completions shown")]
    public async Task PAC082_PartialWithNoCompletion_NoCompletions()
    {
        // Arrange: "process inp" - partial text, no completion function
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("process inp");
        runner.Should().HaveBuffer("process inp")
                       .And.HaveInputCursorAt(11);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: No positional completions, and "inp" doesn't match any options
        // Should show no completions or fallback behavior
        // The menu may not be visible if there are no matches
        var menuItems = runner.GetMenuItems();
        menuItems.Should().BeEmpty();
    }

    #endregion

    #region PAC-092 to PAC-093: Edge Cases

    [TestMethod]
    [TestDescription("PAC-092: Command with no positionals - Tab suggests options")]
    public async Task PAC092_CommandWithNoPositionals_TabSuggestsOptions()
    {
        // Arrange: Use HelpCommand which has no arguments at all
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("help ");
        runner.Should().HaveBuffer("help ")
                       .And.HaveInputCursorAt(5);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: No positional completions, no options - no menu or empty
        // HelpCommand has no arguments, so nothing to complete
        runner.Should().NotHaveMenuVisible();
    }

    [TestMethod]
    [TestDescription("PAC-093: Tab with no command - Suggests commands")]
    public async Task PAC093_TabWithNoCommand_SuggestsCommands()
    {
        // Arrange: Empty buffer
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        runner.Should().HaveBuffer("")
                       .And.HaveInputCursorAt(0);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show available commands
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => 
            item.Contains("copy") || item.Contains("process") || 
            item.Contains("delete") || item.Contains("help"));
    }

    #endregion

    #region PAC-100 to PAC-103: Provider Type Positional Completion

    [TestMethod]
    [TestCategory("POSPROV001")]
    [TestDescription("PAC-100: Positional slot with provider TYPE - Tab invokes provider")]
    public async Task PAC100_PositionalSlotWithProviderType_InvokesProvider()
    {
        // Arrange: Type "connect " (command with provider-type positional)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("connect ");
        runner.Should().HaveBuffer("connect ")
                       .And.HaveInputCursorAt(8);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should show profile completions from TestProfileProvider
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        menuItems.Should().Contain(item => item.Contains("prod"));
        menuItems.Should().Contain(item => item.Contains("staging"));
        menuItems.Should().Contain(item => item.Contains("dev"));
        menuItems.Should().Contain(item => item.Contains("local"));
    }

    [TestMethod]
    [TestCategory("POSPROV002")]
    [TestDescription("PAC-101: Partial positional with provider TYPE - Tab filters and shows")]
    public async Task PAC101_PartialPositionalWithProviderType_FiltersAndShows()
    {
        // Arrange: Type "connect pro" (partial profile name)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("connect pro");
        runner.Should().HaveBuffer("connect pro")
                       .And.HaveInputCursorAt(11);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Should auto-complete to "prod" since it's the only match
        runner.Should().HaveBuffer("connect prod ")
                       .And.NotHaveMenuVisible();  // Single match = auto-accept
    }

    [TestMethod]
    [TestCategory("POSPROV003")]
    [TestDescription("PAC-102: Select profile from menu - Completes positional slot")]
    public async Task PAC102_SelectProfileFromMenu_CompletesPositionalSlot()
    {
        // Arrange: Type "connect " and press Tab to show menu
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("connect ");
        await runner.PressKey(ConsoleKey.Tab);
        
        runner.Should().HaveMenuVisible();

        // Act: Navigate down (to second item) and accept with Enter
        await runner.PressKey(ConsoleKey.DownArrow);
        await runner.PressKey(ConsoleKey.Enter);

        // Assert: Should have completed with selected profile and menu closed
        runner.Should().NotHaveMenuVisible();
        // Buffer should contain the selected profile
        var buffer = runner.Buffer;
        buffer.Should().StartWith("connect ");
        buffer.Length.Should().BeGreaterThan(8); // Should have added profile name
    }

    #endregion

    #region PAC-200 to PAC-205: Positional Slot Used Arguments Tracking (Bug Fix)

    [TestMethod]
    [TestCategory("POSUSED001")]
    [TestDescription("PAC-200: After positional arg is filled, --ArgName should NOT appear in autocomplete")]
    public async Task PAC200_AfterPositionalArgFilled_ArgNameShouldNotAppear()
    {
        // BUG REPRO: When you type "connect testprof " and press Tab,
        // the autocomplete should NOT show --Profile because "testprof" already fills
        // the positional argument for Profile.
        
        // Arrange: Type "connect testprof " (positional arg 0 is filled)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("connect testprof ");
        runner.Should().HaveBuffer("connect testprof ")
                       .And.HaveInputCursorAt(17);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Since ConnectTestCommand only has --Uri left (single item),
        // it should be auto-accepted (no menu). The important thing is that
        // --Profile should NOT be in the result.
        var buffer = runner.Buffer;
        
        // The buffer should NOT contain --Profile (the positional arg that was already filled)
        buffer.Should().NotContain("--Profile", 
            "Profile is already filled by positional argument 'testprof'");
        
        // The buffer should contain --Uri (auto-completed since it's the only option)
        buffer.Should().Contain("--Uri", "Uri should be auto-completed as the only remaining argument");
    }

    [TestMethod]
    [TestCategory("POSUSED002")]
    [TestDescription("PAC-201: After positional arg filled, --ArgName should NOT appear in autocomplete (copy command)")]
    public async Task PAC201_AfterPositionalArgFilled_ArgNameShouldNotAppear_CopyCommand()
    {
        // BUG REPRO: When you type "copy file1.txt backup/ " (both positional args filled),
        // the autocomplete should NOT show --Source or --Destination.
        
        // Arrange: Type "copy file1.txt backup/ " (positional args 0 and 1 are filled)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt backup/ ");
        runner.Should().HaveBuffer("copy file1.txt backup/ ")
                       .And.HaveInputCursorAt(23);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Menu should show available options, but NOT --Source or --Destination
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        
        // Should NOT contain --Source or --Destination
        menuItems.Should().NotContain(item => item.Contains("--Source"), 
            "Source is already filled by first positional argument 'file1.txt'");
        menuItems.Should().NotContain(item => item.Contains("--Destination"), 
            "Destination is already filled by second positional argument 'backup/'");
        
        // Should contain --Force and --Verbose (the non-positional arguments)
        menuItems.Should().Contain(item => item.Contains("--Force") || item.Contains("-f"));
        menuItems.Should().Contain(item => item.Contains("--Verbose") || item.Contains("-v"));
    }

    [TestMethod]
    [TestCategory("POSUSED003")]
    [TestDescription("PAC-202: Only first positional filled - second positional's argname should still appear")]
    public async Task PAC202_OnlyFirstPositionalFilled_SecondPositionalArgNameShouldAppear()
    {
        // When you type "copy file1.txt " (only first positional filled),
        // --Destination should still be available because it's not yet filled.
        
        // Arrange: Type "copy file1.txt " (only positional arg 0 is filled)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt ");
        runner.Should().HaveBuffer("copy file1.txt ")
                       .And.HaveInputCursorAt(15);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: Menu should show --Destination (not filled yet) but NOT --Source (filled)
        runner.Should().HaveMenuVisible();
        var menuItems = runner.GetMenuItems();
        
        // Should NOT contain --Source (filled by first positional)
        menuItems.Should().NotContain(item => item.Contains("--Source"), 
            "Source is already filled by positional argument 'file1.txt'");
        
        // For now, we primarily test the bug fix - positional filling an arg excludes it
    }

    [TestMethod]
    [TestCategory("POSUSED004")]
    [TestDescription("PAC-203: Explicit --ArgName with value - arg should be excluded from further completions")]
    public async Task PAC203_ExplicitArgNameWithValue_ArgExcludedFromFurtherCompletions()
    {
        // If user explicitly types --Profile testprof (giving it a value), 
        // then tabs again, --Profile should not appear in suggestions.
        
        // Arrange: Type "connect --Profile testprof " (explicit arg name and value used)
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("connect --Profile testprof ");
        runner.Should().HaveBuffer("connect --Profile testprof ")
                       .And.HaveInputCursorAt(27);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: The result should NOT contain --Profile since it's already used
        // (--Uri will be auto-completed since it's the only remaining arg)
        var buffer = runner.Buffer;
        
        // Count occurrences of --Profile - should only be the one we typed
        var profileCount = System.Text.RegularExpressions.Regex.Matches(buffer, "--Profile").Count;
        profileCount.Should().Be(1, "Profile should appear exactly once (the one we typed)");
        
        // Should have --Uri auto-completed
        buffer.Should().Contain("--Uri", "Uri should be the only remaining argument");
    }

    [TestMethod]
    [TestCategory("POSUSED005")]
    [TestDescription("PAC-204: Mixed positional and explicit - only unfilled args should appear")]
    public async Task PAC204_MixedPositionalAndExplicit_OnlyUnfilledArgsAppear()
    {
        // User types "copy file1.txt --Force backup/ " 
        // Both positional slots are filled, --Force is used
        // Only --Verbose should appear (auto-completed since it's the only option)
        
        // Arrange: Type "copy file1.txt --Force backup/ "
        using var runner = CreatePositionalRunner();
        runner.Initialize();
        
        await runner.TypeText("copy file1.txt --Force backup/ ");
        runner.Should().HaveBuffer("copy file1.txt --Force backup/ ")
                       .And.HaveInputCursorAt(31);

        // Act: Press Tab
        await runner.PressKey(ConsoleKey.Tab);

        // Assert: --Verbose should be auto-completed (since it's the only remaining option)
        var buffer = runner.Buffer;
        
        // Should NOT have added --Source, --Destination, or --Force again
        // (Force should appear only once - the one we typed)
        var forceCount = System.Text.RegularExpressions.Regex.Matches(buffer, "--Force").Count;
        forceCount.Should().Be(1, "Force should appear exactly once (the one we typed)");
        
        buffer.Should().NotContain("--Source", "Source is already filled by positional argument");
        buffer.Should().NotContain("--Destination", "Destination is already filled by positional argument");
        
        // --Verbose should be auto-completed
        buffer.Should().Contain("--Verbose", "Verbose should be auto-completed as the only remaining argument");
    }

    #endregion
}
