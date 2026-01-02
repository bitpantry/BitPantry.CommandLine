using BitPantry.CommandLine.AutoComplete;
using Microsoft.Extensions.DependencyInjection;
using BitPantry.CommandLine.AutoComplete.Cache;
using BitPantry.CommandLine.AutoComplete.Providers;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.VirtualConsole;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections.Generic;
using System.IO.Abstractions.TestingHelpers;
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;

namespace BitPantry.CommandLine.Tests.AutoComplete.Visual;

/// <summary>
/// Base class for Visual UX tests using StepwiseTestRunner.
/// 
/// Provides shared test commands, registry creation, and runner factory methods.
/// All visual tests should inherit from this class to ensure consistent test setup.
/// </summary>
public abstract class VisualTestBase
{
    #region Test Commands

    /// <summary>
    /// Server group for testing hierarchical command structures.
    /// </summary>
    [Group(Name = "server")]
    [CmdDescription("Server management")]
    public class ServerGroup
    {
        /// <summary>
        /// Nested profile group for testing deep hierarchies.
        /// </summary>
        [Group(Name = "profile")]
        [CmdDescription("Profile management")]
        public class ProfileGroup { }
    }

    /// <summary>
    /// Connect command with arguments for testing argument completion.
    /// Mirrors the real SignalR ConnectCommand structure with multiple args including Option flags.
    /// </summary>
    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [CmdDescription("Connect to server")]
    public class ConnectCommand : CommandBase
    {
        [Argument(Name = "host")]
        [CmdDescription("Hostname")]
        public string Host { get; set; }

        [Argument(Name = "port")]
        [Alias('p')]
        [CmdDescription("Port number")]
        public int Port { get; set; } = 22;

        [Argument]
        [Alias('k')]
        [CmdDescription("API key for authentication")]
        public string ApiKey { get; set; }

        [Argument]
        [Alias('d')]
        [CmdDescription("Confirm disconnect if already connected")]
        public Option ConfirmDisconnect { get; set; }

        [Argument]
        [Alias('u')]
        [CmdDescription("URI endpoint")]
        public string Uri { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Simple disconnect command for testing basic completion.
    /// </summary>
    [Command(Group = typeof(ServerGroup), Name = "disconnect")]
    [CmdDescription("Disconnect from server")]
    public class DisconnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Status command for testing completion ordering.
    /// </summary>
    [Command(Group = typeof(ServerGroup), Name = "status")]
    [CmdDescription("Show status")]
    public class StatusCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Profile add command for testing nested group completion.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "add")]
    [CmdDescription("Add a profile")]
    public class ProfileAddCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Profile remove command for testing nested group completion.
    /// </summary>
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "remove")]
    [CmdDescription("Remove a profile")]
    public class ProfileRemoveCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Root-level help command for testing root completions.
    /// </summary>
    [Command(Name = "help")]
    [CmdDescription("Show help")]
    public class HelpCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Config command for testing multiple matches (config, connect).
    /// </summary>
    [Command(Name = "config")]
    [CmdDescription("Configuration settings")]
    public class ConfigCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Prompts

    /// <summary>
    /// Simple prompt for basic tests.
    /// </summary>
    protected class SimplePrompt : IPrompt
    {
        public string Render() => "> ";
        public int GetPromptLength() => 2;
        public void Write(IAnsiConsole console)
        {
            console.Write(new Text("> "));
        }
    }

    /// <summary>
    /// Realistic prompt matching the sandbox: "[purple]sandbox[/] [grey]$[/] "
    /// This includes ANSI markup codes that affect cursor positioning.
    /// </summary>
    protected class RealisticPrompt : IPrompt
    {
        // The markup: "[purple]sandbox[/] [grey]$[/] "
        // Visual output: "sandbox $ " (10 characters)
        public string Render() => "[purple]sandbox[/] [grey]$[/] ";
        public int GetPromptLength() => Render().GetTerminalDisplayLength();
        public void Write(IAnsiConsole console)
        {
            console.Markup(Render());
        }
    }

    #endregion

    #region Registry and Runner Creation

    /// <summary>
    /// Creates a command registry with all test commands.
    /// </summary>
    protected static CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.ReplaceDuplicateCommands = true;
        registry.RegisterGroup<ServerGroup>();
        registry.RegisterGroup<ServerGroup.ProfileGroup>();
        registry.RegisterCommand<ConnectCommand>();
        registry.RegisterCommand<DisconnectCommand>();
        registry.RegisterCommand<StatusCommand>();
        registry.RegisterCommand<ProfileAddCommand>();
        registry.RegisterCommand<ProfileRemoveCommand>();
        registry.RegisterCommand<HelpCommand>();
        registry.RegisterCommand<ConfigCommand>();
        return registry;
    }

    /// <summary>
    /// Creates a mock file system with sample directories and files for path completion testing.
    /// </summary>
    protected static MockFileSystem CreateMockFileSystem()
    {
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddDirectory("bin");
        mockFileSystem.AddDirectory("obj");
        mockFileSystem.AddDirectory("src");
        mockFileSystem.AddFile("README.md", new MockFileData("# Readme"));
        mockFileSystem.AddFile("config.json", new MockFileData("{}"));
        return mockFileSystem;
    }

    /// <summary>
    /// Creates all production providers to match real environment behavior.
    /// </summary>
    protected static List<ICompletionProvider> CreateProviders(
        CommandRegistry registry, 
        InputLog inputLog, 
        MockFileSystem mockFileSystem)
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

    /// <summary>
    /// Creates a StepwiseTestRunner with the realistic prompt.
    /// This is the primary factory method for visual tests.
    /// </summary>
    protected static StepwiseTestRunner CreateRunner()
    {
        return CreateRunner(new RealisticPrompt());
    }

    /// <summary>
    /// Creates a StepwiseTestRunner with a custom prompt.
    /// Uses all production providers to match real environment behavior.
    /// </summary>
    protected static StepwiseTestRunner CreateRunner(IPrompt prompt)
    {
        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var mockFileSystem = CreateMockFileSystem();
        var providers = CreateProviders(registry, inputLog, mockFileSystem);
        
        var orchestrator = new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    /// <summary>
    /// Creates a StepwiseTestRunner with default pre-populated history.
    /// </summary>
    protected static StepwiseTestRunner CreateRunnerWithHistory()
    {
        return CreateRunnerWithHistory("server connect", "server disconnect", "help");
    }

    /// <summary>
    /// Creates a StepwiseTestRunner with pre-populated history.
    /// Uses all production providers to match real environment behavior.
    /// </summary>
    protected static StepwiseTestRunner CreateRunnerWithHistory(params string[] historyEntries)
    {
        var console = new VirtualAnsiConsole().Interactive();
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        
        // Add history entries
        foreach (var entry in historyEntries)
        {
            inputLog.Add(entry);
        }
        
        var cache = new CompletionCache();
        var mockFileSystem = CreateMockFileSystem();
        var providers = CreateProviders(registry, inputLog, mockFileSystem);
        
        var orchestrator = new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
        var prompt = new RealisticPrompt();
        var controller = new AutoCompleteController(orchestrator, console, prompt);

        return new StepwiseTestRunner(console, prompt, controller, inputLog);
    }

    /// <summary>
    /// Creates an orchestrator directly without the StepwiseTestRunner wrapper.
    /// Useful for lower-level tests that need to test orchestrator behavior directly.
    /// </summary>
    protected static ICompletionOrchestrator CreateOrchestrator()
    {
        var registry = CreateRegistry();
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var mockFileSystem = CreateMockFileSystem();
        var providers = CreateProviders(registry, inputLog, mockFileSystem);
        
        return new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
    }

    /// <summary>
    /// Creates an orchestrator with a custom registry.
    /// Useful for tests that need specific command configurations.
    /// </summary>
    protected static ICompletionOrchestrator CreateOrchestrator(CommandRegistry registry)
    {
        var inputLog = new InputLog();
        var cache = new CompletionCache();
        var mockFileSystem = CreateMockFileSystem();
        var providers = CreateProviders(registry, inputLog, mockFileSystem);
        
        return new CompletionOrchestrator(providers, cache, registry, new ServiceCollection().BuildServiceProvider());
    }

    #endregion
}
