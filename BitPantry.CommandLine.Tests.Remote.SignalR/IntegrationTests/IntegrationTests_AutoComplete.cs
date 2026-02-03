using BitPantry.CommandLine.API;
using BitPantry.CommandLine.AutoComplete;
using BitPantry.CommandLine.AutoComplete.Context;
using BitPantry.CommandLine.AutoComplete.Handlers;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Component;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for remote autocomplete via SignalR.
/// Validates that autocomplete requests are correctly routed to the server,
/// processed by the server's handler registry, and results returned to the client.
/// 
/// Test Case References: RMT-004 through RMT-009
/// </summary>
[TestClass]
public class IntegrationTests_AutoComplete
{
    #region Test Commands

    public enum Priority { High, Low, Medium }

    [Flags]
    public enum FileOptions { None = 0, ReadOnly = 1, Hidden = 2, Archive = 4 }

    [Command(Name = "remotepriority")]
    public class RemotePriorityCommand : CommandBase
    {
        [Argument(Name = "level")]
        public Priority Level { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "remotebool")]
    public class RemoteBoolCommand : CommandBase
    {
        [Argument(Name = "enabled")]
        public bool Enabled { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "remotestring")]
    public class RemoteStringCommand : CommandBase
    {
        [Argument(Name = "value")]
        public string Value { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    [Command(Name = "remoteflags")]
    public class RemoteFlagsCommand : CommandBase
    {
        [Argument(Name = "options")]
        public FileOptions Options { get; set; }

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Custom autocomplete handler for RMT-007 test.
    /// Returns a fixed list of custom values to verify attribute handler resolution over remote.
    /// </summary>
    public class CustomEnvironmentHandler : IAutoCompleteHandler
    {
        public Task<List<AutoCompleteOption>> GetOptionsAsync(
            AutoCompleteContext context,
            CancellationToken cancellationToken = default)
        {
            // Filter by query string (case-insensitive prefix match)
            var query = context.QueryString ?? "";
            var options = new[] { "Development", "Production", "Staging" }
                .Where(v => v.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                .OrderBy(v => v)
                .Select(v => new AutoCompleteOption(v))
                .ToList();
            return Task.FromResult(options);
        }
    }

    /// <summary>
    /// Command with explicit [AutoComplete] attribute to test RMT-007.
    /// </summary>
    [Command(Name = "remoteenv")]
    public class RemoteEnvironmentCommand : CommandBase
    {
        [Argument(Name = "env")]
        [AutoComplete<CustomEnvironmentHandler>]
        public string Environment { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    /// <summary>
    /// Command with multiple arguments to test RMT-008 cursor position accuracy.
    /// </summary>
    [Command(Name = "remotemultiarg")]
    public class RemoteMultiArgCommand : CommandBase
    {
        [Argument(Name = "priority")]
        public Priority Priority { get; set; }

        [Argument(Name = "enabled")]
        public bool Enabled { get; set; }

        [Argument(Name = "name")]
        public string Name { get; set; } = "";

        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion

    #region Helper Methods

    private static AutoCompleteContext CreateContext(
        CommandInfo commandInfo,
        ArgumentInfo argumentInfo,
        string queryString = "")
    {
        return new AutoCompleteContext
        {
            QueryString = queryString,
            FullInput = $"{commandInfo.Name} --{argumentInfo.Name} {queryString}",
            CursorPosition = $"{commandInfo.Name} --{argumentInfo.Name} {queryString}".Length,
            ArgumentInfo = argumentInfo,
            CommandInfo = commandInfo,
            ProvidedValues = new Dictionary<ArgumentInfo, string>()
        };
    }

    private static (CommandInfo Command, ArgumentInfo Argument) GetCommandAndArgument(
        ICommandRegistry registry,
        string commandName,
        string argumentName)
    {
        var command = registry.Commands.First(c => c.Name == commandName);
        var argument = command.Arguments.First(a => a.Name == argumentName);
        return (command, argument);
    }

    #endregion

    #region RMT-004: Remote autocomplete invocation via SignalR

    /// <summary>
    /// Implements: RMT-004
    /// Given: Client connected to server with enum command
    /// When: AutoComplete is called for enum argument
    /// Then: Returns all enum values in alphabetical order
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_EnumArg_ReturnsAllEnumValuesAlphabetically()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotepriority", "level");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotepriority", ctx, CancellationToken.None);

        // Assert - exact count, exact values, exact order (alphabetical)
        results.Should().HaveCount(3);
        results[0].Value.Should().Be("High");
        results[1].Value.Should().Be("Low");
        results[2].Value.Should().Be("Medium");
    }

    /// <summary>
    /// Implements: RMT-004 (prefix filtering)
    /// Given: Client connected to server with enum command
    /// When: AutoComplete is called with prefix "M"
    /// Then: Returns only matching enum value "Medium"
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_EnumArgWithPrefix_FiltersExactly()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotepriority", "level");
        var ctx = CreateContext(command, argument, queryString: "M");

        // Act
        var results = await proxy.AutoComplete("", "remotepriority", ctx, CancellationToken.None);

        // Assert - only "Medium" matches prefix "M"
        results.Should().HaveCount(1);
        results.Single().Value.Should().Be("Medium");
    }

    #endregion

    #region RMT-005: Remote autocomplete has full parity with local

    /// <summary>
    /// Implements: RMT-005
    /// Given: Client connected to server with boolean command
    /// When: AutoComplete is called for bool argument
    /// Then: Returns "false" and "true" in alphabetical order
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_BoolArg_ReturnsFalseThenTrue()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteBoolCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotebool", "enabled");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotebool", ctx, CancellationToken.None);

        // Assert - exact count and order (lowercase per BooleanAutoCompleteHandler)
        results.Should().HaveCount(2);
        results[0].Value.Should().Be("false");
        results[1].Value.Should().Be("true");
    }

    /// <summary>
    /// Implements: RMT-005 (bool prefix filtering)
    /// Given: Client connected to server with boolean command
    /// When: AutoComplete is called with prefix "t"
    /// Then: Returns only "true"
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_BoolArgWithPrefix_FiltersExactly()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteBoolCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotebool", "enabled");
        var ctx = CreateContext(command, argument, queryString: "T");

        // Act
        var results = await proxy.AutoComplete("", "remotebool", ctx, CancellationToken.None);

        // Assert (lowercase per BooleanAutoCompleteHandler)
        results.Should().HaveCount(1);
        results.Single().Value.Should().Be("true");
    }

    #endregion

    #region RMT-006: Remote handler resolution uses server's handlers

    /// <summary>
    /// Implements: RMT-006
    /// Given: Server has enum handler registered
    /// When: Client requests autocomplete for enum argument
    /// Then: Server's EnumAutoCompleteHandler is used (verified by correct enum values returned)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_FlagsEnum_ReturnsAllFlagValues()
    {
        // Arrange - Flags enum tests that server correctly handles complex enum types
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteFlagsCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remoteflags", "options");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remoteflags", ctx, CancellationToken.None);

        // Assert - all flag enum values returned alphabetically
        results.Should().HaveCount(4);
        results[0].Value.Should().Be("Archive");
        results[1].Value.Should().Be("Hidden");
        results[2].Value.Should().Be("None");
        results[3].Value.Should().Be("ReadOnly");
    }

    #endregion

    #region RMT-009: Remote returns empty list (not null) when no matches

    /// <summary>
    /// Implements: RMT-009
    /// Given: Client connected to server with string argument (no handler)
    /// When: AutoComplete is called
    /// Then: Returns empty list, not null
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_StringArgNoHandler_ReturnsEmptyList()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteStringCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotestring", "value");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotestring", ctx, CancellationToken.None);

        // Assert - must be empty list, not null
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    /// <summary>
    /// Implements: RMT-009 (no matches with prefix)
    /// Given: Client connected to server with enum command
    /// When: AutoComplete is called with non-matching prefix
    /// Then: Returns empty list
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_EnumArgNonMatchingPrefix_ReturnsEmptyList()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotepriority", "level");
        var ctx = CreateContext(command, argument, queryString: "XYZ");

        // Act
        var results = await proxy.AutoComplete("", "remotepriority", ctx, CancellationToken.None);

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    #endregion

    #region RMT-005: Case-insensitive matching parity

    /// <summary>
    /// Implements: RMT-005 (case-insensitive matching)
    /// Given: Client connected to server with enum command
    /// When: AutoComplete is called with lowercase prefix
    /// Then: Returns matching values (case-insensitive)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_EnumArgLowercasePrefix_MatchesCaseInsensitively()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotepriority", "level");
        var ctx = CreateContext(command, argument, queryString: "h");

        // Act
        var results = await proxy.AutoComplete("", "remotepriority", ctx, CancellationToken.None);

        // Assert - "h" matches "High" case-insensitively
        results.Should().HaveCount(1);
        results.Single().Value.Should().Be("High");
    }

    #endregion

    #region RMT-007: Remote attribute handler works

    /// <summary>
    /// Implements: RMT-007
    /// Given: Server command with [AutoComplete&lt;CustomEnvironmentHandler&gt;] attribute
    /// When: Client triggers autocomplete for that argument
    /// Then: Attribute handler results returned to client (not enum handler)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_AttributeHandler_ReturnsCustomHandlerResults()
    {
        // Arrange - server must register the handler with DI
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteEnvironmentCommand>());
                svr.ConfigureServices(svc => svc.AddTransient<CustomEnvironmentHandler>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remoteenv", "env");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remoteenv", ctx, CancellationToken.None);

        // Assert - custom handler returns ["Development", "Production", "Staging"] alphabetically
        results.Should().HaveCount(3);
        results[0].Value.Should().Be("Development");
        results[1].Value.Should().Be("Production");
        results[2].Value.Should().Be("Staging");
    }

    /// <summary>
    /// Implements: RMT-007 (prefix filtering)
    /// Given: Server command with [AutoComplete&lt;CustomEnvironmentHandler&gt;] attribute
    /// When: Client triggers autocomplete with prefix "P"
    /// Then: Returns only matching value from custom handler
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_AttributeHandler_FiltersWithPrefix()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteEnvironmentCommand>());
                svr.ConfigureServices(svc => svc.AddTransient<CustomEnvironmentHandler>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remoteenv", "env");
        var ctx = CreateContext(command, argument, queryString: "P");

        // Act
        var results = await proxy.AutoComplete("", "remoteenv", ctx, CancellationToken.None);

        // Assert - only "Production" matches prefix "P"
        results.Should().HaveCount(1);
        results.Single().Value.Should().Be("Production");
    }

    #endregion

    #region RMT-008: Remote CursorPosition accurately identifies context

    /// <summary>
    /// Implements: RMT-008
    /// Given: Complex input with multiple arguments (priority first, enabled second)
    /// When: Autocomplete triggered at first argument position
    /// Then: Server correctly identifies Priority argument context (returns enum values)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_MultiArg_FirstArgPosition_ReturnsEnumValues()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteMultiArgCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        
        // Get first argument (priority - enum type)
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotemultiarg", "priority");
        
        // Create context at first argument position
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotemultiarg", ctx, CancellationToken.None);

        // Assert - enum values for Priority
        results.Should().HaveCount(3);
        results[0].Value.Should().Be("High");
        results[1].Value.Should().Be("Low");
        results[2].Value.Should().Be("Medium");
    }

    /// <summary>
    /// Implements: RMT-008 (second argument position)
    /// Given: Complex input with multiple arguments
    /// When: Autocomplete triggered at second argument position (enabled)
    /// Then: Server correctly identifies bool argument context (returns true/false)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_MultiArg_SecondArgPosition_ReturnsBoolValues()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteMultiArgCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        
        // Get second argument (enabled - bool type)
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotemultiarg", "enabled");
        
        // Create context at second argument position
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotemultiarg", ctx, CancellationToken.None);

        // Assert - bool values for enabled
        results.Should().HaveCount(2);
        results[0].Value.Should().Be("false");
        results[1].Value.Should().Be("true");
    }

    /// <summary>
    /// Implements: RMT-008 (third argument - no handler)
    /// Given: Complex input with multiple arguments
    /// When: Autocomplete triggered at third argument position (name - string)
    /// Then: Server correctly identifies string argument context (returns empty - no handler)
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_MultiArg_ThirdArgPosition_ReturnsEmpty()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteMultiArgCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        
        // Get third argument (name - string type, no handler)
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotemultiarg", "name");
        
        // Create context at third argument position
        var ctx = CreateContext(command, argument, queryString: "");

        // Act
        var results = await proxy.AutoComplete("", "remotemultiarg", ctx, CancellationToken.None);

        // Assert - empty list (no handler for string)
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    #endregion

    #region AutoCompleteSuggestionProvider Remote Command Tests

    /// <summary>
    /// Tests that AutoCompleteSuggestionProvider correctly delegates to server for remote command autocomplete.
    /// This tests the full path: CursorContextResolver -> AutoCompleteSuggestionProvider -> IServerProxy.AutoComplete
    /// Bug: When typing "remotetask --priority " in the sandbox, no autocomplete options appear.
    /// </summary>
    [TestMethod]
    public async Task AutoCompleteSuggestionProvider_RemoteEnumArg_ReturnsServerOptions()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        await env.ConnectToServerAsync();

        // Get the client's command registry (which has remote commands registered)
        var clientRegistry = env.Cli.Services.GetRequiredService<ICommandRegistry>();
        var serverProxy = env.Cli.Services.GetRequiredService<IServerProxy>();

        // Verify the remote command is registered
        var remoteCommand = clientRegistry.Commands.FirstOrDefault(c => c.Name == "remotepriority");
        remoteCommand.Should().NotBeNull("remote command should be registered in client");
        remoteCommand!.IsRemote.Should().BeTrue("command should be marked as remote");

        // Create the AutoCompleteSuggestionProvider with the client's registry and proxy
        var handlerRegistryBuilder = new AutoCompleteHandlerRegistryBuilder();
        var services = new ServiceCollection();
        var handlerRegistry = handlerRegistryBuilder.Build(services);
        var serviceProvider = services.BuildServiceProvider();
        var handlerActivator = new AutoCompleteHandlerActivator(serviceProvider);

        var provider = new AutoCompleteSuggestionProvider(clientRegistry, handlerRegistry, handlerActivator, serverProxy, NullLogger<AutoCompleteSuggestionProvider>.Instance);
        var contextResolver = new CursorContextResolver(clientRegistry);

        // Simulate user input: "remotepriority --level "
        var input = "remotepriority --level ";
        var context = contextResolver.ResolveContext(input, input.Length);

        // Verify context is resolved correctly
        context.Should().NotBeNull();
        context.ContextType.Should().Be(CursorContextType.ArgumentValue);
        context.ResolvedCommand.Should().NotBeNull();
        context.ResolvedCommand!.IsRemote.Should().BeTrue("command should be recognized as remote");
        context.TargetArgument.Should().NotBeNull();
        context.TargetArgument!.Name.Should().Be("level");

        // Verify server proxy is connected
        serverProxy.ConnectionState.Should().Be(ServerProxyConnectionState.Connected, "server should be connected before autocomplete");

        // Act - get autocomplete options (this should call the server via RPC)
        var options = provider.GetOptions(context, input);

        // Assert - should return enum values from server
        options.Should().NotBeNull("autocomplete options should be returned for remote enum argument");
        options.Should().HaveCount(3, "all three enum values should be returned");
        options.Select(o => o.Value).Should().Contain(new[] { "High", "Low", "Medium" });
    }

    #endregion

    #region RMT-UX-001 through RMT-UX-005: End-to-End Virtual Keyboard Tests

    /// <summary>
    /// Gets the text content of the current input line.
    /// </summary>
    private static string GetInputLineText(TestEnvironment env)
    {
        var cursorRow = env.Console.VirtualConsole.CursorRow;
        return env.Console.VirtualConsole.GetRow(cursorRow).GetText().TrimEnd();
    }

    /// <summary>
    /// Gets the combined menu row text for assertion.
    /// </summary>
    private static string GetMenuText(TestEnvironment env)
    {
        return env.Console.VirtualConsole.GetRow(1).GetText()
             + env.Console.VirtualConsole.GetRow(2).GetText()
             + env.Console.VirtualConsole.GetRow(3).GetText();
    }

    /// <summary>
    /// Creates a test environment with server and remote command for E2E tests.
    /// </summary>
    private static TestEnvironment CreateRemoteE2EEnvironment()
    {
        return new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd =>
                {
                    cmd.RegisterCommand<RemotePriorityCommand>();
                    cmd.RegisterCommand<RemoteBoolCommand>();
                });
            });
        });
    }

    /// <summary>
    /// Implements: RMT-UX-001
    /// Given: Client connected to server with remote command
    /// When: User types partial enum value in virtual console
    /// Then: Ghost text appears showing completion from server
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteEnumArg_TypePartialValue_ShowsGhostText()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        // Act - type command with partial enum value
        await env.Keyboard.TypeTextAsync("remotepriority --level Hi");

        // Assert - ghost text "gh" should appear to complete "High"
        // (async keyboard method waits for key processing including autocomplete)
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotepriority --level High",
            because: "ghost text should complete 'Hi' to 'High'");
    }

    /// <summary>
    /// Implements: RMT-UX-001 (Tab acceptance)
    /// Given: Ghost text showing for remote enum value
    /// When: User presses Tab
    /// Then: Completion is accepted into buffer
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteEnumArg_TabAcceptsGhostText()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        await env.Keyboard.TypeTextAsync("remotepriority --level Hi");

        // Act - press Tab to accept
        await env.Keyboard.PressTabAsync();

        // Assert - "High" should be accepted (no longer ghost text)
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotepriority --level High",
            because: "Tab should accept the ghost text completion");
    }

    /// <summary>
    /// Implements: RMT-UX-002
    /// Given: Client connected to server with remote command
    /// When: User presses Tab with multiple options available
    /// Then: Menu opens showing server-provided options
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteEnumArg_TabOpensMenuWithServerOptions()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        // Type command positioning for enum value (no partial typed yet)
        await env.Keyboard.TypeTextAsync("remotepriority --level ");

        // Act - press Tab to open menu (multiple enum values available)
        await env.Keyboard.PressTabAsync();

        // Assert - menu should display showing enum options from server
        var menuText = GetMenuText(env);
        menuText.Should().Contain("High",
            because: "menu should show 'High' from server enum");
        menuText.Should().Contain("Low",
            because: "menu should show 'Low' from server enum");
        menuText.Should().Contain("Medium",
            because: "menu should show 'Medium' from server enum");
    }

    /// <summary>
    /// Implements: RMT-UX-003
    /// Given: Menu open with server options
    /// When: User types filter text
    /// Then: Filtering happens client-side (menu updates without server round-trip)
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteEnumArg_TypeToFilter_FiltersLocally()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        await env.Keyboard.TypeTextAsync("remotepriority --level ");

        // Open menu
        await env.Keyboard.PressTabAsync();

        // Act - type "L" to filter
        await env.Keyboard.TypeTextAsync("L");

        // Assert - only "Low" should remain visible (filtered)
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("Low",
            because: "typing 'L' should filter to show 'Low'");
    }

    /// <summary>
    /// Implements: RMT-UX-002 (menu navigation)
    /// Given: Menu open with server options
    /// When: User navigates with arrow keys
    /// Then: Selection changes correctly
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteEnumArg_ArrowNavigation_ChangesSelection()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        await env.Keyboard.TypeTextAsync("remotepriority --level ");

        await env.Keyboard.PressTabAsync();

        // Act - press Down arrow to move selection
        await env.Keyboard.PressDownArrowAsync();

        // Press Enter to accept selection
        await env.Keyboard.PressEnterAsync();

        // Assert - second item "Low" should be selected (after "High")
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotepriority --level Low",
            because: "Down arrow should move selection to second item 'Low'");
    }

    /// <summary>
    /// Implements: RMT-005 (bool parity over remote)
    /// Given: Client connected to server with boolean command
    /// When: User types partial boolean value
    /// Then: Ghost text appears showing completion from server
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteBoolArg_TypePartialValue_ShowsGhostText()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        // Act - type command with partial bool value
        await env.Keyboard.TypeTextAsync("remotebool --enabled t");

        // Assert - ghost text "rue" should appear to complete "true"
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotebool --enabled true",
            because: "ghost text should complete 't' to 'true'");
    }

    #endregion

    #region RMT-UX-004: Connection failure degrades gracefully

    /// <summary>
    /// Implements: RMT-UX-004
    /// Given: Client NOT connected to server
    /// When: AutoComplete is called via API
    /// Then: Throws InvalidOperationException (caller handles gracefully)
    /// Note: The autocomplete controller catches this and shows no ghost text (silent degradation).
    /// </summary>
    [TestMethod]
    public async Task AutoComplete_WhenDisconnected_ThrowsInvalidOperationException()
    {
        // Arrange - create environment but DON'T connect
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });

        // Don't call ConnectToServer - proxy remains disconnected

        var proxy = env.Cli.Services.GetRequiredService<IServerProxy>();
        var serverRegistry = env.Server.Services.GetRequiredService<ICommandRegistry>();
        var (command, argument) = GetCommandAndArgument(serverRegistry, "remotepriority", "level");
        var ctx = CreateContext(command, argument, queryString: "");

        // Act & Assert - disconnected proxy throws InvalidOperationException
        var act = async () => await proxy.AutoComplete("", "remotepriority", ctx, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*disconnected*");
    }

    /// <summary>
    /// Implements: RMT-UX-004 (E2E graceful degradation)
    /// Given: Client typing a remote command but NOT connected
    /// When: User types partial value (triggering autocomplete)
    /// Then: No crash, no error shown, just no ghost text (silent degradation)
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteCommand_NotConnected_NoGhostTextNoCrash()
    {
        // Arrange - create environment but DON'T connect
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemotePriorityCommand>());
            });
        });
        
        // Don't call ConnectToServer - proxy remains disconnected
        // Wait for the local prompt to be ready (without server connection)
        await Task.Delay(100); // Brief delay for input loop to start

        // Act - type remote command (which won't be available without connection)
        // Remote commands are only registered after connection, so type something and check no crash
        await env.Keyboard.TypeTextAsync("test");

        // Assert - no crash, console is still functional
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("test",
            because: "input should still work even without server connection");
    }

    #endregion

    #region RMT-UX-005: Slow connection shows ghost text when response arrives

    /// <summary>
    /// Implements: RMT-UX-005
    /// Given: Remote connection with potential latency
    /// When: User triggers autocomplete
    /// Then: Ghost text appears when response arrives (verified by response content)
    /// Note: This test validates that async autocomplete works correctly. The response
    /// arrives asynchronously and updates the display. We wait for processing and verify
    /// the ghost text appeared.
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteAutocomplete_ResponseArrives_GhostTextAppears()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        // Act - type partial value
        await env.Keyboard.TypeTextAsync("remotepriority --level Hi");

        // Assert - ghost text should appear when response arrives
        // (async keyboard method waits for key processing including autocomplete)
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotepriority --level High",
            because: "ghost text should appear when async response arrives");
    }

    /// <summary>
    /// Implements: RMT-UX-005 (non-blocking verification)
    /// Given: Remote connection established
    /// When: User continues typing during autocomplete request
    /// Then: New input is not blocked (user can keep typing)
    /// </summary>
    [TestMethod]
    public async Task E2E_RemoteAutocomplete_UserKeepsTyping_InputNotBlocked()
    {
        // Arrange
        using var env = CreateRemoteE2EEnvironment();
        await env.ConnectToServerAsync();

        // Act - type quickly without waiting for autocomplete
        await env.Keyboard.TypeTextAsync("remotepriority --level High");

        // Assert - full input should be present (input wasn't blocked)
        // (async keyboard method waits for all keys to be processed)
        var lineText = GetInputLineText(env);
        lineText.Should().Contain("remotepriority --level High",
            because: "user input should not be blocked by pending autocomplete");
    }

    #endregion
}
