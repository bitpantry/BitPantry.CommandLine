# BitPantry.VirtualConsole.Testing

Testing extensions that bridge VirtualConsole with FluentAssertions and Spectre.Console for automated UX testing.

> **Companion package** — Published as a separate NuGet package. Targets .NET Standard 2.0.

---

## Installation

```shell
dotnet add package BitPantry.VirtualConsole.Testing
```

---

## VirtualConsoleAssertions

FluentAssertions extensions for asserting on console output:

```csharp
using BitPantry.VirtualConsole.Testing;

var console = new VirtualConsole(80, 24);
// ... run commands that produce output ...

console.Should().ContainText("Hello, World!");
console.Should().NotContainText("Error");
console.Should().HaveLineContaining("Success");
```

| Assertion | Description |
|-----------|-------------|
| `.ContainText(string)` | Screen contains the specified text |
| `.NotContainText(string)` | Screen does not contain the text |
| `.HaveLineContaining(string)` | At least one line contains the text |

---

## VirtualConsoleAnsiAdapter

A bridge between `VirtualConsole` and Spectre.Console's `IAnsiConsole`. This allows you to capture Spectre.Console output in a virtual terminal:

```csharp
var virtualConsole = new VirtualConsole(80, 24);
var ansiConsole = new VirtualConsoleAnsiAdapter(virtualConsole);

// Use as IAnsiConsole in the builder
var app = new CommandLineApplicationBuilder()
    .UsingConsole(ansiConsole)
    .RegisterCommand<GreetCommand>()
    .Build();

await app.RunOnce("greet World");

virtualConsole.Should().ContainText("Hello, World!");
```

---

## Keyboard Simulation

Simulate keyboard input for testing interactive commands:

| Type | Description |
|------|-------------|
| `KeyboardSimulator` | Enqueues keystrokes for consumption by the application |
| `TestConsoleInput` | Implements console input backed by the keyboard simulator |

```csharp
var keyboard = new KeyboardSimulator();
keyboard.EnqueueKeys("hello\n");   // Type "hello" and press Enter

// The application reads from the simulated keyboard
```

---

## Full Integration Example

```csharp
[Fact]
public async Task Greet_Command_Produces_Expected_Output()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var ansiConsole = new VirtualConsoleAnsiAdapter(virtualConsole);

    var app = new CommandLineApplicationBuilder()
        .UsingConsole(ansiConsole)
        .RegisterCommand<GreetCommand>()
        .Build();

    var result = await app.RunOnce("greet World");

    result.ResultCode.Should().Be(RunResultCode.Success);
    virtualConsole.Should().ContainText("Hello, World!");
    virtualConsole.Should().NotContainText("Error");
}
```

---

## See Also

- [BitPantry.VirtualConsole](index.md)
- [UX Testing](../testing/ux-testing.md)
- [Integration Testing](../testing/integration-testing.md)
