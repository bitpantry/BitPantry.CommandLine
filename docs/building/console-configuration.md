# Console Configuration

Configure the console output, low-level console service, and file system abstraction used by the application.

---

## Custom `IAnsiConsole`

By default, the builder creates a standard `AnsiConsole`. Use `UsingConsole()` to provide your own:

```csharp
var console = AnsiConsole.Create(new AnsiConsoleSettings
{
    Ansi = AnsiSupport.Yes,
    ColorSystem = ColorSystemSupport.TrueColor
});

var app = new CommandLineApplicationBuilder()
    .UsingConsole(console)
    .Build();
```

This is useful for:

- Testing with a `VirtualConsole`-backed `IAnsiConsole`
- Customizing ANSI support or color system
- Redirecting output

---

## `IConsoleService`

`IConsoleService` provides low-level console operations not covered by Spectre.Console:

```csharp
public interface IConsoleService
{
    CursorPosition GetCursorPosition();
}
```

The default implementation is `SystemConsoleService`, which wraps `System.Console`. To provide a custom implementation:

```csharp
builder.UsingConsole(console, new MyConsoleService());
```

---

## File System Abstraction

The framework uses `System.IO.Abstractions.IFileSystem` for all file operations, enabling test-friendly file access:

```csharp
builder.UsingFileSystem(new MockFileSystem());
```

The default is the real file system. The sandboxed file system used by the remote server is also an `IFileSystem` implementation, so commands using `IFileSystem` work identically in local and sandboxed modes.

---

## See Also

- [Building the Application](index.md)
- [BitPantry.VirtualConsole](../virtual-console/index.md)
- [Syntax Highlighting](../syntax-highlighting.md)
- [Remote Console I/O](../remote/remote-console-io.md)
- [Interfaces](../api-reference/interfaces.md)
