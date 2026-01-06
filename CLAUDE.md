# BitPantry.CommandLine Development Guidelines

Auto-generated from all feature plans. Last updated: 2025-12-24

**See [DESIGN.md](DESIGN.md) for CLI look-and-feel guidelines (colors, spacing, symbols, output formatting).**

## Active Technologies
- C# / .NET 8.0 + Spectre.Console, Microsoft.AspNetCore.SignalR.Client, System.Security.Cryptography.ProtectedData (Windows), Sodium.Core (cross-platform) (006-core-commands)
- JSON file for profiles, OS credential store + encrypted file fallback for credentials (006-core-commands)
- C# / .NET 8.0 + MSBuild/NuGet (CPM), GitHub Actions, Git (007-flex-version-mgmt)
- N/A (file-based: .csproj, Directory.Packages.props, YAML workflows) (007-flex-version-mgmt)
- C# / .NET 8.0 + System.IO.Abstractions (existing), Spectre.Console (existing), SignalR (existing) (008-remote-file-commands)
- `SandboxedFileSystem` confined to `StorageRootPath` (from 001-unified-file-system) (008-remote-file-commands)
- C# / .NET 8.0 + Spectre.Console 0.49.1 (production), Spectre.Console.Testing 0.54.0 (test), Verify.MSTest (test) (009-spectre-visual-refactor)
- C# / .NET 8.0 + Spectre.Console (rendering, markup) (010-menu-filter)
- N/A (in-memory state only) (010-menu-filter)
- C# / .NET 8.0 + None (zero external dependencies for core package) (011-virtual-console)
- In-memory 2D character buffer (ScreenCell[,]) (011-virtual-console)
- C# / .NET 8.0 + MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers (all existing) (012-virtualconsole-autocomplete-tests)
- N/A (testing infrastructure only) (012-virtualconsole-autocomplete-tests)

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
- 012-virtualconsole-autocomplete-tests: Added C# / .NET 8.0 + MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers (all existing)
- 012-virtualconsole-autocomplete-tests: Added C# / .NET 8.0 + MSTest, FluentAssertions, Moq, System.IO.Abstractions.TestingHelpers (all existing)
- 011-virtual-console: Added C# / .NET 8.0 + None (zero external dependencies for core package)

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

For detailed testing patterns, see [specs/009-spectre-visual-refactor/quickstart.md](specs/009-spectre-visual-refactor/quickstart.md#writing-tests).

### Testing Categories

| Category | What It Tests | Infrastructure | When to Use |
|----------|---------------|----------------|-------------|
| **State Tests** | Controller behavior (IsMenuVisible, SelectedIndex, Buffer) | StepwiseTestRunner | Logic, navigation, behavior |
| **Visual Output Tests** | Rendered ANSI sequences (colors, highlighting) | StepwiseTestRunner + ANSI assertions | Styling, colors, formatting |
| **Snapshot Tests** | Full rendered output against baselines | Verify.MSTest | Regression prevention |
| **Unit Tests** | Isolated component logic | Direct instantiation | Component implementation |

### ⚠️ Critical: State Tests vs Visual Output Tests

If your feature involves **styling** (colors, highlighting, selection indicators), you MUST verify ANSI output:

```csharp
// State test - checks behavior only
runner.Should().HaveMenuVisible().WithSelectedIndex(0);

// Visual test - checks rendered output
runner.Should().HaveBlueHighlighting();  // Verifies \u001b[34m in output
runner.Should().HaveInvertedSelection(); // Verifies \u001b[7m in output
runner.Should().ContainAnsiSequence("\u001b[34m"); // Custom ANSI check
```

### Testing Requirements

1. **Every user flow** (happy path AND edge case) MUST be validated by an E2E test
2. **Visual features** (colors, highlighting) MUST assert on ANSI output, not just state
3. **E2E tests use `ConsolidatedTestConsole`** which captures ANSI output by default
4. **Bug fixes MUST include an E2E test** that reproduces the bug scenario
5. Unit/component tests are helpful for implementation, but NOT sufficient for user flow validation

### Test Infrastructure

- `ConsolidatedTestConsole` - Spectre TestConsole wrapper with cursor tracking and ANSI capture
- `StepwiseTestRunner` - Process keystrokes ONE AT A TIME with state inspection between each key
- `StepwiseTestRunnerAssertions` - FluentAssertions extensions:
  - State: `HaveBuffer`, `HaveState`, `HaveMenuVisible`, `HaveMenuSelectedIndex`
  - ANSI: `ContainAnsiSequence`, `HaveBlueHighlighting`, `HaveInvertedSelection`
- `Verify.MSTest` - Snapshot testing for visual regression
- Test files: Visual tests in `AutoComplete/Visual/`, Snapshots in `Snapshots/`

### StepwiseTestRunner (Visual UX Testing)

For debugging complex visual issues, use `StepwiseTestRunner` which allows:
- Processing keystrokes one at a time with `TypeText()` and `PressKey()`
- Inspecting `Buffer`, `BufferPosition`, `DisplayedLine`, `CursorColumn` between steps
- Asserting menu state with `IsMenuVisible`, `SelectedMenuItem`
- Asserting ANSI output with `Console.Output` and ANSI assertion helpers

```csharp
using var runner = CreateRunner();
runner.Initialize();
await runner.TypeText("server ");
runner.Should().HaveState("server ", 7);
await runner.PressKey(ConsoleKey.Tab);
runner.Should().HaveMenuVisible();
runner.SelectedMenuItem.Should().Be("connect");

// For visual features, also verify ANSI output:
runner.Should().HaveInvertedSelection();  // Menu selection styling
```

### Common ANSI Codes

| Visual Effect | ANSI Code | Assertion |
|--------------|-----------|-----------|
| Blue foreground (highlighting) | `\u001b[34m` | `.HaveBlueHighlighting()` |
| Inverted (selection) | `\u001b[7m` | `.HaveInvertedSelection()` |
| Dim/gray (ghost text) | `\u001b[90m` | `.ContainAnsiSequence("\u001b[90m")` |
| Reset | `\u001b[0m` | N/A |

### Test Organization

AutoComplete tests are organized in `BitPantry.CommandLine.Tests/AutoComplete/`:

```text
AutoComplete/
├── Visual/                    # Visual UX tests (1100+ tests)
│   ├── VisualTestBase.cs     # Shared infrastructure (CreateRunner with ANSI enabled)
│   ├── MenuBehaviorTests.cs  # Menu opening, navigation, selection
│   ├── GhostBehaviorTests.cs # Ghost text display and interaction
│   ├── InputEditingTests.cs  # Typing, backspace, cursor movement
│   ├── WorkflowTests.cs      # Multi-step user scenarios
│   └── EdgeCaseTests.cs      # Edge cases and boundary conditions
├── Rendering/                 # Isolated renderable tests
├── Providers/                 # Provider unit tests
└── Orchestrator/             # Orchestrator behavior tests
Snapshots/                     # Snapshot baseline files (.verified.txt)
```

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
