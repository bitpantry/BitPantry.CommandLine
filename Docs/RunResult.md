# RunResult
```BitPantry.CommandLine.RunResult```

The ```RunResult``` is returned from the [CommandLineApplication](CommandLineApplication)```.Run``` function and provides the status and any  output from the executed command.

```cs
/// <summary>
/// A result code enum representing the outcome of the command execution
/// </summary>
public RunResultCode ResultCode { get; internal set; }

/// <summary>
/// Any data returned as a result of the execution of the command
/// </summary>
public object Result { get; internal set; }

/// <summary>
/// Any unhandled error originating from the command and intercepted by the command line application
/// </summary>
public Exception RunError { get; internal set; }
```

---
See also,

- [CommandLineApplication](CommandLineApplication.md)
- [ResultCode](ResultCode.md)