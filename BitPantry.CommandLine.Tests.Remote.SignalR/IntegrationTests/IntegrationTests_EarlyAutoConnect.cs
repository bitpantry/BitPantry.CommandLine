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
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Spectre.Console;
using Spectre.Console.Testing;
using IHttpClientFactory = BitPantry.CommandLine.Remote.SignalR.Client.IHttpClientFactory;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for early auto-connect in RunOnce mode.
/// Validates that remote commands are registered before resolution when a profile is available.
/// These tests cover the full pipeline integration (auto-connect → registration → resolution → execution).
/// 
/// Verification question: "If someone reverted the early EnsureConnectedAsync() call in
/// CommandLineApplicationCore.Run(), would any test fail?"
/// Answer: YES. Tests 1-5 all depend on auto-connect happening BEFORE command resolution.
/// Without early connect, remote commands like "remote-test" would not be in the registry
/// when the resolver runs, causing ResolutionError.
/// </summary>
[TestClass]
public class IntegrationTests_EarlyAutoConnect
{
    #region Test Commands (Server-Side)

    /// <summary>
    /// Captures execution results for test verification via DI.
    /// </summary>
    public class ExecutionTracker
    {
        public bool WasExecuted { get; set; }
        public string LastMessage { get; set; }
    }

    [Command(Name = "remote-test")]
    public class RemoteTestCommand : CommandBase
    {
        private readonly ExecutionTracker _tracker;

        [Argument(Name = "message")]
        public string Message { get; set; } = "default";

        public RemoteTestCommand(ExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            _tracker.WasExecuted = true;
            _tracker.LastMessage = Message;
        }
    }

    #endregion

    #region Test Commands (Client-Side Local)

    [Command(Name = "local-test")]
    public class LocalTestCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            // No tracking needed - success verified by result code
        }
    }

    #endregion

    #region Test Infrastructure Helpers

    /// <summary>
    /// Creates a server-only TestEnvironment and returns the server for use with a separate CLI.
    /// This allows testing RunOnce() without the REPL loop interference.
    /// IMPORTANT: The returned TestEnvironment must be disposed to clean up the server.
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
    /// This CLI is independent of TestEnvironment's REPL loop, suitable for RunOnce() testing.
    /// </summary>
    private static CommandLineApplication CreateRunOnceClient(
        TestServer server,
        Mock<IProfileManager> profileManagerMock,
        TestConsole console)
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

    #endregion

    #region RunOnce Tests (Test Cases 1-5)

    /// <summary>
    /// Test Case 1: RunOnce_RemoteCommand_WithProfile_ResolvesAndExecutes
    /// 
    /// Given: Default profile configured, server has "remote-test" command
    /// When: RunOnce("remote-test --message hello")
    /// Then: Command resolves and executes on server (proves auto-connect happened before resolution)
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - RunOnce() triggers early EnsureConnectedAsync()
    ///   Breakage detection: YES - without early connect, remote-test would not be in registry
    ///   Not a tautology: YES - verifies pipeline ordering, not just connection
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_RemoteCommand_WithProfile_ResolvesAndExecutes()
    {
        // Arrange - Create server with remote command
        var (serverEnv, server) = CreateServerOnly(svr =>
        {
            svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
            svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
        });

        using (serverEnv)
        {
            var profile = CreateTestProfile(server);
            var profileMock = CreateProfileMock(profile);
            var console = new TestConsole();

            using var cli = CreateRunOnceClient(server, profileMock, console);

            // Act - RunOnce with default profile configured
            // This should: parse → auto-connect (using profile) → register remote commands → resolve → execute
            var result = await cli.RunOnce("remote-test --message hello");

            // Assert
            var tracker = server.Services.GetRequiredService<ExecutionTracker>();
            
            result.ResultCode.Should().Be(RunResultCode.Success,
                "remote command should resolve and execute when profile enables auto-connect");
            tracker.WasExecuted.Should().BeTrue(
                "command should have executed on server");
            tracker.LastMessage.Should().Be("hello",
                "argument should have been passed correctly");
        }
    }

    /// <summary>
    /// Test Case 2: RunOnce_RemoteCommand_WithExplicitProfile_ResolvesAndExecutes
    /// 
    /// Given: Profile "production" exists, passed via --profile
    /// When: RunOnce("--profile production remote-test")
    /// Then: Command resolves using explicit profile
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_RemoteCommand_WithExplicitProfile_ResolvesAndExecutes()
    {
        // Arrange
        var (serverEnv, server) = CreateServerOnly(svr =>
        {
            svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
            svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
        });

        using (serverEnv)
        {
            // Create profile with specific name
            var profile = CreateTestProfile(server, "production");

            var console = new TestConsole();
            var profileManagerMock = new Mock<IProfileManager>();

            // Setup: no default profile, but "production" exists
            profileManagerMock.Setup(m => m.GetDefaultProfileNameAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync((string)null);
            profileManagerMock.Setup(m => m.GetProfileAsync("production", It.IsAny<CancellationToken>()))
                .ReturnsAsync(profile);

            using var cli = CreateRunOnceClient(server, profileManagerMock, console);

            // Act - Use --profile to specify the profile explicitly
            var result = await cli.RunOnce("--profile production remote-test --message explicit");

            // Assert
            var tracker = server.Services.GetRequiredService<ExecutionTracker>();
            
            result.ResultCode.Should().Be(RunResultCode.Success,
                "remote command should resolve with explicit --profile");
            tracker.WasExecuted.Should().BeTrue();
            tracker.LastMessage.Should().Be("explicit");
        }
    }

    /// <summary>
    /// Test Case 3: RunOnce_RemoteCommand_NoProfile_ReturnsResolutionError
    /// 
    /// Given: No profile configured (no default, no --profile, no env var)
    /// When: RunOnce("remote-test")
    /// Then: ResolutionError (command not found because auto-connect couldn't run)
    /// 
    /// This proves that auto-connect is actually the mechanism that registers remote commands.
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_RemoteCommand_NoProfile_ReturnsResolutionError()
    {
        // Arrange - server has remote-test, but client has NO profile configured
        var (serverEnv, server) = CreateServerOnly(svr =>
        {
            svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
            svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
        });

        using (serverEnv)
        {
            // No profile - auto-connect can't run
            var profileMock = CreateProfileMock(defaultProfile: null);
            var console = new TestConsole();

            using var cli = CreateRunOnceClient(server, profileMock, console);

            // Act
            var result = await cli.RunOnce("remote-test --message hello");

            // Assert - should fail to resolve because remote commands weren't registered
            result.ResultCode.Should().Be(RunResultCode.ResolutionError,
                "remote command should not be found when no profile is available for auto-connect");
            
            // Verify the command wasn't executed on the server
            var tracker = server.Services.GetRequiredService<ExecutionTracker>();
            tracker.WasExecuted.Should().BeFalse();
        }
    }

    /// <summary>
    /// Test Case 4: RunOnce_RemoteCommandHelp_WithProfile_DisplaysHelp
    /// 
    /// Given: Default profile configured, server has "remote-test" command
    /// When: RunOnce("remote-test --help")
    /// Then: Help is displayed for the remote command (proves command was registered)
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_RemoteCommandHelp_WithProfile_DisplaysHelp()
    {
        // Arrange
        var (serverEnv, server) = CreateServerOnly(svr =>
        {
            svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
            svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
        });

        using (serverEnv)
        {
            var profile = CreateTestProfile(server);
            var profileMock = CreateProfileMock(profile);
            var console = new TestConsole();

            using var cli = CreateRunOnceClient(server, profileMock, console);

            // Act - Request help for a remote command
            var result = await cli.RunOnce("remote-test --help");

            // Assert - Help should be displayed (command was found in registry)
            result.ResultCode.Should().Be(RunResultCode.HelpDisplayed,
                "help should be displayed for remote command (proves it was registered via auto-connect)");
            
            // The command should not have been executed
            var tracker = server.Services.GetRequiredService<ExecutionTracker>();
            tracker.WasExecuted.Should().BeFalse("--help should show help, not execute the command");
        }
    }

    /// <summary>
    /// Test Case 5: RunOnce_AutoConnectFails_LocalCommand_StillExecutes
    /// 
    /// Given: Profile configured but server unreachable
    /// When: RunOnce("local-test")
    /// Then: Local command still executes (auto-connect failure doesn't block local commands)
    /// </summary>
    [TestMethod]
    [Timeout(15000)]
    public async Task RunOnce_AutoConnectFails_LocalCommand_StillExecutes()
    {
        // Arrange - Create a profile pointing to an invalid server (port 9999)
        // Note: We don't start a test server at all
        var badProfile = new ServerProfile
        {
            Name = "unreachable",
            Uri = "http://localhost:9999/cli", // Nothing listening here
            ApiKey = "key1"
        };

        var console = new TestConsole();
        var profileManagerMock = CreateProfileMock(badProfile);

        var builder = new CommandLineApplicationBuilder()
            .UsingConsole(console)
            .ConfigureSignalRClient(); // Default HTTP client (will fail to connect)

        builder.Services.AddSingleton<IProfileManager>(profileManagerMock.Object);

        // Register a local command
        builder.RegisterCommand<LocalTestCommand>();

        using var cli = builder.Build();

        // Act - Run local command (auto-connect will fail, but shouldn't block)
        var result = await cli.RunOnce("local-test");

        // Assert - Local command should still execute
        result.ResultCode.Should().Be(RunResultCode.Success,
            "local command should execute even when auto-connect fails");

        // Console should show warning about failed connection
        console.Output.Should().Contain("Warning",
            "auto-connect failure should produce a warning");
    }

    #endregion

    #region REPL Mode Tests (Test Case 6)

    /// <summary>
    /// Test Case 6: Run_ReplMode_DoesNotEarlyConnect
    /// 
    /// Given: REPL mode (via TestEnvironment which calls RunInteractive)
    /// When: Running a remote command without explicit connection
    /// Then: No early auto-connect (AutoConnectEnabled=false in REPL)
    /// 
    /// This test validates that the early connect only happens in RunOnce mode.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_ReplMode_DoesNotEarlyConnect()
    {
        // Arrange - Create environment with server but NO explicit connection
        // In REPL mode, AutoConnectEnabled is false, so early connect should not trigger
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
            });
        });

        // Act - Try to run a remote command without connecting first
        // In REPL mode, this should fail with resolution error since remote commands aren't registered
        var result = await env.RunCommandAsync("remote-test --message hello");

        // Assert - Should get resolution error because remote command isn't registered
        // (early auto-connect didn't happen because AutoConnectEnabled=false in REPL)
        result.ResultCode.Should().Be(RunResultCode.ResolutionError,
            "REPL mode should not early auto-connect, so remote command should not be found");
    }

    #endregion
}
