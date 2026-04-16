<!--
  STAGED ISSUE — not yet published to GitHub.
  Use /publish-issues to create this issue on GitHub.
  
  Staging Number: 006
  GitHub Issue Number: #56
-->

# End-to-end integration: single file GetFile and SaveFile

**Labels**: enhancement, spec-012
**Blocked by**: 003, 005
**Implements**: FR-001, FR-002, FR-003, FR-004, FR-005, FR-006, FR-008, FR-009, FR-010, FR-015, FR-016, FR-017
**Covers**: US-001, US-002, US-003, US-004, US-005, US-006

## Summary

Validate the complete round-trip for single-file `GetFileAsync` and `SaveFileAsync` operations through integration tests using `TestEnvironment`. This issue is primarily about proving the end-to-end flow works by writing integration tests with purpose-built test commands and fixing any issues discovered during integration. No major new production code should be needed — phases 1-3 (issues 001–005) provide all the pieces.

## Current Behavior

Issues 001–005 provide all production code pieces: local implementation, protocol messages, server implementation, client handler, and consent infrastructure. However, the full round-trip has not been validated through real client/server interaction.

## Expected Behavior

Integration tests prove that:
- A server-side command can save a file to the client and the file appears on disk
- A server-side command can read a file from the client and receives correct content
- The same command works locally without a server
- Consent prompts appear when paths aren't pre-allowed
- Consent denial prevents file transfer
- Console output buffering works during concurrent output + consent

## Affected Area

- **Project(s):** `BitPantry.CommandLine.Tests.Remote.SignalR`, potentially minor fixes to any of the production projects
- **Key files:**
  - `BitPantry.CommandLine.Tests.Remote.SignalR/ClientFileAccess/ClientFileAccessIntegrationTests.cs` — NEW
  - `BitPantry.CommandLine.Tests.Remote.SignalR/ClientFileAccess/TestCommands/TestSaveFileCommand.cs` — NEW: test command registered on server
  - `BitPantry.CommandLine.Tests.Remote.SignalR/ClientFileAccess/TestCommands/TestGetFileCommand.cs` — NEW: test command registered on server
  - `BitPantry.CommandLine.Tests.Remote.SignalR/ClientFileAccess/TestCommands/TestStreamSaveCommand.cs` — NEW: test command for stream save
- **Spec reference:** See `specs/012-client-file-access/spec.md`
- **Plan reference:** See `specs/012-client-file-access/plan.md`

## Requirements

- [ ] Integration test proves server command saves file to client disk via `IClientFileAccess.SaveFileAsync(string, ...)` with `--allow-path` (US-001)
- [ ] Integration test proves server command reads file from client disk via `IClientFileAccess.GetFileAsync` with `--allow-path` (US-002)
- [ ] Integration test proves `SaveFileAsync(Stream, ...)` delivers in-memory content to client (US-006)
- [ ] Integration test proves same command works locally (no server) for both get and save (US-003)
- [ ] Integration test proves consent prompt appears on VirtualConsole when no `--allow-path` configured (US-004)
- [ ] Integration test proves consent prompt shows correct path (not server-controlled text) (FR-010)
- [ ] Integration test proves user denial prevents file transfer and server gets error (US-004, FR-014)
- [ ] Integration test proves `--allow-path` bypasses consent prompt (FR-011)
- [ ] Integration test proves console output is buffered during consent and resumes after (US-005, FR-012)
- [ ] Integration test proves parent directories are created on client when saving (FR-008)
- [ ] Integration test proves cancellation token cancels the operation (FR-015)
- [ ] All integration tests use `TestEnvironment` with real in-memory ASP.NET TestServer

## Prerequisites

- Blocked by: 003 — `RemoteClientFileAccess` must be implemented
- Blocked by: 005 — Client handler and consent UX must be implemented

## Implementation Guidance

### Test Commands

Create simple test commands that are registered on the server via `TestEnvironment` server options. Each command injects `IClientFileAccess` and exercises one operation:

```csharp
[Command(Name = "test-save")]
public class TestSaveFileCommand : CommandBase
{
    [Argument(Position = 0)]
    public string SourcePath { get; set; }

    [Argument(Position = 1)]
    public string ClientPath { get; set; }

    private readonly IClientFileAccess _clientFiles;

    public TestSaveFileCommand(IClientFileAccess clientFiles) { _clientFiles = clientFiles; }

    public async Task<int> ExecuteAsync(CommandExecutionContext ctx)
    {
        await _clientFiles.SaveFileAsync(SourcePath, ClientPath, ct: ctx.CancellationToken);
        return 0;
    }
}
```

Register in test setup:
```csharp
using var env = new TestEnvironment(opt => opt.ConfigureServer(serverOpts =>
{
    serverOpts.RegisterCommand<TestSaveFileCommand>();
    serverOpts.RegisterCommand<TestGetFileCommand>();
}));
```

### Integration Test Pattern

```csharp
[TestMethod]
public async Task SaveFile_RemoteCommand_FileAppearsOnClient()
{
    using var serverTemp = new TempDirectoryScope();
    using var clientTemp = new TempDirectoryScope();

    // Place source file on server
    File.WriteAllText(Path.Combine(serverTemp.Path, "export.json"), "{\"data\": true}");

    using var env = new TestEnvironment(opt => opt.ConfigureServer(serverOpts =>
    {
        serverOpts.StorageRoot = serverTemp.Path;
        serverOpts.RegisterCommand<TestSaveFileCommand>();
    }));

    await env.ConnectToServerAsync(allowPaths: new[] { clientTemp.Path + "\\**" });
    await env.RunCommandAsync($"test-save export.json {clientTemp.Path}\\export.json");

    File.Exists(Path.Combine(clientTemp.Path, "export.json")).Should().BeTrue();
    File.ReadAllText(Path.Combine(clientTemp.Path, "export.json")).Should().Be("{\"data\": true}");
}
```

### Consent Test Pattern

```csharp
[TestMethod]
public async Task GetFile_NoAllowPath_PromptsForConsent()
{
    using var clientTemp = new TempDirectoryScope();
    File.WriteAllText(Path.Combine(clientTemp.Path, "data.csv"), "a,b,c");

    using var env = new TestEnvironment(opt => opt.ConfigureServer(serverOpts =>
    {
        serverOpts.RegisterCommand<TestGetFileCommand>();
    }));

    await env.ConnectToServerAsync(); // no --allow-path

    // The command will block waiting for consent. Simulate approval:
    var commandTask = env.RunCommandAsync($"test-get {clientTemp.Path}\\data.csv");

    // Wait for prompt to appear
    await env.Console.WaitForText("File Access Request", timeout: 5000);
    env.Console.VirtualConsole.Should().ContainText("data.csv");

    // Approve
    env.Keyboard.PressKey(ConsoleKey.Y);

    await commandTask;
    // Verify command succeeded (check pipeline result or temp marker)
}
```

### Note on TestEnvironment Extensions

Some tests may require extending `TestEnvironment` or `ConnectToServerAsync` to support `--allow-path`. If so, add an `allowPaths` parameter or use the `RunCommandAsync` approach to issue the connect command with the argument directly.

## Implementer Autonomy

This issue was authored from a specification and plan — the guidance above reflects our best understanding at issue-creation time, but **the implementer will have ground truth that we don't have yet**.

**Standing directive:** If, during implementation, you discover that a different approach would better satisfy the Requirements above — a more elegant fix, a simpler design, a more robust solution — **you have full authority to deviate from the Implementation Guidance.** The Requirements section is the contract; the Implementation Guidance section is a starting point.

When deviating:
1. **Verify** the alternative still satisfies every item in Requirements.
2. **Document** the deviation and your reasoning in the PR description.
3. **Do not** silently drop requirements or weaken test coverage.

## Testing Requirements

### Test Approach

- **Test level:** Integration (TestEnvironment with real client/server)
- **Test project:** `BitPantry.CommandLine.Tests.Remote.SignalR`
- **Existing fixtures to reuse:** `TestEnvironment`, `VirtualConsole`, `VirtualConsoleAssertions`, `TempFileScope`, keyboard simulation

### Prescribed Test Cases

| # | Test Name Pattern | Scenario | Expected Outcome |
|---|-------------------|----------|------------------|
| 1 | `SaveFile_RemoteCommand_FileAppearsOnClient` | Server command saves file via IClientFileAccess | File exists on client with correct content |
| 2 | `GetFile_RemoteCommand_ReadsClientFile` | Server command reads file via IClientFileAccess | Command receives correct file content |
| 3 | `SaveFile_Stream_ContentArrivesOnClient` | Server command saves MemoryStream | Content appears on client |
| 4 | `SaveFile_LocalCommand_WritesDirectly` | Command runs locally, saves file | File written without server |
| 5 | `GetFile_LocalCommand_ReadsDirectly` | Command runs locally, reads file | File read without server |
| 6 | `GetFile_NoAllowPath_PromptsForConsent` | No --allow-path set | VirtualConsole shows consent panel |
| 7 | `GetFile_AllowPathConfigured_NoPrompt` | --allow-path covers requested path | Transfer succeeds, no prompt shown |
| 8 | `GetFile_UserDenies_CommandReceivesError` | User presses N at consent | Command gets FileAccessDeniedException |
| 9 | `ConsentPrompt_DuringOutput_OutputBuffered` | Command streams output + requests file | Output pauses during prompt, resumes after |
| 10 | `SaveFile_CreatesParentDirectories` | Destination parent doesn't exist | Parent dirs created, file saved |
| 11 | `SaveFile_CancellationToken_CancelsOperation` | Cancel during transfer | OperationCanceledException |

### Discovering Additional Test Cases

The test cases above are a starting point. During implementation, **discover and add additional test cases** as you encounter edge cases or error paths not covered above. Expect to find integration-level issues with timing, serialization, or DI scoping that unit tests didn't catch.

### TDD Workflow

Follow the `tdd-workflow` skill: write failing tests first (RED), implement (GREEN), refactor. For integration issues, the RED phase may reveal needed fixes in issues 001–005's code.
