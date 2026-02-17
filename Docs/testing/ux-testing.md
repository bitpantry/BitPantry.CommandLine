# UX Testing with VirtualConsole

Test console output, prompt rendering, and interactive behavior using `VirtualConsole` and `VirtualConsoleAssertions`.

---

## Setup

Install the companion testing package:

```shell
dotnet add package BitPantry.VirtualConsole.Testing
```

Create a `VirtualConsole` and wire it into the builder:

```csharp
var virtualConsole = new VirtualConsole(80, 24);
var ansiConsole = new VirtualConsoleAnsiAdapter(virtualConsole);

var app = new CommandLineApplicationBuilder()
    .UsingConsole(ansiConsole)
    .RegisterCommand<GreetCommand>()
    .Build();
```

---

## Asserting Output

Run a command and assert on the rendered output:

```csharp
[Fact]
public async Task Greet_Renders_Name_In_Output()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var ansiConsole = new VirtualConsoleAnsiAdapter(virtualConsole);

    var app = new CommandLineApplicationBuilder()
        .UsingConsole(ansiConsole)
        .RegisterCommand<GreetCommand>()
        .Build();

    await app.RunOnce("greet World");

    virtualConsole.Should().ContainText("Hello, World!");
}
```

---

## Testing Error Output

```csharp
[Fact]
public async Task Delete_Missing_File_Shows_Error()
{
    var virtualConsole = new VirtualConsole(80, 24);
    var ansiConsole = new VirtualConsoleAnsiAdapter(virtualConsole);

    var app = new CommandLineApplicationBuilder()
        .UsingConsole(ansiConsole)
        .UsingFileSystem(new MockFileSystem())
        .RegisterCommand<DeleteCommand>()
        .Build();

    var result = await app.RunOnce("delete missing.txt");

    result.ResultCode.Should().Be(RunResultCode.RunError);
    virtualConsole.Should().ContainText("File not found");
}
```

---

## Simulating Keyboard Input

Use `KeyboardSimulator` for testing interactive commands:

```csharp
[Fact]
public async Task Confirm_Command_Accepts_Yes()
{
    var keyboard = new KeyboardSimulator();
    keyboard.EnqueueKeys("y\n");

    var virtualConsole = new VirtualConsole(80, 24);
    // Wire keyboard and console together...

    virtualConsole.Should().ContainText("Confirmed");
}
```

---

## Assertions Reference

| Method | Description |
|--------|-------------|
| `.Should().ContainText(string)` | Screen contains the expected text |
| `.Should().NotContainText(string)` | Screen does not contain the text |
| `.Should().HaveLineContaining(string)` | At least one line contains the text |

---

## See Also

- [Testing Guide](index.md)
- [Integration Testing](integration-testing.md)
- [BitPantry.VirtualConsole](../virtual-console/index.md)
- [VirtualConsole.Testing](../virtual-console/testing-extensions.md)
