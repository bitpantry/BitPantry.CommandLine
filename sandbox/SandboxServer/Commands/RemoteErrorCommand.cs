using BitPantry.CommandLine.API;
using BitPantry.CommandLine.Commands;
using Spectre.Console;

namespace SandboxServer.Commands;

public enum ErrorType
{
    /// <summary>Throws CommandFailedException directly.</summary>
    UserFacing,

    /// <summary>Throws a regular Exception (not user-facing).</summary>
    Regular,

    /// <summary>Throws CommandFailedException with an inner exception.</summary>
    WithInner,

    /// <summary>Throws a custom exception that implements IUserFacingException.</summary>
    Custom
}

/// <summary>
/// A custom exception implementing IUserFacingException to test the marker interface.
/// </summary>
public class CustomCommandFailedException : Exception, IUserFacingException
{
    public CustomCommandFailedException(string message) : base(message) { }
    public CustomCommandFailedException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Tests remote error handling for user-facing exceptions.
/// This command allows manual testing of different exception types
/// and how they are propagated and rendered on the client.
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
