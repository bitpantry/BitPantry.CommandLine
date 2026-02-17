# Error Handling

How commands signal failures and how exceptions are surfaced to users.

---

## The `Fail()` Method

`CommandBase` provides a `Fail()` helper that throws a `CommandFailedException`:

```csharp
[Command(Name = "delete")]
public class DeleteCommand : CommandBase
{
    [Argument(Position = 0, IsRequired = true)]
    public string FileName { get; set; } = "";

    public void Execute(CommandExecutionContext ctx)
    {
        if (!System.IO.File.Exists(FileName))
            Fail($"File not found: {FileName}");

        System.IO.File.Delete(FileName);
        Console.MarkupLine($"[green]Deleted {FileName}[/]");
    }
}
```

```
app> delete missing.txt
Error: File not found: missing.txt
```

`Fail()` has two overloads:

```csharp
protected void Fail(string message)
protected void Fail(string message, Exception innerException)
```

---

## CommandFailedException

The exception thrown by `Fail()`. It implements `IUserFacingException`, which marks its message as safe to display to end users:

```csharp
public class CommandFailedException : Exception, IUserFacingException
{
    public CommandFailedException(string message)
    public CommandFailedException(string message, Exception innerException)
}
```

You can also throw `CommandFailedException` directly:

```csharp
throw new CommandFailedException("Invalid configuration");
```

---

## IUserFacingException

A marker interface that indicates an exception's message is safe to display to end users — including over remote connections:

```csharp
public interface IUserFacingException { }
```

Any exception implementing this interface will have its message shown to the user. Exceptions that do not implement it produce a generic error message instead, preventing internal details from leaking.

Implement this on your own exception types when appropriate:

```csharp
public class ValidationException : Exception, IUserFacingException
{
    public ValidationException(string message) : base(message) { }
}
```

---

## RunResult Error Codes

When a command fails, the `RunResult` captures the outcome:

| `RunResultCode` | Value | Description |
|-----------------|-------|-------------|
| `Success` | `0` | Command completed successfully |
| `ParsingError` | `1001` | Input could not be parsed |
| `ResolutionError` | `1002` | Command or arguments could not be resolved |
| `RunError` | `1003` | Command threw an exception during execution |
| `RunCanceled` | `1004` | Execution was canceled via `CancellationToken` |
| `HelpValidationError` | `1005` | Help-related validation failed |
| `HelpDisplayed` | `0` | Help was displayed (treated as success) |

```csharp
var result = await app.RunOnce("delete missing.txt");

if (result.ResultCode == RunResultCode.RunError)
    System.Console.Error.WriteLine(result.RunError.Message);
```

---

## See Also

- [The Processing Pipeline](../running/processing-pipeline.md)
- [Running Commands](../running/index.md)
- [Remote Console I/O](../remote/remote-console-io.md)
- [Interfaces](../api-reference/interfaces.md)
