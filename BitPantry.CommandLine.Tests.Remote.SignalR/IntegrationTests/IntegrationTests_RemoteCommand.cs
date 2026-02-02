using BitPantry.CommandLine;
using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Remote.SignalR.Server;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;

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

    [Command(Name = "enumcmd")]
    public class EnumCommand : CommandBase
    {
        [Argument(Name = "priority")]
        public TaskPriority Priority { get; set; }

        public static TaskPriority? LastExecutedPriority { get; set; }

        public void Execute(CommandExecutionContext ctx)
        {
            LastExecutedPriority = Priority;
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
        EnumCommand.LastExecutedPriority = null;

        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<EnumCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("enumcmd --priority Low");

        // Build error info for assertion message
        var serverLogs = env.GetServerLogs<ServerLogic>();
        var serverLogInfo = serverLogs.Any() 
            ? $" ServerLogs: {string.Join(" | ", serverLogs.Select(l => l.Message))}"
            : "";
        
        var errorInfo = result.RunError != null 
            ? $" Error: {result.RunError.GetType().Name}: {result.RunError.Message}" +
              (result.RunError.InnerException != null 
                  ? $" Inner: {result.RunError.InnerException.GetType().Name}: {result.RunError.InnerException.Message}" 
                  : "") + serverLogInfo
            : "";

        // Assert
        result.ResultCode.Should().Be(RunResultCode.Success, 
            $"remote command with enum argument should execute successfully.{errorInfo}");
        EnumCommand.LastExecutedPriority.Should().Be(TaskPriority.Low,
            "command should have received the correct enum value");
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
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<EnumCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        var enumValues = new[] { "Low", "Medium", "High", "Critical" };
        var expectedValues = new[] { TaskPriority.Low, TaskPriority.Medium, TaskPriority.High, TaskPriority.Critical };

        for (int i = 0; i < enumValues.Length; i++)
        {
            // Reset
            EnumCommand.LastExecutedPriority = null;

            // Act
            var result = await env.Cli.Run($"enumcmd --priority {enumValues[i]}");

            // Assert
            result.ResultCode.Should().Be(RunResultCode.Success,
                $"remote command with enum value '{enumValues[i]}' should execute successfully");
            EnumCommand.LastExecutedPriority.Should().Be(expectedValues[i],
                $"command should have received enum value {expectedValues[i]}");
        }
    }

    #endregion
}
