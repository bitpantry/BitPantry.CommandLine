using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Input;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.VirtualConsole;
using BitPantry.VirtualConsole.Testing;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for remote autocomplete ghost text behavior.
/// Reproduces the bug where remote commands with positional [FilePathAutoComplete]
/// arguments do not show ghost text when cursor enters the value position,
/// while local commands with the same setup work correctly.
/// </summary>
[TestClass]
public class IntegrationTests_RemoteGhostText
{
    #region Test Commands

    /// <summary>
    /// A simple autocomplete handler that returns fixed options.
    /// Avoids filesystem dependency so the test is deterministic.
    /// </summary>
    public class FixedPathHandler : IAutoCompleteHandler
    {
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            var query = context.QueryString ?? "";
            var all = new[] { @"docs\", @"src\", @"readme.txt" };
            var options = all
                .Where(v => v.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .Select(v => new AutoCompleteOption(v))
                .ToList();
            return Task.FromResult(options);
        }
    }

    public class FixedPathAttribute : AutoCompleteAttribute<FixedPathHandler> { }

    /// <summary>
    /// Group marker for the grouped remote command tests.
    /// </summary>
    [Group(Name = "rexplore")]
    [BitPantry.CommandLine.API.Description("Test remote explore group")]
    public class TestRemoteExploreGroup { }

    /// <summary>
    /// Server-side command with a positional argument decorated with a custom autocomplete handler.
    /// </summary>
    [Command(Name = "browse")]
    public class ServerBrowseCommand : CommandBase
    {
        [Argument(Name = "path", Position = 0)]
        [FixedPath]
        public string Path { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Server-side grouped command — same as ServerBrowseCommand but in "rexplore" group.
    /// Matches the real-world structure of RemoteExploreBrowseCommand.
    /// </summary>
    [InGroup<TestRemoteExploreGroup>]
    [Command(Name = "browse")]
    [BitPantry.CommandLine.API.Description("Browse with autocomplete in rexplore group")]
    public class GroupedServerBrowseCommand : CommandBase
    {
        [Argument(Name = "path", Position = 0)]
        [FixedPath]
        public string Path { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Local command with the exact same shape — same positional argument and handler.
    /// Used as a baseline to compare local vs remote behavior.
    /// </summary>
    [Command(Name = "localbrowse")]
    public class LocalBrowseCommand : CommandBase
    {
        [Argument(Name = "path", Position = 0)]
        [FixedPath]
        public string Path { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Diagnostic: Remote command data round-trip

    /// <summary>
    /// Given: Server registers a command with a positional [FixedPath] argument
    /// When: Client connects and receives the remote command
    /// Then: The client-side ArgumentInfo has IsPositional=true and Position=0
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RemoteCommand_AfterConnect_HasPositionalArgument()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        // Act — inspect the client's view of the remote command
        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var remoteCmd = clientRegistry.Commands.FirstOrDefault(c => c.Name == "browse" && c.IsRemote);

        // Assert
        remoteCmd.Should().NotBeNull("the remote command should be registered on the client");
        remoteCmd.Arguments.Should().NotBeEmpty("the command should have arguments");

        var pathArg = remoteCmd.Arguments.FirstOrDefault(a => a.Name == "path");
        pathArg.Should().NotBeNull("the 'path' argument should exist");
        pathArg.IsPositional.Should().BeTrue("Position=0 means IsPositional should be true");
        pathArg.Position.Should().Be(0);
    }

    #endregion

    #region Diagnostic: FindCommand for remote command

    /// <summary>
    /// Given: Client registry has remote "browse" command
    /// When: FindCommand("browse", null) is called
    /// Then: Returns the remote command
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RemoteCommand_FindCommand_FindsRootLevelRemoteCommand()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();

        // Act
        var found = clientRegistry.FindCommand("browse", null);

        // Assert
        found.Should().NotBeNull("FindCommand should locate the root-level remote command");
        found.IsRemote.Should().BeTrue();
        found.Name.Should().Be("browse");
    }

    #endregion

    #region Diagnostic: CursorContext for remote positional argument

    /// <summary>
    /// Given: Client registry contains a remote command with a positional argument
    /// When: CursorContextResolver resolves "browse " (trailing space, cursor at end)
    /// Then: Context type is PositionalValue with the correct TargetArgument
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RemoteCommand_CursorAfterCommand_ResolvesToPositionalValueContext()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var resolver = new CursorContextResolver(clientRegistry);

        // Act — resolve context for "browse " with cursor at end 
        // Use ResolveContext with 0-based buffer position (input.Length = past end)
        var input = "browse ";
        var context = resolver.ResolveContext(input, input.Length);

        // Assert
        context.Should().NotBeNull();
        context.ContextType.Should().Be(CursorContextType.PositionalValue,
            "cursor after a command with unfilled positional args should be PositionalValue");
        context.TargetArgument.Should().NotBeNull("the target argument should be the first positional arg");
        context.TargetArgument.Name.Should().Be("path");
        context.ResolvedCommand.Should().NotBeNull();
        context.ResolvedCommand.IsRemote.Should().BeTrue("browse is a remote command");
    }

    #endregion

    #region Diagnostic: Remote autocomplete returns options via client's ArgumentInfo

    /// <summary>
    /// Given: Client connected with remote "browse" command (positional [FixedPath] arg)
    /// When: AutoComplete is called using the CLIENT's deserialized ArgumentInfo (not server's)
    /// Then: Server correctly finds the handler and returns options
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RemoteAutoComplete_UsingClientArgumentInfo_ReturnsOptions()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();

        // Get the CLIENT's view of the remote command (deserialized from JSON)
        var remoteCmd = clientRegistry.Commands.First(c => c.Name == "browse" && c.IsRemote);
        var pathArg = remoteCmd.Arguments.First(a => a.Name == "path");

        // Build context from the CLIENT's deserialized objects — this is what happens in real usage
        var ctx = new AutoCompleteContext
        {
            QueryString = "",
            FullInput = "browse ",
            CursorPosition = 7,
            ArgumentInfo = pathArg,
            CommandInfo = remoteCmd,
            ProvidedValues = new Dictionary<ArgumentInfo, string>()
        };

        // Act
        var results = await proxy.AutoComplete("", "browse", ctx, CancellationToken.None);

        // Assert
        results.Should().NotBeNull("server should return autocomplete results");
        results.Should().HaveCount(3, "FixedPathHandler returns 3 options for empty query");
        results[0].Value.Should().Be(@"docs\");
        results[1].Value.Should().Be(@"src\");
        results[2].Value.Should().Be(@"readme.txt");
    }

    #endregion

    #region UX: Ghost text for remote positional argument

    /// <summary>
    /// Given: Client connected, local "localbrowse" command with positional [FixedPath] arg
    /// When: User types "localbrowse " (space after command)
    /// Then: Ghost text appears for the first autocomplete option
    /// 
    /// This is the baseline test — local commands should work.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task LocalCommand_PositionalArgWithHandler_ShowsGhostText()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureCommands(builder =>
            {
                builder.RegisterCommand<LocalBrowseCommand>();
            });
        });

        // Act — type the command and a space
        await env.Keyboard.TypeTextAsync("localbrowse ");

        // Assert — ghost text should appear (dim text for first option)
        var cursorRow = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();

        // FixedPathHandler returns: "docs\", "readme.txt", "src\" — first alphabetically is "docs\"
        lineText.Should().Contain(@"docs\",
            "ghost text should show the first autocomplete option from FixedPathHandler");
    }

    /// <summary>
    /// Given: Client connected to server with remote "browse" command (positional [FixedPath] arg)
    /// When: User types "browse " (space after remote command)
    /// Then: Ghost text appears for the first autocomplete option
    /// 
    /// This is the BUG REPRODUCTION test — remote commands should show ghost text
    /// just like local commands do.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RemoteCommand_PositionalArgWithHandler_ShowsGhostText()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        // Act — type the remote command and a space
        await env.Keyboard.TypeTextAsync("browse ");

        // Assert — ghost text should appear (dim text for first option)
        var cursorRow = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();

        // FixedPathHandler returns: "docs\", "readme.txt", "src\" — first alphabetically is "docs\"
        lineText.Should().Contain(@"docs\",
            "ghost text should show the first autocomplete option from FixedPathHandler for remote command");
    }

    /// <summary>
    /// Given: Client connected to server with remote "rexplore browse" grouped command (positional [FixedPath] arg)
    /// When: User types "rexplore browse " (space after grouped remote command)
    /// Then: Ghost text appears for the first autocomplete option
    /// 
    /// This tests the exact scenario from the bug report where ghost text doesn't appear
    /// for grouped remote commands.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task GroupedRemoteCommand_PositionalArgWithHandler_ShowsGhostText()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<GroupedServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        // Act — type the grouped remote command and a space
        await env.Keyboard.TypeTextAsync("rexplore browse ");

        // Assert — ghost text should appear (dim text for first option)
        var cursorRow = env.Console.VirtualConsole.CursorRow;
        var lineText = env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();

        // FixedPathHandler returns: "docs\", "src\", "readme.txt"
        lineText.Should().Contain(@"docs\",
            "ghost text should show the first autocomplete option from FixedPathHandler for grouped remote command");
    }

    /// <summary>
    /// Given: Client connected to server with remote "rexplore browse" grouped command
    /// When: CursorContextResolver resolves "rexplore browse " (trailing space, cursor at end)
    /// Then: Context type is PositionalValue for the grouped remote command
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task GroupedRemoteCommand_CursorAfterCommand_ResolvesToPositionalValueContext()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<GroupedServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var resolver = new CursorContextResolver(clientRegistry);

        // Act — resolve context for "rexplore browse " with cursor at end
        var input = "rexplore browse ";
        var context = resolver.ResolveContext(input, input.Length);

        // Assert
        context.Should().NotBeNull();
        context.ContextType.Should().Be(CursorContextType.PositionalValue,
            "cursor after a grouped command with unfilled positional args should be PositionalValue");
        context.TargetArgument.Should().NotBeNull("the target argument should be the positional arg");
        context.TargetArgument.Name.Should().Be("path");
        context.ResolvedCommand.Should().NotBeNull();
        context.ResolvedCommand.IsRemote.Should().BeTrue("rexplore browse is a remote command");
    }

    /// <summary>
    /// Given: Client connected with grouped remote "rexplore browse" command  
    /// When: AutoComplete is called using the CLIENT's deserialized ArgumentInfo for the grouped command
    /// Then: Server correctly finds the handler and returns options
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task GroupedRemoteAutoComplete_UsingClientArgumentInfo_ReturnsOptions()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<GroupedServerBrowseCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();

        // Get the CLIENT's view of the remote command (deserialized from JSON)
        var remoteCmd = clientRegistry.Commands.First(c => c.Name == "browse" && c.IsRemote);
        var pathArg = remoteCmd.Arguments.First(a => a.Name == "path");

        // Build context from the CLIENT's deserialized objects
        var ctx = new AutoCompleteContext
        {
            QueryString = "",
            FullInput = "rexplore browse ",
            CursorPosition = 16,
            ArgumentInfo = pathArg,
            CommandInfo = remoteCmd,
            ProvidedValues = new Dictionary<ArgumentInfo, string>()
        };

        // Act
        var results = await proxy.AutoComplete("rexplore", "browse", ctx, CancellationToken.None);

        // Assert
        results.Should().NotBeNull("server should return autocomplete results for grouped command");
        results.Should().HaveCount(3, "FixedPathHandler returns 3 options for empty query");
    }

    #endregion
}
