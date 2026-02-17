# Unit Testing Commands

Test commands in isolation using mocks, a minimal builder, and `RunOnce()`.

---

## Basic Pattern

Build a minimal application, run a command, and assert on the result:

```csharp
[Fact]
public async Task Greet_Returns_Success()
{
    var app = new CommandLineApplicationBuilder()
        .RegisterCommand<GreetCommand>()
        .Build();

    var result = await app.RunOnce("greet World");

    Assert.Equal(RunResultCode.Success, result.ResultCode);
}
```

---

## Mocking Dependencies

Use Moq (or your preferred mocking library) and register mocks in the builder's DI container:

```csharp
[Fact]
public async Task Notify_Sends_Email()
{
    var emailMock = new Mock<IEmailService>();

    var builder = new CommandLineApplicationBuilder();
    builder.Services.AddSingleton(emailMock.Object);

    var app = builder
        .RegisterCommand<NotifyCommand>()
        .Build();

    await app.RunOnce("notify --to user@example.com --message Hello");

    emailMock.Verify(e => e.SendAsync("user@example.com", "Hello"), Times.Once);
}
```

---

## MockFileSystem

Use `System.IO.Abstractions.TestingHelpers.MockFileSystem` for file-dependent commands:

```csharp
[Fact]
public async Task ListFiles_Shows_Files()
{
    var fs = new MockFileSystem(new Dictionary<string, MockFileData>
    {
        { "/data/report.csv", new MockFileData("header,value") },
        { "/data/summary.txt", new MockFileData("summary content") },
    });

    var app = new CommandLineApplicationBuilder()
        .UsingFileSystem(fs)
        .RegisterCommand<ListFilesCommand>()
        .Build();

    var result = await app.RunOnce("list-files");

    Assert.Equal(RunResultCode.Success, result.ResultCode);
}
```

---

## Mocking IServerProxy

For commands that depend on `IServerProxy`:

```csharp
[Fact]
public async Task Status_Shows_Connected()
{
    var proxyMock = new Mock<IServerProxy>();
    proxyMock.Setup(p => p.ConnectionState)
        .Returns(ServerProxyConnectionState.Connected);

    var builder = new CommandLineApplicationBuilder();
    builder.Services.AddSingleton(proxyMock.Object);

    var app = builder
        .RegisterCommand<StatusCommand>()
        .Build();

    var result = await app.RunOnce("status");

    Assert.Equal(RunResultCode.Success, result.ResultCode);
}
```

---

## Asserting RunResult

| Property | Usage |
|----------|-------|
| `result.ResultCode` | Check for `Success`, `RunError`, etc. |
| `result.Result` | The return value from `Execute()` (for piped commands) |
| `result.RunError` | The exception if `ResultCode == RunError` |

---

## See Also

- [Testing Guide](index.md)
- [Dependency Injection](../building/dependency-injection.md)
- [Defining Commands](../commands/index.md)
