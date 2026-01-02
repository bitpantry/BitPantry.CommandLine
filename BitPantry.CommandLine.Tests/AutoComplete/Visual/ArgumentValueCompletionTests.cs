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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Tests for argument VALUE completion (not argument names).
/// 
/// These tests verify the UX behavior when completing argument values:
/// - Static values from [Completion("a", "b")]
/// - Enum values
/// - File/directory path completion
/// - Fallback behavior when custom provider returns empty
/// 
/// This addresses the gap where argument NAME completion was well-tested
/// but argument VALUE completion lacked UX/visual tests.
/// </summary>
[TestClass]
public class ArgumentValueCompletionTests
{
    private MockFileSystem _mockFileSystem;

    [TestInitialize]
    public void Setup()
    {
        _mockFileSystem = new MockFileSystem();
        // Set up a realistic file system
        _mockFileSystem.AddDirectory(@"C:\work");
        _mockFileSystem.AddDirectory(@"C:\work\bin");
        _mockFileSystem.AddDirectory(@"C:\work\obj");
        _mockFileSystem.AddFile(@"C:\work\config.json", new MockFileData("{}"));
        _mockFileSystem.AddFile(@"C:\work\README.md", new MockFileData("# README"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");
    }

    #region Test Commands

    /// <summary>
    /// Command with static value completion.
    /// </summary>
    [Command(Name = "deploy")]
    [CmdDescription("Deploy to environment")]
    public class DeployCommand : CommandBase
    {
        [Argument]
        [CmdDescription("Target environment")]
        [Completion(new[] { "dev", "staging", "prod" })]
        public string Environment { get; set; }

        [Argument]
        [CmdDescription("Deployment region")]
        [Completion(new[] { "us-east", "us-west", "eu-central" })]
        public string Region { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with enum argument.
    /// </summary>
    [Command(Name = "format")]
    [CmdDescription("Format output")]
    public class FormatCommand : CommandBase
    {
        [Argument]
        [CmdDescription("Output format")]
        public OutputFormat Format { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    public enum OutputFormat
    {
        Json,
        Xml,
        Csv,
        Text
    }

    /// <summary>
    /// Command with file path argument.
    /// </summary>
    [Command(Name = "load")]
    [CmdDescription("Load a file")]
    public class LoadCommand : CommandBase
    {
        [Argument]
        [CmdDescription("File to load")]
        [FilePathCompletion]
        public string Path { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with NO completion attribute on string argument.
    /// Tests fallback behavior to file system completion.
    /// </summary>
    [Command(Name = "open")]
    [CmdDescription("Open something")]
    public class OpenCommand : CommandBase
    {
        [Argument]
        [CmdDescription("Target to open (no completion attribute)")]
        public string Target { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Infrastructure

    private StepwiseTestRunner CreateRunnerWithValueProviders()
    {
        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        // Include value completion providers
        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry),
            new StaticValuesProvider(),     // For [Completion("a", "b")]
            new EnumProvider(),              // For enum types
            new FilePathProvider(_mockFileSystem),      // For file paths
            new DirectoryPathProvider(_mockFileSystem)  // For directory paths
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    private static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterCommand<DeployCommand>();
        registry.RegisterCommand<FormatCommand>();
        registry.RegisterCommand<LoadCommand>();
        registry.RegisterCommand<OpenCommand>();
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

    #region SV-001: Static values menu appears

    [TestMethod]
    [TestDescription("SV-001: Tab after 'deploy --Environment ' shows static values menu")]
    public async Task Tab_AfterStaticValueArg_ShowsStaticValuesMenu()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        await runner.TypeText("deploy --Environment ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        runner.IsMenuVisible.Should().BeTrue("menu should appear for static value completion");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        menuItems.Should().Contain("dev");
        menuItems.Should().Contain("staging");
        menuItems.Should().Contain("prod");
    }

    #endregion

    #region Single Match Auto-Accept (related to MC-012)

    [TestMethod]
    [TestDescription("Single matching value on initial Tab auto-accepts without menu")]
    public async Task Tab_WithSingleMatchingValue_AutoAccepts()
    {
        // Conflict #3 Resolution: This tests the "initial Tab" scenario.
        // When user presses Tab and only ONE result matches, we auto-insert.
        // This is consistent with MC-012 behavior for commands.
        // Note: This differs from "filtering to single" which keeps menu open.
        
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        await runner.TypeText("deploy --Environment st");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Single match (staging) should be auto-accepted
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', IsMenuVisible: {runner.IsMenuVisible}");

        // Menu should NOT be visible (single match auto-accepts)
        runner.IsMenuVisible.Should().BeFalse("single match should auto-accept without showing menu");
        
        // Buffer should contain the completed value
        runner.Buffer.Should().Contain("staging", "single matching value should be auto-inserted");
    }

    #endregion

    #region EN-001: Enum values menu appears

    [TestMethod]
    [TestDescription("EN-001: Tab after enum argument shows enum values")]
    public async Task Tab_AfterEnumArg_ShowsEnumValuesMenu()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        await runner.TypeText("format --Format ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        runner.IsMenuVisible.Should().BeTrue("menu should appear for enum completion");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        menuItems.Should().Contain("Json");
        menuItems.Should().Contain("Xml");
        menuItems.Should().Contain("Csv");
        menuItems.Should().Contain("Text");
    }

    #endregion

    #region FP-001: File path completion shows files and directories

    [TestMethod]
    [TestDescription("FP-001: Tab after file path argument shows file system")]
    public async Task Tab_AfterFilePathArg_ShowsFileSystem()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        await runner.TypeText("load --Path ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        runner.IsMenuVisible.Should().BeTrue("menu should appear for file path completion");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        // Should contain files and directories from mock file system
        menuItems.Should().Contain(i => i.Contains("bin") || i.Contains("obj") || 
                                        i.Contains("config.json") || i.Contains("README.md"),
            "should show files and directories from current directory");
    }

    #endregion

    #region FB-001: Fallback to file system when no completion attribute

    /// <summary>
    /// This test documents the CURRENT behavior where FilePathProvider acts as
    /// a fallback for any ArgumentValue without a specific completion attribute.
    /// 
    /// This reproduces the scenario reported: typing "server connect --Profile "
    /// and pressing Tab shows file system completions because ProfileNameProvider
    /// returns empty and FilePathProvider kicks in as fallback.
    /// 
    /// This may be intentional (like Windows Terminal behavior) or a bug.
    /// </summary>
    [TestMethod]
    [TestDescription("FB-001: Tab after unattributed argument shows file system (fallback behavior)")]
    public async Task Tab_AfterUnattributedArg_ShowsFileSystemFallback()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        // "open --Target " has no [Completion] attribute on Target property
        await runner.TypeText("open --Target ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Current behavior: file system completions appear as fallback
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        runner.IsMenuVisible.Should().BeTrue("menu should appear with file system fallback");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        // File system items should appear
        menuItems.Should().Contain(i => i.Contains("bin") || i.Contains("obj") || 
                                        i.Contains("config.json") || i.Contains("README.md"),
            "file system completions should appear as fallback when no completion attribute");
    }

    #endregion

    #region ACC-001: Accept value from menu

    [TestMethod]
    [TestDescription("ACC-001: Enter accepts selected value from menu")]
    public async Task Enter_AcceptsSelectedValue()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        await runner.TypeText("deploy --Environment ");
        await runner.PressKey(ConsoleKey.Tab);  // Opens menu with dev, staging, prod

        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', Selected: '{runner.SelectedMenuItem}'");

        // Navigate to "staging" (if not already first)
        await runner.PressKey(ConsoleKey.DownArrow);  // Move selection

        Debug.WriteLine($"After DownArrow - Selected: '{runner.SelectedMenuItem}'");

        // ACT
        await runner.PressKey(ConsoleKey.Enter);

        // ASSERT
        Debug.WriteLine($"After Enter - Buffer: '{runner.Buffer}'");
        // Check that one of the values was inserted
        var hasValue = runner.Buffer.Contains("dev") || 
                       runner.Buffer.Contains("staging") || 
                       runner.Buffer.Contains("prod");
        hasValue.Should().BeTrue("selected value should be inserted into buffer");
    }

    #endregion

    #region MV-001: Multiple value arguments work independently

    [TestMethod]
    [TestDescription("MV-001: Second value argument gets its own completions")]
    public async Task Tab_AfterSecondValueArg_ShowsCorrectCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithValueProviders();
        runner.Initialize();

        // First argument value already specified
        await runner.TypeText("deploy --Environment dev --Region ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - should show Region completions, not Environment completions
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        runner.IsMenuVisible.Should().BeTrue();
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        menuItems.Should().Contain("us-east");
        menuItems.Should().Contain("us-west");
        menuItems.Should().Contain("eu-central");
        // Should NOT contain Environment values
        menuItems.Should().NotContain("dev");
        menuItems.Should().NotContain("staging");
        menuItems.Should().NotContain("prod");
    }

    #endregion
}
