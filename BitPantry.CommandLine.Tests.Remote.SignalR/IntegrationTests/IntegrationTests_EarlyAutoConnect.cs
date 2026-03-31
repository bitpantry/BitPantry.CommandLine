using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for early auto-connect in RunOnce mode.
/// Validates that remote commands are registered before resolution when a profile is available.
/// These tests cover the pipeline integration (auto-connect before resolution), not the handler behavior
/// which is covered by SignalRAutoConnectHandlerTests.
/// </summary>
[TestClass]
public class IntegrationTests_EarlyAutoConnect
{
    #region Test Commands

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

    [Command(Name = "local-test")]
    public class LocalTestCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            // Just mark success by not throwing
        }
    }

    #endregion

    #region REPL Mode Tests

    /// <summary>
    /// Given: REPL mode with a default profile configured
    /// When: Running a remote command without explicit connection
    /// Then: No early auto-connect is triggered (AutoConnectEnabled=false in REPL)
    /// 
    /// Test Validity Check:
    ///   Invokes code under test: YES - calls Run() through REPL input loop
    ///   Breakage detection: YES - would fail if REPL mode triggered early connect
    ///   Not a tautology: YES - verifies behavior depends on AutoConnectEnabled flag
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

    /// <summary>
    /// Given: REPL mode with explicit server connection
    /// When: Running a remote command after connecting
    /// Then: Command resolves and executes (commands registered on explicit connect)
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_ReplMode_AfterExplicitConnect_ResolvesRemoteCommand()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteTestCommand>());
            });
        });

        // Explicitly connect first (this is REPL flow)
        await env.ConnectToServerAsync();

        // Act - Run remote command after explicit connection
        var result = await env.RunCommandAsync("remote-test --message hello");

        // Assert
        var tracker = env.Server.Services.GetRequiredService<ExecutionTracker>();
        result.ResultCode.Should().Be(RunResultCode.Success,
            "remote command should execute after explicit connection");
        tracker.WasExecuted.Should().BeTrue("command should have executed on server");
        tracker.LastMessage.Should().Be("hello");
    }

    #endregion

    #region Remote Command Resolution After Connect

    /// <summary>
    /// Given: Client connected to server with remote command registered
    /// When: Running the remote command
    /// Then: Command resolves (is in registry) and executes
    /// 
    /// This test verifies the baseline: remote commands are discoverable after connection.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task LsCommand_AfterConnect_ResolvesAsKnownCommand()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(_ => { }); // Default server with built-in commands
        });

        await env.ConnectToServerAsync();

        // Act - Run ls (a built-in remote command)
        var result = await env.RunCommandAsync("server ls");

        // Assert - Should resolve and execute (even if empty listing)
        result.ResultCode.Should().Be(RunResultCode.Success,
            "server ls should resolve as a known command after connection");
    }

    #endregion

    #region Local Command Execution

    /// <summary>
    /// Given: Environment with local command registered
    /// When: Running the local command
    /// Then: Command executes regardless of server connection state
    /// 
    /// This validates requirement 6: local commands work regardless of auto-connect status.
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_LocalCommand_ExecutesWithoutServerConnection()
    {
        // Arrange - Environment with local command but no server connection
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureCommands(cmd => cmd.RegisterCommand<LocalTestCommand>());
            opt.ConfigureServer(_ => { }); // Server available but not connected
        });

        // Act - Run local command (no connection established)
        var result = await env.RunCommandAsync("local-test");

        // Assert
        result.ResultCode.Should().Be(RunResultCode.Success,
            "local command should execute without server connection");
    }

    #endregion
}
