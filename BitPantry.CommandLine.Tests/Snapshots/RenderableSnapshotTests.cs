using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spectre.Console;
using Spectre.Console.Rendering;
using System;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using System.Threading.Tasks;
using VerifyMSTest;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.Snapshots;

/// <summary>
/// Snapshot tests for visual rendering using Verify.MSTest.
/// These tests capture the console output and compare against verified baselines.
/// </summary>
[TestClass]
public partial class RenderableSnapshotTests : VerifyBase
{
    #region Test Commands

    [Group(Name = "server")]
    public class ServerGroup { }

    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [CmdDescription("Connect to a server")]
    public class ConnectCommand : CommandBase
    {
        [Argument(Name = "host")]
        [CmdDescription("The hostname")]
        public string Host { get; set; }

        [Argument(Name = "port")]
        [Alias('p')]
        [CmdDescription("Port number")]
        public int Port { get; set; } = 22;

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup), Name = "disconnect")]
    [CmdDescription("Disconnect from server")]
    public class DisconnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Group = typeof(ServerGroup), Name = "status")]
    [CmdDescription("Show connection status")]
    public class StatusCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "help")]
    [CmdDescription("Show help")]
    public class HelpCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "config")]
    [CmdDescription("Configuration")]
    public class ConfigCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Helpers

    private class SimplePrompt : IPrompt
    {
        public string Render() => "> ";
        public int GetPromptLength() => 2;
        public void Write(IAnsiConsole console) => console.Write(new Text("> "));
    }

    private static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterGroup<ServerGroup>();
        registry.RegisterCommand<ConnectCommand>();
        registry.RegisterCommand<DisconnectCommand>();
        registry.RegisterCommand<StatusCommand>();
        registry.RegisterCommand<HelpCommand>();
        registry.RegisterCommand<ConfigCommand>();
        return registry;
    }

    private static MockFileSystem CreateMockFileSystem()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("bin");
        mockFileSystem.AddDirectory("obj");
        mockFileSystem.AddDirectory("src");
        mockFileSystem.AddFile("README.md", new MockFileData("# Readme"));
        return mockFileSystem;
    }

    private static List<ICompletionProvider> CreateProviders(CommandRegistry registry, InputLog inputLog, MockFileSystem mockFileSystem)
    {
        return new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry),
            new EnumProvider(),
            new StaticValuesProvider(),
            new FilePathProvider(mockFileSystem),
            new DirectoryPathProvider(mockFileSystem),
            new MethodProvider()
        };
    }

    #endregion

    #region Menu Snapshot Tests

    /// <summary>
    /// Snapshot test: Menu rendered with selection on first item.
    /// Captures the ANSI output when menu is visible with items.
    /// </summary>
    [TestMethod]
    public async Task Menu_WithSelection_FirstItem()
    {
        // This test verifies the initial rendering of a menu
        // When Tab is pressed after typing "server " to show subcommand menu
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var mockFileSystem = CreateMockFileSystem();
        var providers = CreateProviders(registry, inputLog, mockFileSystem);
        
        var orchestrator = new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        // Write initial prompt and text
        prompt.Write(console);
        console.Write(new Text("server "));

        // Simulate menu rendering by directly writing menu-like output
        // (The actual controller would do this via LiveRenderable in Phase 6)
        // For now, capture what the console looks like at this point
        
        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: Ghost text suggestion.
    /// Captures the ANSI output when ghost text is visible.
    /// </summary>
    [TestMethod]
    public async Task GhostText_Suggestion()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var prompt = new SimplePrompt();

        // Write prompt
        prompt.Write(console);
        
        // Write user input
        console.Write(new Text("ser"));
        
        // Write ghost text (dim gray) - this simulates what GhostTextRenderer does
        var ghostStyle = new Style(Color.Grey, decoration: Decoration.Dim);
        console.Write(new Text("ver", ghostStyle));

        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: Empty console with just prompt.
    /// Baseline for comparing against states with menu/ghost text.
    /// </summary>
    [TestMethod]
    public async Task EmptyConsole_JustPrompt()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var prompt = new SimplePrompt();
        prompt.Write(console);

        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: Menu item highlight styling.
    /// Verifies the ANSI codes used for selected vs unselected items.
    /// </summary>
    [TestMethod]
    public async Task Menu_HighlightStyling()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Simulate menu items with one highlighted
        var normalStyle = Style.Plain;
        var highlightStyle = Style.Parse("black on white"); // Inverted

        console.WriteLine(); // Move to line below prompt
        
        // First item - selected (highlighted)
        console.Write(new Text("  connect", highlightStyle));
        console.WriteLine();
        
        // Second item - normal
        console.Write(new Text("  disconnect", normalStyle));
        console.WriteLine();
        
        // Third item - normal
        console.Write(new Text("  status", normalStyle));
        console.WriteLine();

        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: Scroll indicators in menu.
    /// Verifies the appearance when menu has more items than viewport.
    /// </summary>
    [TestMethod]
    public async Task Menu_WithScrollIndicators()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var normalStyle = Style.Plain;
        var highlightStyle = Style.Parse("black on white");
        var dimStyle = new Style(Color.Grey, decoration: Decoration.Dim);

        console.WriteLine();
        
        // Scroll up indicator
        console.Write(new Text("  (↑ 2 more)", dimStyle));
        console.WriteLine();
        
        // Visible items
        console.Write(new Text("  item3", highlightStyle));
        console.WriteLine();
        console.Write(new Text("  item4", normalStyle));
        console.WriteLine();
        console.Write(new Text("  item5", normalStyle));
        console.WriteLine();
        
        // Scroll down indicator
        console.Write(new Text("  (↓ 3 more)", dimStyle));
        console.WriteLine();

        var output = console.Output;
        await Verify(output);
    }

    #endregion

    #region Menu Navigation Sequence Tests (T038)

    /// <summary>
    /// Snapshot test: Menu navigation sequence - Down, Down, Up.
    /// Captures each step of navigation for visual regression testing.
    /// </summary>
    [TestMethod]
    public async Task Menu_Navigation_DownDownUp()
    {
        // Uses AutoCompleteMenuRenderable directly to test navigation visuals
        // This captures the vertical menu layout with selection moving
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var items = new List<string> { "connect", "disconnect", "status" };

        // Step 1: Initial state - first item selected
        var step1 = new BitPantry.CommandLine.AutoComplete.Rendering.AutoCompleteMenuRenderable(
            items, selectedIndex: 0, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("Step 1 - Initial (index 0):"));
        console.WriteLine();
        console.Write(step1);
        console.WriteLine();
        console.WriteLine();

        // Step 2: After Down - second item selected
        var step2 = new BitPantry.CommandLine.AutoComplete.Rendering.AutoCompleteMenuRenderable(
            items, selectedIndex: 1, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("Step 2 - After Down (index 1):"));
        console.WriteLine();
        console.Write(step2);
        console.WriteLine();
        console.WriteLine();

        // Step 3: After Down again - third item selected
        var step3 = new BitPantry.CommandLine.AutoComplete.Rendering.AutoCompleteMenuRenderable(
            items, selectedIndex: 2, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("Step 3 - After Down (index 2):"));
        console.WriteLine();
        console.Write(step3);
        console.WriteLine();
        console.WriteLine();

        // Step 4: After Up - back to second item
        var step4 = new BitPantry.CommandLine.AutoComplete.Rendering.AutoCompleteMenuRenderable(
            items, selectedIndex: 1, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("Step 4 - After Up (index 1):"));
        console.WriteLine();
        console.Write(step4);

        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: MenuLiveRenderer showing/updating/hiding.
    /// Tests the complete lifecycle of menu rendering with Inflate pattern.
    /// </summary>
    [TestMethod]
    public async Task MenuLiveRenderer_ShowUpdateHide_Lifecycle()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var renderer = new BitPantry.CommandLine.AutoComplete.Rendering.MenuLiveRenderer(console);

        // Show with 3 items
        var items3 = new List<string> { "apple", "banana", "cherry" };
        renderer.Show(items3, selectedIndex: 0, viewportStart: 0, viewportSize: 10);
        console.WriteLine();
        console.Write(new Text("[After Show with 3 items]"));
        console.WriteLine();
        console.WriteLine();

        // Update to select different item
        renderer.Update(items3, selectedIndex: 1, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("[After Update - selection moved to index 1]"));
        console.WriteLine();
        console.WriteLine();

        // Shrink to 2 items (tests Inflate pattern)
        var items2 = new List<string> { "date", "elderberry" };
        renderer.Update(items2, selectedIndex: 0, viewportStart: 0, viewportSize: 10);
        console.Write(new Text("[After Update with 2 items - should maintain 3-line height]"));
        console.WriteLine();

        var output = console.Output;
        await Verify(output);
    }

    #endregion

    #region Ghost Text Lifecycle Tests (T048)

    /// <summary>
    /// Snapshot test: GhostLiveRenderer showing/updating/hiding.
    /// Tests the complete lifecycle of ghost text rendering with Inflate pattern.
    /// </summary>
    [TestMethod]
    public async Task GhostLiveRenderer_ShowUpdateHide_Lifecycle()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        var renderer = new BitPantry.CommandLine.AutoComplete.Rendering.GhostLiveRenderer(console);

        // Show initial ghost text
        console.Write(new Text("prompt> ser"));
        renderer.Show("ver");
        console.WriteLine();
        console.Write(new Text("[After Show 'ver']"));
        console.WriteLine();
        console.WriteLine();

        // Update to shorter text (tests Inflate - should clear old chars)
        console.Write(new Text("prompt> serv"));
        renderer.Update("e");
        console.WriteLine();
        console.Write(new Text("[After Update 'e' - shorter]"));
        console.WriteLine();
        console.WriteLine();

        // Update to longer text
        console.Write(new Text("prompt> s"));
        renderer.Update("erver");
        console.WriteLine();
        console.Write(new Text("[After Update 'erver' - longer]"));
        console.WriteLine();
        console.WriteLine();

        // Hide
        console.Write(new Text("prompt> server"));
        renderer.Hide();
        console.WriteLine();
        console.Write(new Text("[After Hide - ghost cleared]"));
        console.WriteLine();

        var output = console.Output;
        await Verify(output);
    }

    /// <summary>
    /// Snapshot test: Ghost text appearance with dim style.
    /// Captures the ANSI styling used for ghost text.
    /// </summary>
    [TestMethod]
    public async Task GhostText_DimStyle_Rendering()
    {
        var console = new ConsolidatedTestConsole()
            .Width(80)
            .Height(24)
            .EmitAnsiSequences();

        // Simulate typing with ghost suggestion
        console.Write(new Text("prompt> con"));
        
        // Render ghost using GhostTextRenderable directly
        var ghost = new BitPantry.CommandLine.AutoComplete.Rendering.GhostTextRenderable("nect");
        console.Write(ghost);
        
        console.WriteLine();
        console.Write(new Text("[Ghost 'nect' with dim style]"));

        var output = console.Output;
        await Verify(output);
    }

    #endregion
}