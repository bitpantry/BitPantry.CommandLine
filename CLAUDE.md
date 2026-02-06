# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

## Active Technologies
- C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0) + FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1 (005-virtualconsole-integration)
- C# / .NET 8.0 + Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations) (006-upload-command)
- Local filesystem (client), server-side sandboxed storage (via existing infrastructure) (006-upload-command)
- C# / .NET 8.0 + System.IO.Abstractions, Spectre.Console, Microsoft.Extensions.FileSystemGlobbing, SignalR (007-download-command)
- Local file system (destination), Remote sandboxed file system (source) (007-download-command)
- C# / .NET 8.0 + Microsoft.Extensions.DependencyInjection, Spectre.Console, Microsoft.Extensions.Logging (008-autocomplete-extensions)
- N/A (in-memory registry, no persistence) (008-autocomplete-extensions)
- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (001-server-profile)
- [if applicable, e.g., PostgreSQL, CoreData, files or N/A] (001-server-profile)
- C# / .NET 8.0 + Spectre.Console (console rendering with ANSI color support) (010-input-syntax-highlight)
- N/A (in-memory only) (010-input-syntax-highlight)

- C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq (004-positional-arguments)

## Project Structure

```text
src/
tests/
```

## Commands

# Add commands for C# / .NET (matches existing solution)

## Code Style

C# / .NET (matches existing solution): Follow standard conventions

## Recent Changes
- 010-input-syntax-highlight: Added C# / .NET 8.0 + Spectre.Console (console rendering with ANSI color support)
- 001-server-profile: Added [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION]
- 008-autocomplete-extensions: Added C# / .NET 8.0 + Microsoft.Extensions.DependencyInjection, Spectre.Console, Microsoft.Extensions.Logging


<!-- MANUAL ADDITIONS START -->

## File Transfer Commands

### Upload Command (`server upload`)

Uploads files from local machine to connected remote server:
- Supports glob patterns: `*.txt`, `**/*.log`, `data?.json`
- Progress display for large files (>= 25MB threshold)
- Concurrent uploads with throttling (max 4 concurrent)
- Path traversal protection on server

```csharp
await cli.Run("server upload ./local/*.txt /remote/backup/");
```

### Download Command (`server download`)

Downloads files from connected remote server to local machine:
- Supports glob patterns: `*.txt`, `**/*.log`, `data?.json`
- Filename collision detection (prevents overwrite of same-named files)
- Progress display for large transfers (>= 25MB threshold)
- Concurrent downloads with SemaphoreSlim throttling (max 4 concurrent)
- Streaming download with checksum verification
- Creates parent directories automatically

```csharp
await cli.Run("server download /remote/*.txt ./local/backup/");
```

Key Classes:
- `DownloadCommand` - Main command with pattern expansion, collision detection
- `DownloadConstants` - Thresholds: MaxConcurrentDownloads=4, ProgressDisplayThreshold=25MB
- `FileTransferService.DownloadFile()` - Streaming HTTP download with progress callback
- `FileTransferService.EnumerateFiles()` - RPC to list server files matching pattern
- `GlobPatternHelper` - Shared glob pattern parsing for both commands

## Testing Infrastructure

### Available Test Levels

| Level | Infrastructure | Use For |
|-------|----------------|---------|
| Unit/Component | Moq, MockFileSystem | Isolated class behavior, parsing, validation, mocked dependencies |
| Integration | `TestEnvironment`, `TestServer` | Client-server flows, RPC, file transfer, real HTTP/SignalR |
| UX/Functional | `VirtualConsole`, `VirtualConsoleAssertions` | Console output, prompts, progress display |

### TestEnvironment (Integration Tests)

`BitPantry.CommandLine.Tests.Remote.SignalR.Environment.TestEnvironment` provides:
- In-memory ASP.NET TestServer (no network required)
- Configured SignalR client connected to test server
- VirtualConsole for capturing console output
- Test logger for inspecting client/server logs

```csharp
using var env = new TestEnvironment(opts => {
    opts.RequireAuthentication = true;
    opts.StorageRootPath = tempDir;
});
await env.Cli.Run("server connect -u http://test/cli");
await env.Cli.Run("server upload file.txt");
env.Console.Should().ContainText("Uploaded");
```

### VirtualConsole (UX Tests)

`BitPantry.VirtualConsole.Testing.VirtualConsoleAssertions` provides FluentAssertions for console output:

```csharp
console.Should().ContainText("Upload complete");
console.Should().NotContainText("Error");
console.Should().HaveLineContaining(row: 5, "Progress:");
```

### When to Use Each Level

- **"Then: returns X"** → Unit test with mocks
- **"Then: server receives X"** → Integration test with TestEnvironment  
- **"Then: displays X"** → UX test with VirtualConsoleAssertions
- **"Then: file appears on server"** → Integration test with real temp filesystem

### Shared Test Helpers (`Helpers/`)

Reusable test infrastructure in `BitPantry.CommandLine.Tests.Remote.SignalR/Helpers/`:

| Helper | Purpose | Usage |
|--------|---------|-------|
| `TestServerProxyFactory` | Creates `Mock<IServerProxy>` with standard ServerCapabilities | `TestServerProxyFactory.CreateConnected()`, `.CreateDisconnected()` |
| `TestFileTransferServiceFactory` | Creates `FileTransferService` with all mocks wired up | `.Create(proxyMock)`, `.CreateWithContext(proxyMock)` |
| `FileTransferServiceTestContext` | Exposes all mocks from factory for test verification | `_context.Service`, `_context.HttpMessageHandlerMock`, `_context.SetupAuthenticatedTokenAsync()` |
| `TempFileScope` | Disposable temp file with automatic cleanup | `using var tempFile = new TempFileScope("content");` |
| `TestAccessTokenManager` | Creates AccessTokenManager with mocked HTTP | `TestAccessTokenManager.Create(httpResponse)` |
| `TestJwtTokenService` | Generates valid test JWT tokens | `TestJwtTokenService.GenerateAccessToken()` |
| `TestHttpClient` | Pre-configured HttpClient for tests | Various HTTP testing scenarios |

**Usage Example:**
```csharp
[TestInitialize]
public void Setup()
{
    _proxyMock = TestServerProxyFactory.CreateConnected();
    _context = TestFileTransferServiceFactory.CreateWithContext(_proxyMock);
}

[TestMethod]
public async Task Download_ValidFile_Succeeds()
{
    await _context.SetupAuthenticatedTokenAsync();
    using var tempFile = new TempFileScope();
    
    await _context.Service.DownloadFile("file.txt", tempFile.Path, CancellationToken.None);
    
    tempFile.ReadAllText().Should().Contain("expected content");
}
```

### Bug Fix Process (Quick Reference)

When fixing bugs, follow this structured approach:

1. **Hypothesize** - What specific condition causes the symptom?
2. **Search** - Does existing test coverage miss this case?
3. **Test (RED)** - Write a failing test that proves the hypothesis
4. **Trace** - Follow execution to confirm root cause
5. **Fix (GREEN)** - Minimal change to pass the test
6. **Verify** - Run related tests for regressions

See `/speckit.bugfix` for the full TDD-based bug fix workflow.

<!-- MANUAL ADDITIONS END -->
