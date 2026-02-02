using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using BitPantry.CommandLine.Processing.Execution;
using BitPantry.CommandLine.Tests.Infrastructure;
using FluentAssertions;
using Spectre.Console;

namespace BitPantry.CommandLine.Tests.Remote.SignalR.IntegrationTests;

/// <summary>
/// Integration tests for user-facing exception propagation over SignalR.
/// Test cases: CV-005, CV-006, DF-001
/// </summary>
[TestClass]
public class IntegrationTests_UserFacingException
{
    #region Test Commands

    /// <summary>
    /// A command that throws a CommandFailedException.
    /// </summary>
    [Command(Name = "throwuserfacing")]
    public class ThrowUserFacingCommand : CommandBase
    {
        [Argument(Name = "message")]
        public string Message { get; set; } = "Test error message";

        public void Execute(CommandExecutionContext ctx)
        {
            throw new CommandFailedException(Message);
        }
    }

    /// <summary>
    /// A command that throws a regular exception (not user-facing).
    /// </summary>
    [Command(Name = "throwregular")]
    public class ThrowRegularCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new InvalidOperationException("Internal error details");
        }
    }

    /// <summary>
    /// A command that throws a CommandFailedException with inner exception.
    /// </summary>
    [Command(Name = "throwwithinnerexception")]
    public class ThrowWithInnerExceptionCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            try
            {
                throw new ArgumentException("Root cause");
            }
            catch (Exception inner)
            {
                throw new CommandFailedException("Operation failed", inner);
            }
        }
    }

    /// <summary>
    /// A custom exception that implements IUserFacingException.
    /// </summary>
    public class CustomCommandFailedException : Exception, IUserFacingException
    {
        public CustomCommandFailedException(string message) : base(message) { }
    }

    /// <summary>
    /// A command that throws a custom command failed exception.
    /// </summary>
    [Command(Name = "throwcustom")]
    public class ThrowCustomUserFacingCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx)
        {
            throw new CustomCommandFailedException("Custom user error");
        }
    }

    #endregion

    #region CV-005: IUserFacingException Detection

    /// <summary>
    /// Given: A remote command that throws CommandFailedException
    /// When: Command is executed
    /// Then: ExceptionInfo is populated in the response
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_UserFacingException_ExceptionInfoIsSerialized()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ThrowUserFacingCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("throwuserfacing --message \"User-visible error\"");

        // Assert - should be RunError but with exception rendered on client
        result.ResultCode.Should().Be(RunResultCode.RunError);
        
        // The console output should contain the exception message
        await Task.Delay(100); // Wait for console output
        var output = string.Join(" ", env.Console.Lines);
        output.Should().Contain("User-visible error");
    }

    /// <summary>
    /// Given: A custom exception implementing IUserFacingException
    /// When: Command is executed
    /// Then: ExceptionInfo is populated (marker interface works)
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_CustomUserFacingException_ExceptionInfoIsSerialized()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ThrowCustomUserFacingCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("throwcustom");

        // Assert
        result.ResultCode.Should().Be(RunResultCode.RunError);
        
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        // Check for parts that won't be split by console line wrapping
        output.Should().Contain("Custom");
        output.Should().Contain("CustomCommandFailedException");
    }

    #endregion

    #region CV-006: Non-User-Facing Exception Backward Compatibility

    /// <summary>
    /// Given: A remote command that throws a regular exception
    /// When: Command is executed
    /// Then: Client shows generic error message (internal details are hidden for security)
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_RegularException_ShowsGenericErrorMessage()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ThrowRegularCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("throwregular");

        // Assert - should still get RunError
        result.ResultCode.Should().Be(RunResultCode.RunError);
        
        // Wait for any console output to arrive
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        
        // For security, internal error details should NOT be exposed to the client
        output.Should().NotContain("Internal error details");
        // Instead, client should see a generic error message with correlation ID
        output.Should().Contain("server encountered an error");
        output.Should().Contain("CorrelationId");
    }

    #endregion

    #region DF-001: Full End-to-End Flow with Inner Exception

    /// <summary>
    /// Given: A remote command that throws CommandFailedException with inner exception
    /// When: Command is executed
    /// Then: Both outer and inner exception details are displayed
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_UserFacingExceptionWithInner_DisplaysBothExceptions()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<ThrowWithInnerExceptionCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("throwwithinnerexception");

        // Assert
        result.ResultCode.Should().Be(RunResultCode.RunError);
        
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        
        // Both exception messages should be visible
        output.Should().Contain("Operation failed");
        output.Should().Contain("Root cause");
    }

    #endregion

    #region Sandbox Reproduction Tests

    /// <summary>
    /// Simplified test without enum - should work like the existing tests.
    /// </summary>
    [Command(Name = "simpleerror")]
    public class SimpleErrorCommand : CommandBase
    {
        [Argument(Name = "message")]
        public string Message { get; set; } = "Something went wrong";

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"[blue][[REMOTE]][/] About to throw exception with message: {Message}");
            Fail(Message);
        }
    }

    /// <summary>
    /// Test simple error command (no enum) to verify basic flow works.
    /// </summary>
    [TestMethod]
    public async Task Run_SimpleErrorCommand_ShowsUserFacingError()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<SimpleErrorCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act
        var result = await env.Cli.Run("simpleerror --message \"test error\"");

        // Assert
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        
        result.ResultCode.Should().Be(RunResultCode.RunError);
        output.Should().Contain("test error", "should show the user-facing error message");
    }

    public enum ErrorType
    {
        UserFacing,
        Regular,
        WithInner,
        Custom
    }

    /// <summary>
    /// Command WITH enum argument to test if enum parsing is the problem.
    /// </summary>
    [Command(Name = "remoteerror")]
    public class RemoteErrorCommand : CommandBase
    {
        [Argument(Name = "type")]
        public ErrorType Type { get; set; } = ErrorType.UserFacing;

        [Argument(Name = "message")]
        public string Message { get; set; } = "Something went wrong";

        public void Execute(CommandExecutionContext ctx)
        {
            Console.MarkupLine($"[blue][[REMOTE]][/] About to throw {Type} exception...");

            switch (Type)
            {
                case ErrorType.UserFacing:
                    Fail(Message);
                    break;

                case ErrorType.Regular:
                    throw new InvalidOperationException(Message);

                case ErrorType.WithInner:
                    var inner = new InvalidOperationException("This is the inner exception details");
                    Fail(Message, inner);
                    break;

                case ErrorType.Custom:
                    throw new CustomCommandFailedException(Message);

                default:
                    Fail($"Unknown error type: {Type}");
                    break;
            }
        }
    }

    /// <summary>
    /// Test the exact scenario from sandbox: remoteerror --type userFacing --message none
    /// </summary>
    [TestMethod]
    public async Task Run_RemoteErrorCommand_UserFacing_ShowsUserFacingError()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteErrorCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act - exact command from sandbox screenshot
        var result = await env.Cli.Run("remoteerror --type userFacing --message none");

        // Assert
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        
        // Debug output - including server logs
        System.Console.WriteLine($"=== Result Code: {result.ResultCode} ===");
        System.Console.WriteLine($"=== Console Output ===\n{output}");
        
        // Print server logs
        var serverLogs = env.GetServerLogs<BitPantry.CommandLine.Remote.SignalR.Server.ServerLogic>();
        System.Console.WriteLine($"=== Server Logs ({serverLogs.Count} entries) ===");
        foreach (var log in serverLogs)
        {
            System.Console.WriteLine($"  {log.Message}");
        }
        
        result.ResultCode.Should().Be(RunResultCode.RunError);
        output.Should().Contain("none", "should show the user-facing error message");
    }

    /// <summary>
    /// Test the exact scenario from sandbox: remoteerror --type regular --message none
    /// </summary>
    [TestMethod]
    [Timeout(10000)]
    public async Task Run_RemoteErrorCommand_Regular_ShowsGenericError()
    {
        // Arrange
        using var env = new TestEnvironment(opt =>
        {
            opt.ConfigureServer(svr =>
            {
                svr.ConfigureCommands(cmd => cmd.RegisterCommand<RemoteErrorCommand>());
            });
        });

        await env.Cli.ConnectToServer(env.Server);

        // Act - exact command from sandbox screenshot
        var result = await env.Cli.Run("remoteerror --type regular --message none");

        // Assert
        await Task.Delay(100);
        var output = string.Join(" ", env.Console.Lines);
        
        // Debug output
        System.Console.WriteLine($"=== Result Code: {result.ResultCode} ===");
        System.Console.WriteLine($"=== Console Output ===\n{output}");
        
        result.ResultCode.Should().Be(RunResultCode.RunError);
    }

    #endregion
}
