# Integration Testing

Test the full client-server pipeline using an in-memory `TestServer` — no network required.

---

## TestEnvironment

The test infrastructure provides a `TestEnvironment` that bundles:

- An in-memory ASP.NET `TestServer` with the SignalR hub configured
- A `CommandLineApplicationBuilder` pre-wired to connect to the test server
- A test logger for inspecting client and server log output

This enables full round-trip testing: client sends a command, server executes it, and the client receives the result.

---

## Basic Pattern

```csharp
[Fact]
public async Task Remote_Command_Executes_On_Server()
{
    using var env = new TestEnvironment(serverOpts =>
    {
        serverOpts.RegisterCommand<ServerStatusCommand>();
    });

    // Connect client to test server
    await env.ConnectAsync();

    // Execute a remote command
    var result = await env.App.RunOnce("server-status");

    Assert.Equal(RunResultCode.Success, result.ResultCode);
}
```

---

## Verifying Server-Side Behavior

Register services with the test server and verify they were called:

```csharp
[Fact]
public async Task Deploy_Calls_Deployment_Service()
{
    var deployMock = new Mock<IDeploymentService>();

    using var env = new TestEnvironment(serverOpts =>
    {
        serverOpts.RegisterCommand<DeployCommand>();
    }, services =>
    {
        services.AddSingleton(deployMock.Object);
    });

    await env.ConnectAsync();
    await env.App.RunOnce("deploy --environment staging");

    deployMock.Verify(d => d.DeployAsync("staging"), Times.Once);
}
```

---

## Testing File Transfers

The `TestEnvironment` configures file transfer options with a temporary storage root, enabling upload/download tests without affecting the real file system.

---

## No Network Required

The in-memory `TestServer` uses `Microsoft.AspNetCore.TestHost`, which processes HTTP/SignalR requests in-process. Tests run fast and reliably in CI without opening ports.

---

## See Also

- [Testing Guide](index.md)
- [UX Testing](ux-testing.md)
- [Building the Application](../building/index.md)
- [VirtualConsole.Testing](../virtual-console/testing-extensions.md)
