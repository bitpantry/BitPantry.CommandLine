using BitPantry.CommandLine;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for remote command execution via SignalR.
/// Validates that commands with various argument types execute correctly on the server.
/// </summary>
[TestClass]
public class IntegrationTests_RemoteCommand
{
    #region Test Commands

    public enum TaskPriority { Low, Medium, High, Critical }

    /// <summary>
    /// Captures execution results for test verification via DI.
    /// Registered as singleton so test can retrieve it after command execution.
    /// </summary>
    public class ExecutionTracker
    {
        public TaskPriority? LastPriority { get; set; }
    }

    [Command(Name = "enumcmd")]
    public class EnumCommand : CommandBase
    {
        private readonly ExecutionTracker _tracker;

        [Argument(Name = "priority")]
        public TaskPriority Priority { get; set; }

        public EnumCommand(ExecutionTracker tracker)
        {
            _tracker = tracker;
        }

        public void Execute(CommandExecutionContext ctx)
        {
            _tracker.LastPriority = Priority;
        }
    }

    #endregion

    #region Remote Command Execution with Enum Arguments

    /// <summary>
    /// Given: Client connected to server with enum command registered
    /// When: Running a command with an enum argument value
    /// Then: Command executes successfully on the server
    /// </summary>
    [TestMethod]
    [Timeout(10000)] // 10 second timeout
    public async Task Run_RemoteCommandWithEnumArg_ExecutesSuccessfully()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<EnumCommand>());
            });
        });

        await env.ConnectToServerAsync();

        // Act
        var result = await env.RunCommandAsync("enumcmd --priority Low");

        // Get the tracker from DI to verify execution
        var tracker = env.Server.Services.GetRequiredService<ExecutionTracker>();

        // Build error info for assertion message
        var serverErrors = env.GetAllServerErrors();
        var serverLogInfo = serverErrors.Any() 
            ? $" ServerErrors: {string.Join(" | ", serverErrors.Select(l => l.ToString()))}"
            : " (no server errors logged)";
        
        var errorInfo = result.RunError != null 
            ? $" Error: {result.RunError.GetType().Name}: {result.RunError.Message}" +
              (result.RunError.InnerException != null 
                  ? $" Inner: {result.RunError.InnerException.GetType().Name}: {result.RunError.InnerException.Message}" 
                  : "") + serverLogInfo
            : "";

        // Assert
        result.ResultCode.Should().Be(RunResultCode.Success, 
            $"remote command with enum argument should execute successfully.{errorInfo}");
        tracker.LastPriority.Should().Be(TaskPriority.Low,
            $"command should have received the correct enum value.{serverLogInfo}");
    }

    /// <summary>
    /// Given: Client connected to server with enum command registered
    /// When: Running a command with each enum value
    /// Then: All enum values execute successfully
    /// </summary>
    [TestMethod]
    public async Task Run_RemoteCommandWithAllEnumValues_AllExecuteSuccessfully()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureServices(svc => svc.AddSingleton<ExecutionTracker>());
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<EnumCommand>());
            });
        });

        await env.ConnectToServerAsync();

        var tracker = env.Server.Services.GetRequiredService<ExecutionTracker>();
        var enumValues = new[] { "Low", "Medium", "High", "Critical" };
        var expectedValues = new[] { TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Critical };

        for (int i = 0; i < enumValues.Length; i++)
        {
            // Reset
            tracker.LastPriority = null;

            // Act
            var result = await env.RunCommandAsync($"enumcmd --priority {enumValues[i]}");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success,
                $"remote command with enum value '{enumValues[i]}' should execute successfully");
            tracker.LastPriority.Should().Be(expectedValues[i],
                $"command should have received enum value {expectedValues[i]}");
        }
    }

    #endregion
}
