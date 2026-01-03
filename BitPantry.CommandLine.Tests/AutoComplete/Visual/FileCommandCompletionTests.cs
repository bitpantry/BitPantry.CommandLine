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
/// TDD tests for file path completion on file commands.
/// 
/// Tests verify that:
/// - FC-001: file upload Source argument shows local file paths
/// - FC-002: file cat Path argument shows file paths (for remote, will be sandboxed paths)
/// 
/// These tests are written BEFORE the fix to demonstrate the bug (TDD approach).
/// </summary>
[TestClass]
public class FileCommandCompletionTests
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
        _mockFileSystem.AddFile(@"C:\work\test.txt", new MockFileData("hello"));
        _mockFileSystem.Directory.SetCurrentDirectory(@"C:\work");
    }

    #region Test Commands - Mirror actual file commands

    /// <summary>
    /// File group for file management commands
    /// </summary>
    [Group(Name = "file")]
    [CmdDescription("File management")]
    public class FileGroup { }

    /// <summary>
    /// Mirrors FileUploadCommand from BitPantry.CommandLine.Remote.SignalR.Client
    /// The Source argument should provide LOCAL file path completion.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "upload")]
    [CmdDescription("Uploads a local file to the remote server")]
    public class FileUploadCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        [Alias('s')]
        [CmdDescription("The local file path to upload")]
        [FilePathCompletion]
        public string Source { get; set; } = string.Empty;

        [Argument(Position = 1)]
        [Alias('d')]
        [CmdDescription("The destination path on the remote server")]
        [FilePathCompletion]
        public string Destination { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Mirrors FileCatCommand from BitPantry.CommandLine.Remote.SignalR.Server
    /// The Path argument should provide file path completion (server-side paths).
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "cat")]
    [CmdDescription("Displays the contents of a file")]
    public class FileCatCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        [Alias('p')]
        [CmdDescription("The path of the file to display")]
        [FilePathCompletion]
        public string Path { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Mirrors FileDownloadCommand from BitPantry.CommandLine.Remote.SignalR.Client
    /// The Source (remote) and Destination (local) arguments should provide path completion.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "download")]
    [CmdDescription("Downloads a file from the remote server")]
    public class FileDownloadCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        [Alias('s')]
        [CmdDescription("The remote file path to download")]
        [FilePathCompletion]
        public string Source { get; set; } = string.Empty;

        [Argument(Position = 1)]
        [Alias('d')]
        [CmdDescription("The local destination path")]
        [FilePathCompletion]
        public string Destination { get; set; } = string.Empty;

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Infrastructure

    private StepwiseTestRunner CreateRunnerWithFileCommands()
    {
        var console = new ConsolidatedTestConsole().Interactive();
        var registry = CreateFileCommandRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        // Build service provider with IFileSystem registered for FilePathCompletionProvider
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(_mockFileSystem);
        var serviceProvider = services.BuildServiceProvider();

        // Include value completion providers (matching production setup)
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

    private static CommandRegistry CreateFileCommandRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterGroup<FileGroup>();
        registry.RegisterCommand<FileUploadCommand>();
        registry.RegisterCommand<FileCatCommand>();
        registry.RegisterCommand<FileDownloadCommand>();
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

    #region FC-001: file upload Source shows local file paths

    [TestMethod]
    [TestDescription("FC-001: Tab after 'file upload ' shows file path completions")]
    public async Task FileUpload_Tab_ShowsFilePathCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileCommands();
        runner.Initialize();

        await runner.TypeText("file upload ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}, HasGhostText: {runner.HasGhostText}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        // Should show file system completions
        runner.IsMenuVisible.Should().BeTrue("menu should appear for file path completion");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        menuItems.Should().Contain(i => i.Contains("bin") || i.Contains("obj") || 
                                        i.Contains("config.json") || i.Contains("README.md") || i.Contains("test.txt"),
            "should show files and directories from current directory");
    }

    [TestMethod]
    [TestDescription("FC-001a: Tab after 'file upload t' shows filtered file path completions")]
    public async Task FileUpload_TabWithPrefix_ShowsFilteredFilePathCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileCommands();
        runner.Initialize();

        await runner.TypeText("file upload t");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', IsMenuVisible: {runner.IsMenuVisible}");

        // Should auto-complete to test.txt (single match starting with 't')
        runner.Buffer.Should().Contain("test.txt", "should auto-complete to matching file");
    }

    #endregion

    #region FC-002: file cat Path shows file paths

    [TestMethod]
    [TestDescription("FC-002: Tab after 'file cat ' shows file path completions")]
    public async Task FileCat_Tab_ShowsFilePathCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileCommands();
        runner.Initialize();

        await runner.TypeText("file cat ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}, HasGhostText: {runner.HasGhostText}");
        if (runner.IsMenuVisible && runner.Controller.MenuItems != null)
        {
            Debug.WriteLine($"  MenuItems: {string.Join(", ", runner.Controller.MenuItems.Select(i => i.DisplayText))}");
        }

        // Should show file system completions
        runner.IsMenuVisible.Should().BeTrue("menu should appear for file path completion");
        
        var menuItems = runner.Controller.MenuItems?.Select(i => i.DisplayText).ToList() ?? new List<string>();
        menuItems.Should().Contain(i => i.Contains("bin") || i.Contains("obj") || 
                                        i.Contains("config.json") || i.Contains("README.md") || i.Contains("test.txt"),
            "should show files and directories from current directory");
    }

    [TestMethod]
    [TestDescription("FC-002a: Tab after 'file cat c' shows filtered file path completions")]
    public async Task FileCat_TabWithPrefix_ShowsFilteredFilePathCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileCommands();
        runner.Initialize();

        await runner.TypeText("file cat c");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}'");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - Buffer: '{runner.Buffer}', IsMenuVisible: {runner.IsMenuVisible}");

        // Should auto-complete to config.json (single match starting with 'c')
        runner.Buffer.Should().Contain("config.json", "should auto-complete to matching file");
    }

    #endregion

    #region FC-003: file download Source/Destination paths

    [TestMethod]
    [TestDescription("FC-003: Tab after 'file download ' shows file path completions")]
    public async Task FileDownload_Tab_ShowsFilePathCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithFileCommands();
        runner.Initialize();

        await runner.TypeText("file download ");

        Debug.WriteLine($"Setup - Buffer: '{runner.Buffer}', Position: {runner.BufferPosition}");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT
        Debug.WriteLine($"After Tab - IsMenuVisible: {runner.IsMenuVisible}, HasGhostText: {runner.HasGhostText}");

        // Should show file system completions for the Source argument
        runner.IsMenuVisible.Should().BeTrue("menu should appear for file path completion");
    }

    #endregion
}
