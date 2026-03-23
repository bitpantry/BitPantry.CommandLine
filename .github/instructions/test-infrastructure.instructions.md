---
description: "Use when: setting up tests, choosing test infrastructure, selecting console type, finding test helpers, creating test fixtures, TestEnvironment, VirtualConsole, mock factories."
applyTo: "**/*Tests*/**/*.cs"
---

# Test Infrastructure

Project-specific test infrastructure for BitPantry.CommandLine. MSTest 3.6.1 + FluentAssertions 6.12.0 + Moq. C# / .NET 8.0.

## Infrastructure Analysis Checkpoint

**Before writing ANY test code, output this:**

```
Infrastructure Analysis:
  Console: [TestConsole | VirtualConsole] because [reason]
  Helpers: [list helpers being reused]
  Pattern: [existing test being followed, if any]
```

## Test Levels

| Level | Infrastructure | Use For |
|-------|----------------|---------|
| Unit/Component | Moq, MockFileSystem | Isolated class behavior, parsing, validation, mocked dependencies |
| Integration | `TestEnvironment`, `TestServer` | Client-server flows, RPC, file transfer, real HTTP/SignalR |
| UX/Functional | `VirtualConsole`, `VirtualConsoleAssertions` | Console output, prompts, progress display |

**Selection rule** — match the test's "Then" clause:
- "returns X" → Unit test with mocks
- "server receives X" → Integration test with TestEnvironment
- "displays X" → UX test with VirtualConsoleAssertions
- "file appears on server" → Integration test with real temp filesystem

## Console Selection

| Need | Use |
|------|-----|
| Text content only | `TestConsole` (Spectre.Console) — STRIPS markup |
| Colors/markup/ANSI codes | `VirtualConsole` with `VirtualConsoleAnsiAdapter` |
| Full integration console flow | `VirtualConsole` |

## TestEnvironment (Integration Tests)

`BitPantry.CommandLine.Tests.Infrastructure.TestEnvironment`:
- In-memory ASP.NET TestServer (no network, via `opts.ConfigureServer()`)
- Configured SignalR client
- VirtualConsole for output capture (default 80x24)
- Test logger with `GetClientLogs<T>()` / `GetServerLogs<T>()`

**Key properties:**

| Property | Type | Notes |
|----------|------|-------|
| `Cli` | `CommandLineApplication` | Run commands via `env.Cli.Run(...)` |
| `Console` | `VirtualConsoleAnsiAdapter` | Console output + input simulation |
| `Input` | `TestConsoleInput` | Convenience: `Console.Input` |
| `Keyboard` | `IKeyboardSimulator` | TypeText, PressTab, PressEnter, etc. |
| `Server` | `TestServer` | Throws if server not configured |
| `RemoteFileSystem` | `TestRemoteFileSystem` | File utilities; throws if no server |
| `HasServer` | `bool` | Whether server is configured |
| `UnrecognizedSequences` | `ConcurrentBag<CsiSequence>` | ANSI debugging |

```csharp
using var env = new TestEnvironment(opts => {
    opts.ConfigureServer(serverOpts => {
        serverOpts.StorageRoot = tempDir;
    });
});
await env.Cli.Run("server connect -u http://test/cli");
await env.Cli.Run("server upload file.txt");
env.Console.VirtualConsole.Should().ContainText("Uploaded");
```

## VirtualConsole (UX Tests)

```csharp
console.Should().ContainText("Upload complete");
console.Should().NotContainText("Error");
console.Should().HaveRowContaining(row: 5, "Progress:");
```

## Shared Test Helpers

All helpers live in `BitPantry.CommandLine.Tests.Infrastructure/`.

### Helpers/ directory

| Helper | Purpose | Usage |
|--------|---------|-------|
| `TestServerProxyFactory` | `Mock<IServerProxy>` with ServerCapabilities | `.CreateConnected(baseUrl?, maxUploadSize?)`, `.CreateDisconnected()`, `.CreateConnecting()`, `.ConfigureConnected()` |
| `TestFileTransferServiceFactory` | `FileTransferService` with all mocks | `.Create(proxyMock)`, `.CreateWithContext(proxyMock)`, `.CreateWithHttpClient(...)`, `.CreateMock(proxyMock)` |
| `FileTransferServiceTestContext` | Exposes all mocks + convenience setup | Props: `Service`, `LoggerMock`, `HttpClientFactoryMock`, `HttpMessageHandlerMock`, `AccessTokenManager`, `UploadRegistry`. Methods: `SetupAuthenticatedTokenAsync()`, `SetupHttpDownloadResponse(content)`, `SetupHttpDownloadResponse(contentBytes)`, `SetupHttpFaultingStreamResponse(faultAfterBytes)` |
| `TempFileScope` | Disposable temp file with auto-cleanup | `new TempFileScope()`, `new TempFileScope("content")`, `new TempFileScope(bytes)`, `TempFileScope.WithoutFile()`. Props: `Path`, `Exists`, `ReadAllText()`, `ReadAllBytes()` |
| `TestHttpClient` | Pre-configured HttpClient | `TestHttpClient.Create(httpResponse)` |
| `VirtualConsoleWriteLogExtensions` | Extensions for inspecting transient UI content | Via `WriteLog` on `VirtualConsoleAnsiAdapter` |

### Authentication/ directory

| Helper | Purpose | Usage |
|--------|---------|-------|
| `TestAccessTokenManager` | AccessTokenManager with mocked HTTP | `TestAccessTokenManager.Create(httpResponse)`, `Create(refreshThreshold, httpResponse)` |
| `TestJwtTokenService` | Valid test JWT tokens | `TestJwtTokenService.GenerateAccessToken()`, `GenerateAccessToken(accessLifetime, refreshLifetime)` |

## Infrastructure Reuse Rules

| Situation | Action |
|-----------|--------|
| Helper exists and covers need | **Reuse** it |
| Helper exists but needs extension | **Extend** it, then use it |
| Pattern repeated 3+ times across files | **Create** new shared helper in `Helpers/` |
| One-off need | Private helper in test class |
