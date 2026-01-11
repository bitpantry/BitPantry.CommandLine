# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

## Active Technologies
- C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0) + FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1 (005-virtualconsole-integration)
- C# / .NET 8.0 + Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations) (006-upload-command)
- Local filesystem (client), server-side sandboxed storage (via existing infrastructure) (006-upload-command)
- C# / .NET 8.0 + System.IO.Abstractions, Spectre.Console, Microsoft.Extensions.FileSystemGlobbing, SignalR (007-download-command)
- Local file system (destination), Remote sandboxed file system (source) (007-download-command)

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
- 007-download-command: Added C# / .NET 8.0 + System.IO.Abstractions, Spectre.Console, Microsoft.Extensions.FileSystemGlobbing, SignalR
- 006-upload-command: Added C# / .NET 8.0 + Spectre.Console (progress display), Microsoft.AspNetCore.SignalR.Client (server communication), System.IO.Abstractions (file operations)
- 005-virtualconsole-integration: Added C# / .NET 8.0 (VirtualConsole targets .NET Standard 2.0) + FluentAssertions 6.12.0, Spectre.Console, MSTest 3.6.1


<!-- MANUAL ADDITIONS START -->

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
