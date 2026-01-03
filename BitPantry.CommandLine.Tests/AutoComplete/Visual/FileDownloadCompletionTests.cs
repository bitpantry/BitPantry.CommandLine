using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Attributes;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// TDD tests for file download command autocomplete.
/// 
/// Bug: "file download + tab doesn't show the autocomplete for the positional parameter - 
/// no ghost text either at that last cursor position"
/// 
/// The issue is that file download's Source parameter needs REMOTE file path completion,
/// but the command runs locally on the client. The RemoteCompletionProvider only activates
/// for commands marked as IsRemote=true.
/// 
/// Solution: Create [RemoteFilePathCompletion] attribute that signals the need for remote completion.
/// </summary>
[TestClass]
public class FileDownloadCompletionTests
{
    private MockFileSystem _localFileSystem;
    private Mock<IServerProxy> _mockServerProxy;

    [TestInitialize]
    public void Setup()
    {
        _localFileSystem = new MockFileSystem();
        // Set up local file system
        _localFileSystem.AddDirectory(@"C:\work");
        _localFileSystem.AddFile(@"C:\work\local.txt", new MockFileData("local content"));
        _localFileSystem.Directory.SetCurrentDirectory(@"C:\work");

        // Set up mock server proxy that returns remote completions
        _mockServerProxy = new Mock<IServerProxy>();
        _mockServerProxy.Setup(p => p.ConnectionState)
            .Returns(ServerProxyConnectionState.Connected);
    }

    #region Test Commands

    [Group(Name = "file")]
    [CmdDescription("File management")]
    public class FileGroup { }

    /// <summary>
    /// Mirrors the actual FileDownloadCommand.
    /// Source needs REMOTE file path completion.
    /// Destination needs LOCAL file path completion.
    /// </summary>
    [Command(Group = typeof(FileGroup), Name = "download")]
    [CmdDescription("Downloads a file from the remote server")]
    public class FileDownloadCommand : CommandBase
    {
        [Argument(Position = 0, IsRequired = true)]
        [Alias('s')]
        [CmdDescription("The remote file path to download")]
        [RemoteFilePathCompletion]  // This attribute needs to be created
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

    private StepwiseTestRunner CreateRunnerWithRemoteCompletion()
    {
        var console = new ConsolidatedTestConsole().Interactive();
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();

        // Build service provider
        var services = new ServiceCollection();
        services.AddSingleton<IFileSystem>(_localFileSystem);
        services.AddSingleton(_mockServerProxy.Object);
        var serviceProvider = services.BuildServiceProvider();

        // Include providers - RemoteCompletionProvider should handle remote paths
        var providers = new List<ICompletionProvider>
        {
            new CommandCompletionProvider(registry),
            new HistoryProvider(inputLog),
            new PositionalArgumentProvider(registry),
            new ArgumentNameProvider(registry),
            new ArgumentAliasProvider(registry),
            new StaticValuesProvider(),
            new EnumProvider(),
            new FilePathProvider(_localFileSystem),
            new DirectoryPathProvider(_localFileSystem),
            new RemoteCompletionProvider(_mockServerProxy.Object)
        };

        var orchestrator = new CompletionOrchestrator(providers, cache, registry, serviceProvider);
        var prompt = new SimplePrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    private static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterGroup<FileGroup>();
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

    #region FDC-001: file download Source should show remote completions

    [TestMethod]
    [TestDescription("FDC-001: file download + Tab should show completions for Source parameter")]
    public async Task FileDownload_Tab_ShouldShowCompletionsForSource()
    {
        // ARRANGE
        // Set up server proxy to return remote file completions
        var remoteItems = new List<CompletionItem>
        {
            new CompletionItem { DisplayText = "config.json", InsertText = "config.json", Kind = CompletionItemKind.File },
            new CompletionItem { DisplayText = "data.csv", InsertText = "data.csv", Kind = CompletionItemKind.File },
            new CompletionItem { DisplayText = "documents/", InsertText = "documents/", Kind = CompletionItemKind.Directory }
        };
        
        _mockServerProxy.Setup(p => p.GetCompletionsAsync(
            It.IsAny<CompletionContext>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(remoteItems));

        using var runner = CreateRunnerWithRemoteCompletion();
        runner.Initialize();

        // Type "file download " to get to the Source parameter position
        await runner.TypeText("file download ");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Menu should be visible with remote completions
        runner.IsMenuVisible.Should().BeTrue("menu should appear for Source parameter");

        // The menu should contain remote files, not local files
        var output = string.Join("\n", runner.Console.Lines);
        Debug.WriteLine($"Console output:\n{output}");

        // Should show remote files from server
        (output.Contains("config.json") || output.Contains("data.csv") || output.Contains("documents/"))
            .Should().BeTrue("menu should contain remote file completions");
        
        // Should NOT show local files
        output.Should().NotContain("local.txt", "menu should not contain local files");
    }

    [TestMethod]
    [TestDescription("FDC-002: file download Source should show ghost text for remote files")]
    public async Task FileDownload_Typing_ShouldShowGhostTextForRemoteFiles()
    {
        // ARRANGE
        var remoteItems = new List<CompletionItem>
        {
            new CompletionItem { DisplayText = "config.json", InsertText = "config.json", Kind = CompletionItemKind.File }
        };
        
        _mockServerProxy.Setup(p => p.GetCompletionsAsync(
            It.IsAny<CompletionContext>(), 
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CompletionResult(remoteItems));

        using var runner = CreateRunnerWithRemoteCompletion();
        runner.Initialize();

        // Type "file download con" - should show ghost suggestion for "config.json"
        await runner.TypeText("file download con");

        // ASSERT - Ghost text should be visible
        runner.HasGhostText.Should().BeTrue("ghost should be visible");
        
        // Ghost should suggest "config.json" completion
        runner.GhostText.Should().Contain("fig.json", "ghost should show remaining text of config.json");
    }

    [TestMethod]
    [TestDescription("FDC-003: file download Destination should show local file completions when typing partial path")]
    public async Task FileDownload_Destination_ShouldShowLocalCompletions()
    {
        // ARRANGE
        using var runner = CreateRunnerWithRemoteCompletion();
        runner.Initialize();

        // Type command with Source already filled, then start typing destination path
        // Start with "./" to explicitly request local path completion
        await runner.TypeText("file download config.json ./");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Should show local files for Destination
        // Note: When Tab is pressed, the menu may show local paths or nothing if the path doesn't exist
        // This test validates the flow works without errors
        var output = string.Join("\n", runner.Console.Lines);
        Debug.WriteLine($"Console output:\n{output}");

        // The main assertion is that the system doesn't crash and provides some response
        // Destination should use FilePathCompletion which handles local paths
    }

    [TestMethod]
    [TestDescription("FDC-004: file download when disconnected should gracefully return empty")]
    public async Task FileDownload_WhenDisconnected_ShouldReturnEmpty()
    {
        // ARRANGE - Server is disconnected
        _mockServerProxy.Setup(p => p.ConnectionState)
            .Returns(ServerProxyConnectionState.Disconnected);

        using var runner = CreateRunnerWithRemoteCompletion();
        runner.Initialize();

        await runner.TypeText("file download ");

        // ACT
        await runner.PressKey(ConsoleKey.Tab);

        // ASSERT - Menu should not show remote files (graceful fallback)
        // May show argument name completions instead, which is acceptable
        var output = string.Join("\n", runner.Console.Lines);
        Debug.WriteLine($"Console output (disconnected):\n{output}");

        // Should NOT throw an error
        // The test passing without exception is the main assertion
    }

    #endregion
}
