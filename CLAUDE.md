# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

## Active Technologies
- C# / .NET 8.0 + Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform) (006-core-commands)
- JSON file for profiles, OS credential store + encrypted file fallback for credentials (006-core-commands)
- C# / .NET 8.0 + MSBuild/NuGet (CPM), GitHub Actions, Git (007-flex-version-mgmt)
- N/A (file-based: .csproj, Directory.Packages.props, YAML workflows) (007-flex-version-mgmt)

- C# / .NET (matches existing solution) + BitPantry.Parsing.Strings (existing), MSTest, FluentAssertions, Moq (004-positional-arguments)

## Project Structure

```text
src/
tests/
```

## Built-in Commands

### Core Package (BitPantry.CommandLine)
- `version [--full]` - Display application version information

### SignalR Client Package (BitPantry.CommandLine.Remote.SignalR.Client)
- `server connect [profile] [--uri] [--apikey]` - Connect to remote server
- `server disconnect [--force]` - Disconnect from server  
- `server status [--verbose]` - Show connection and profile status
- `server profile add <name> --uri` - Create connection profile
- `server profile remove <name>` - Remove profile
- `server profile list` - List all profiles
- `server profile show <name>` - Show profile details
- `server profile set-default <name>` - Set default profile
- `server profile set-key <name> --apikey` - Update API key

## Prompt System

The prompt uses a segment-based architecture:
- `IPromptSegment` - Interface for prompt segments with `Order` and `Render()` 
- `IPrompt` - Interface for prompt rendering
- `CompositePrompt` - Aggregates segments sorted by Order
- `AppNameSegment` - Shows application name (core)
- `ServerConnectionSegment` - Shows connection status (SignalR client)
- `ProfileSegment` - Shows active profile (SignalR client)

## Profile/Credential Storage

- Profiles: JSON at cross-platform config paths
- Credentials: `ICredentialStore` with OS credential store or encrypted file fallback
- Profile manager: `IProfileManager` interface

## Code Style

C# / .NET (matches existing solution): Follow standard conventions

## Recent Changes
- 007-flex-version-mgmt: Added C# / .NET 8.0 + MSBuild/NuGet (CPM), GitHub Actions, Git
- 006-core-commands: Added prompt system (IPromptSegment, CompositePrompt), profile management, version command, removed lc command
- 006-core-commands: Added C# / .NET 8.0 + Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform)

## Package Management (Central Package Management)

This solution uses **NuGet Central Package Management (CPM)** to centralize all dependency versions.

### Key Files
- `Directory.Packages.props` - Central version definitions
- `Directory.Build.props` - Shared build properties including `UseProjectReferences` toggle

### Version Ranges (Internal Dependencies)
Internal BitPantry packages use version ranges for flexibility:
```xml
<PackageVersion Include="BitPantry.CommandLine" Version="[5.0.0, 6.0.0)" />
<PackageVersion Include="BitPantry.CommandLine.Remote.SignalR" Version="[1.0.0, 2.0.0)" />
```

This means downstream packages automatically accept minor/patch updates without republishing.

### External Dependencies
External packages use exact versions (no ranges).

### Local Development
Set `UseProjectReferences=true` (default) in Directory.Build.props to use project references during development.

## Release Workflow

### Unified Workflow
All packages are released via `.github/workflows/release-unified.yml`:
- **Trigger**: Tags matching `release-v*` (e.g., `release-v20260101-143052`)
- **Order**: Core → SignalR → Client/Server (parallel)
- **Features**: Version detection, NuGet polling, GitHub Release creation

### Release Process
1. Update versions in .csproj files
2. Commit changes
3. Create trigger tag: `git tag release-v$(date +%Y%m%d-%H%M%S)`
4. Push: `git push origin HEAD --tags`

### Release Agent
Use `/speckit.bp.release` in Claude to analyze changes and prepare releases.

### Deprecated Workflows
The following workflows are deprecated (use unified workflow instead):
- `build-core.yml` (triggered by `core-v*` tags)
- `build-client.yml` (triggered by `client-v*` tags)
- `build-server.yml` (triggered by `server-v*` tags)
- `build-remote-signalr.yml` (triggered by `remote-signalr-v*` tags)

## Testing

**⚠️ MANDATORY: All user-facing features MUST have End-to-End (E2E) tests.**

See `.specify/memory/testing-patterns.md` for comprehensive testing documentation.

### Testing Requirements

1. **Every user flow** (happy path AND edge case) MUST be validated by an E2E test
2. **E2E tests use `VirtualAnsiConsole` + `InputBuilder`** to simulate real user input
3. **E2E tests push keystrokes** via `console.Input.PushText()` and `console.Input.PushKey()`
4. **E2E tests call `builder.GetInput()`** to exercise the full input loop
5. **Bug fixes MUST include an E2E test** that reproduces the bug scenario
6. Unit/component tests are helpful for implementation, but NOT sufficient for user flow validation

### E2E Test Infrastructure

- `VirtualAnsiConsole` - Mock console that captures output to 2D buffer with cursor tracking
- `VirtualConsoleInput` - Queue keystrokes for consumption by InputBuilder  
- `InputBuilder` - The real input loop that processes keystrokes
- `StepwiseTestRunner` - Process keystrokes ONE AT A TIME with state inspection between each key
- `StepwiseTestRunnerAssertions` - FluentAssertions extensions (`HaveBuffer`, `HaveState`, `HaveMenuVisible`, etc.)
- Test files: `EndToEndAutocompleteTests.cs`, `FullIntegrationTests.cs`, `VisualUxTests.cs`

### StepwiseTestRunner (Visual UX Testing)

For debugging complex visual issues, use `StepwiseTestRunner` which allows:
- Processing keystrokes one at a time with `TypeText()` and `PressKey()`
- Inspecting `Buffer`, `BufferPosition`, `DisplayedLine`, `CursorColumn` between steps
- Asserting menu state with `IsMenuVisible`, `SelectedMenuItem`
- FluentAssertions: `runner.Should().HaveState("text", cursorPos)`, `HaveMenuVisible()`

```csharp
using var runner = CreateRunner();
runner.Initialize();
await runner.TypeText("server ");
runner.Should().HaveState("server ", 7);
await runner.PressKey(ConsoleKey.Tab);
runner.Should().HaveMenuVisible();
runner.SelectedMenuItem.Should().Be("connect");
```

### Test Levels (from testing-patterns.md)

| Test Type | When to Use |
|-----------|-------------|
| **E2E Tests (REQUIRED)** | ALL user flows - happy path AND edge cases |
| **StepwiseTestRunner** | Visual UX debugging, cursor positioning, menu rendering |
| Unit Tests | Isolated logic during component implementation |
| Provider Tests | Completion providers with real registry |
| Application Tests | Command execution and DI |

### Test Organization

AutoComplete tests are organized in `BitPantry.CommandLine.Tests/AutoComplete/`:

```text
AutoComplete/
├── Visual/                    # Visual UX tests (1027+ tests total)
│   ├── VisualTestBase.cs     # Shared infrastructure and test commands
│   ├── MenuBehaviorTests.cs  # Menu opening, navigation, selection
│   ├── GhostBehaviorTests.cs # Ghost text display and interaction
│   ├── InputEditingTests.cs  # Typing, backspace, cursor movement
│   ├── WorkflowTests.cs      # Multi-step user scenarios
│   ├── EdgeCaseTests.cs      # Edge cases and boundary conditions
│   └── ArgumentCompletionTests.cs  # Argument name/alias completion
├── Providers/                 # Provider unit tests
└── Integration/              # Full integration tests
```

### Argument Completion Test Coverage

The `ArgumentCompletionTests.cs` file validates:
- Ghost text shows REMAINDER only (not `-f` or `--Full`, just `f` or `Full`)
- Boolean flags don't trigger directory completion
- Used argument exclusion (by name AND alias)
- Case-insensitive matching
- Partial argument name completion

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
