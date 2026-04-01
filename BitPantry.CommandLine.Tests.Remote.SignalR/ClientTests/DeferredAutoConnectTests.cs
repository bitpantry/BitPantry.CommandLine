using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Client;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Client;
using BitPantry.CommandLine.Remote.SignalR.Client.Profiles;
using BitPantry.CommandLine.Remote.SignalR.Client.Prompt;
using BitPantry.CommandLine.Tests.Infrastructure;
using BitPantry.CommandLine.Tests.Infrastructure.Authentication;
using BitPantry.CommandLine.Tests.Infrastructure.Http;
using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Spectre.Console.Testing;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.ClientTests;

/// <summary>
/// Tests for deferred auto-connect behavior in RunOnce mode.
/// 
/// The enhancement: Auto-connect should be deferred until after local command resolution fails.
/// This means local commands like "server profile list" should not trigger any auto-connect attempt.
/// 
/// These tests verify the ORCHESTRATION in CommandLineApplicationCore.Run():
/// - Local commands execute without auto-connect
/// - Unknown commands trigger auto-connect (if profile available)
/// - After successful auto-connect, remote commands resolve and execute
/// </summary>
[TestClass]
public class DeferredAutoConnectTests
{
    #region Test Commands

    /// <summary>
    /// A local command for testing - simulates "server profile list" or similar local-only commands.
    /// </summary>
    [Command(Name = "local-cmd")]
    public class LocalCommand : CommandBase
    {
        public static bool WasExecuted { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            WasExecuted = true;
        }
    }

    /// <summary>
    /// A server command group (similar to the real "server" group).
    /// </summary>
    [Group(Name = "server")]
    public class ServerGroup 
    { 
        /// <summary>
        /// Profile group nested under server group.
        /// </summary>
        [Group(Name = "profile")]
        public class ProfileGroup { }
    }

    [Command(Name = "list")]
    [InGroup<ServerGroup.ProfileGroup>]
    public class ProfileListCommand : CommandBase
    {
        public static bool WasExecuted { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            WasExecuted = true;
        }
    }

    /// <summary>
    /// Execution tracker for remote command verification.
    /// </summary>
    public class RemoteExecutionTracker
    {
        public bool WasExecuted { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// A remote command that exists only on the server.
    /// </summary>
    [Command(Name = "remote-cmd")]
    public class RemoteCommand : CommandBase
    {
        private readonly RemoteExecutionTracker _tracker;

        [Argument(Name = "message")]
        public string Message { get; set; } = "default";

        public RemoteCommand(RemoteExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            _tracker.WasExecuted = true;
            _tracker.Message = Message;
        }
    }

    #endregion

    #region Test Infrastructure Helpers

    /// <summary>
    /// Creates a test client with auto-connect enabled and a trackable EnsureConnectedAsync mock.
    /// </summary>
    private static (CommandLineApplication cli, Mock<IServerProxy> proxyMock, TestConsole console)
        CreateCliWithMockedProxy(
            Action<Mock<IServerProxy>> configureProxy = null,
            Action<CommandLineApplicationBuilder> configureBuilder = null)
    {
        var console = new TestConsole();
        var proxyMock = new Mock<IServerProxy>();

        // Default: disconnected state
        proxyMock.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Disconnected);
        proxyMock.Setup(p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Apply custom configuration
        configureProxy?.Invoke(proxyMock);

        var builder = new CommandLineApplicationBuilder()
            .UsingConsole(console);

        // Apply custom builder configuration
        configureBuilder?.Invoke(builder);

        // Replace IServerProxy with mock
        builder.Services.AddSingleton<IServerProxy>(proxyMock.Object);

        // Register local test commands
        builder.RegisterCommand<LocalCommand>();
        builder.RegisterCommand<ProfileListCommand>();

        var cli = builder.Build();
        return (cli, proxyMock, console);
    }

    /// <summary>
    /// Creates a server-only TestEnvironment and returns the server for use with a separate CLI.
    /// </summary>
    private static (TestEnvironment env, TestServer server) CreateServerOnly(
        Action<Infrastructure.TestServerOptions> configureServer)
    {
        var env = new TestEnvironment(opts =>
        {
            opts.ConfigureServer(configureServer);
        });
        return (env, env.Server);
    }

    /// <summary>
    /// Creates a CLI application configured with SignalR client and a mocked profile manager.
    /// </summary>
    private static CommandLineApplication CreateRunOnceClient(
        TestServer server,
        Mock<IProfileManager> profileManagerMock,
        TestConsole console,
        Action<CommandLineApplicationBuilder> configureBuilder = null)
    {
        var builder = new CommandLineApplicationBuilder()
            .UsingConsole(console)
            .ConfigureSignalRClient(opt =>
            {
                opt.HttpClientFactory = new TestHttpClientFactory(server);
                opt.HttpMessageHandlerFactory = new TestHttpMessageHandlerFactory(server);
                opt.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.LongPolling;
            });

        // Replace IProfileManager with mock
        builder.Services.AddSingleton<IProfileManager>(profileManagerMock.Object);

        // Register local test commands
        builder.RegisterCommand<LocalCommand>();
        builder.RegisterCommand<ProfileListCommand>();

        // Apply custom configuration
        configureBuilder?.Invoke(builder);

        return builder.Build();
    }

    /// <summary>
    /// Creates a profile mock configured to return the given profile as default.
    /// </summary>
    private static Mock<IProfileManager> CreateProfileMock(ServerProfile defaultProfile)
    {
        var mock = new Mock<IProfileManager>();

        if (defaultProfile != null)
        {
            mock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultProfile.Name);
            mock.Setup(m => m.GetProfileAsync(defaultProfile.Name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(defaultProfile);
        }
        else
        {
            mock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);
        }

        return mock;
    }

    /// <summary>
    /// Creates a profile that can connect to the specified server.
    /// </summary>
    private static ServerProfile CreateTestProfile(TestServer server, string name = "test-profile")
    {
        return new ServerProfile
        {
            Name = name,
            Uri = $"{server.BaseAddress.AbsoluteUri.TrimEnd('/')}/cli",
            ApiKey = "key1" // Matches TestApiKeyStore default
        };
    }

    [TestInitialize]
    public void Setup()
    {
        // Reset static state
        LocalCommand.WasExecuted = false;
        ProfileListCommand.WasExecuted = false;
    }

    #endregion

    #region Test Case 1: RunOnce_LocalCommand_DoesNotAutoConnect

    /// <summary>
    /// Test Case 1: RunOnce_LocalCommand_DoesNotAutoConnect
    /// 
    /// Given: A locally registered command exists, auto-connect profile available
    /// When: RunOnce("local-cmd")
    /// Then: Command executes successfully; EnsureConnectedAsync is NEVER called
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - RunOnce() exercises CommandLineApplicationCore.Run()
    ///   Breakage detection: YES - if auto-connect is called before resolution, the mock verify fails
    ///   Not a tautology: YES - verifies auto-connect is NOT called, not just that command works
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_LocalCommand_DoesNotAutoConnect()
    {
        // Arrange - Create CLI with mocked proxy that tracks EnsureConnectedAsync calls
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Run a locally registered command
            var result = await cli.RunOnce("local-cmd");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success,
                "local command should execute successfully");
            LocalCommand.WasExecuted.Should().BeTrue(
                "local command should have been executed");

            // CRITICAL ASSERTION: EnsureConnectedAsync should NEVER have been called
            // This is the key behavior change - local commands skip auto-connect entirely
            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for locally resolved commands");
        }
    }

    /// <summary>
    /// Same as above but with a grouped local command (server profile list).
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_GroupedLocalCommand_DoesNotAutoConnect()
    {
        // Arrange
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Run a grouped local command (like "server profile list")
            var result = await cli.RunOnce("server profile list");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success,
                "grouped local command should execute successfully");
            ProfileListCommand.WasExecuted.Should().BeTrue(
                "grouped local command should have been executed");

            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for locally resolved grouped commands");
        }
    }

    #endregion

    #region Test Case 2: RunOnce_UnknownCommand_AttemptsAutoConnect

    /// <summary>
    /// Test Case 2: RunOnce_UnknownCommand_AttemptsAutoConnect
    /// 
    /// Given: An unknown command is invoked, auto-connect profile available
    /// When: RunOnce("unknown-cmd")
    /// Then: EnsureConnectedAsync IS called (to discover remote commands)
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - RunOnce() exercises deferred auto-connect path
    ///   Breakage detection: YES - if auto-connect isn't called after resolution fails, verify fails
    ///   Not a tautology: YES - verifies conditional auto-connect behavior
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_UnknownCommand_AttemptsAutoConnect()
    {
        // Arrange - Create CLI with proxy that reports no connection
        var (cli, proxyMock, console) = CreateCliWithMockedProxy(proxy =>
        {
            // Simulate: auto-connect returns false (no profile or connection failed)
            proxy.Setup(p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
        });

        using (cli)
        {
            // Act - Run an unknown command
            var result = await cli.RunOnce("unknown-cmd");

            // Assert - Should fail with resolution error (command not found even after auto-connect)
            result.ResultCode.Should().Be(RunResultCode.ResolutionError,
                "unknown command should result in resolution error");

            // CRITICAL ASSERTION: EnsureConnectedAsync SHOULD have been called
            // because the command wasn't found locally
            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Once,
                "EnsureConnectedAsync SHOULD be called when local resolution fails");
        }
    }

    #endregion

    #region Test Case 3: RunOnce_LocalCommand_WithProfileFlag_DoesNotAutoConnect

    /// <summary>
    /// Test Case 3: RunOnce_LocalCommand_WithProfileFlag_DoesNotAutoConnect
    /// 
    /// Given: Local command invoked with --profile flag
    /// When: RunOnce("--profile myprofile local-cmd")
    /// Then: Command executes locally; --profile is captured but auto-connect NOT triggered
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - exercises global arg parsing + deferred connect
    ///   Breakage detection: YES - if auto-connect fires due to --profile, verify fails
    ///   Not a tautology: YES - verifies --profile doesn't force auto-connect for local commands
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_LocalCommand_WithProfileFlag_DoesNotAutoConnect()
    {
        // Arrange
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Run local command with --profile flag
            var result = await cli.RunOnce("--profile myprofile local-cmd");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success,
                "local command should execute successfully even with --profile flag");
            LocalCommand.WasExecuted.Should().BeTrue();

            // CRITICAL: --profile should be captured but NOT trigger auto-connect
            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for local command even with --profile flag");
        }
    }

    #endregion

    #region Test Case 4: RunOnce_RemoteCommand_AfterAutoConnect_Executes

    /// <summary>
    /// Test Case 4: RunOnce_RemoteCommand_AfterAutoConnect_Executes
    /// 
    /// Given: A command that only exists remotely is invoked; auto-connect succeeds
    /// When: RunOnce("remote-cmd --message hello")
    /// Then: Auto-connect happens (because local resolution fails), remote commands registered, command executes
    /// 
    /// This is a full integration test with a real test server.
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - full RunOnce path with deferred connect
    ///   Breakage detection: YES - if deferred connect doesn't re-resolve, command won't execute
    ///   Not a tautology: YES - validates complete pipeline after deferred connect
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_RemoteCommand_AfterAutoConnect_Executes()
    {
        // Arrange - Create server with remote command
        var (serverEnv, server) = CreateServerOnly(svr =>
        {
            svr.ConfigureServices(svc => svc.AddSingleton<RemoteExecutionTracker>());
            svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteCommand>());
        });

        using (serverEnv)
        {
            var profile = CreateTestProfile(server);
            var profileMock = CreateProfileMock(profile);
            var console = new TestConsole();

            using var cli = CreateRunOnceClient(server, profileMock, console);

            // Act - RunOnce with a remote-only command
            // Expected flow:
            // 1. Parse "remote-cmd --message hello"
            // 2. Try local resolution → fails (CommandNotFound)
            // 3. Auto-connect (profile available) → succeeds, registers remote commands
            // 4. Re-resolve → succeeds (remote-cmd now in registry)
            // 5. Execute remote command
            var result = await cli.RunOnce("remote-cmd --message hello");

            // Assert
            var tracker = server.Services.GetRequiredService<RemoteExecutionTracker>();

            result.ResultCode.Should().Be(RunResultCode.Success,
                "remote command should resolve and execute after deferred auto-connect");
            tracker.WasExecuted.Should().BeTrue(
                "remote command should have executed on server");
            tracker.Message.Should().Be("hello",
                "argument should have been passed correctly to remote command");
        }
    }

    #endregion

    #region Additional Edge Cases

    /// <summary>
    /// Verifies that local help display works without auto-connect.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_LocalCommandHelp_DoesNotAutoConnect()
    {
        // Arrange
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Request help for a local command
            var result = await cli.RunOnce("local-cmd --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpDisplayed,
                "help should be displayed for local command");

            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for local command help");
        }
    }

    /// <summary>
    /// Verifies that root help display works without auto-connect.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_RootHelp_DoesNotAutoConnect()
    {
        // Arrange
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Request root help
            var result = await cli.RunOnce("--help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpDisplayed,
                "root help should be displayed");

            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for root help");
        }
    }

    /// <summary>
    /// Verifies that group help display works without auto-connect.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_GroupHelp_DoesNotAutoConnect()
    {
        // Arrange
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act - Request help for a group
            var result = await cli.RunOnce("server --help");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.HelpDisplayed,
                "group help should be displayed");

            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for group help");
        }
    }

    /// <summary>
    /// Verifies that when already connected, local commands still don't trigger EnsureConnectedAsync.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_LocalCommand_WhenAlreadyConnected_DoesNotCallEnsureConnected()
    {
        // Arrange - Simulate already connected state
        var (cli, proxyMock, console) = CreateCliWithMockedProxy(proxy =>
        {
            proxy.Setup(p => p.ConnectionState).Returns(ServerProxyConnectionState.Connected);
        });

        using (cli)
        {
            // Act
            var result = await cli.RunOnce("local-cmd");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            LocalCommand.WasExecuted.Should().BeTrue();

            // Even though already connected, EnsureConnectedAsync shouldn't be called
            // because the command resolved locally
            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "EnsureConnectedAsync should NOT be called for local command even when already connected");
        }
    }

    /// <summary>
    /// Verifies that auto-connect failure warning is not shown for commands that resolve locally.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_LocalCommand_NoAutoConnectWarning()
    {
        // Arrange - Simulate a scenario where auto-connect would fail if called
        var (cli, proxyMock, console) = CreateCliWithMockedProxy();

        using (cli)
        {
            // Act
            var result = await cli.RunOnce("local-cmd");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success);
            
            // No warning should appear because auto-connect was never attempted
            console.Output.Should().NotContain("Warning",
                "no auto-connect warning should appear for local commands");
        }
    }

    /// <summary>
    /// Verifies that unknown command with auto-connect failure shows appropriate error.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task RunOnce_UnknownCommand_AutoConnectFails_ShowsResolutionError()
    {
        // Arrange - Auto-connect fails (returns false with LastAutoConnectFailure set)
        var autoConnectHandlerMock = new Mock<IAutoConnectHandler>();
        autoConnectHandlerMock.SetupGet(h => h.AutoConnectEnabled).Returns(true);
        autoConnectHandlerMock.SetupGet(h => h.LastAutoConnectFailure).Returns("Connection refused");

        var (cli, proxyMock, console) = CreateCliWithMockedProxy(
            proxy =>
            {
                proxy.Setup(p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>())).ReturnsAsync(false);
            },
            builder =>
            {
                builder.Services.AddSingleton<IAutoConnectHandler>(autoConnectHandlerMock.Object);
            });

        using (cli)
        {
            // Act
            var result = await cli.RunOnce("unknown-cmd");

            // Assert - Should get resolution error
            result.ResultCode.Should().Be(RunResultCode.ResolutionError,
                "unknown command should result in resolution error when auto-connect fails");

            // EnsureConnectedAsync should have been called (because local resolution failed)
            proxyMock.Verify(
                p => p.EnsureConnectedAsync(It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }

    #endregion
}
