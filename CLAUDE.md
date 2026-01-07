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

**⚠️ MANDATORY: All user-facing features MUST have tests using VirtualConsole-based infrastructure.**

### Testing Architecture Overview

The autocomplete system uses `BitPantry.VirtualConsole` for deterministic testing without real console I/O:

| Component | Purpose | Package |
|-----------|---------|---------|
| `VirtualConsole` | In-memory terminal emulator with 2D screen buffer | BitPantry.VirtualConsole |
| `KeyboardSimulator` | Generate realistic key events | BitPantry.VirtualConsole.Testing |
| `AutoCompleteTestHarness` | Complete test environment wrapper | BitPantry.CommandLine.Tests |

### Testing Categories

| Category | What It Tests | Infrastructure | When to Use |
|----------|---------------|----------------|-------------|
| **Behavior Tests** | Controller behavior (IsMenuVisible, SelectedIndex, Buffer) | AutoCompleteTestHarness | Logic, navigation, state |
| **Visual Tests** | Screen buffer content, styling | VirtualConsole + screen assertions | Colors, highlighting, layout |
| **Unit Tests** | Isolated component logic | Direct instantiation | Component implementation |

### AutoCompleteTestHarness

The primary test infrastructure for autocomplete testing:

```csharp
// Single command registration
using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();

// Multiple commands
using var harness = AutoCompleteTestHarness.WithCommands(
    typeof(ServerCommand),
    typeof(HelpCommand));

// Custom configuration
using var harness = new AutoCompleteTestHarness(
    width: 120,
    height: 30,
    promptText: "test> ",
    configureApp: builder => builder
        .RegisterCommand<ServerCommand>()
        .RegisterCommand<CustomCommand>());
```

### Key Test Methods

```csharp
// Input simulation
harness.TypeText("server ");      // Types characters
harness.PressTab();                // Press Tab key
harness.PressEnter();              // Press Enter key
harness.PressKey(ConsoleKey.DownArrow);  // Navigation keys
harness.PressEscape();             // Close menu

// State assertions
harness.Buffer.Should().Be("server ");          // Current input buffer
harness.IsMenuVisible.Should().BeTrue();        // Menu state
harness.MenuItemCount.Should().Be(5);           // Menu items
harness.SelectedIndex.Should().Be(0);           // Selection position
harness.HasGhostText.Should().BeTrue();         // Ghost text visibility

// Menu item access
var items = harness.MenuItems!.Select(m => m.InsertText).ToList();
items.Should().Contain("connect");
items.Should().Contain("disconnect");

// Screen buffer (visual assertions)
var screenContent = harness.GetScreenContent();
screenContent.Should().Contain("server connect");
```

### Writing VirtualConsole-Based Tests

**Basic pattern:**

```csharp
[TestMethod]
public void TC_X_Y_DescriptiveTestName()
{
    // Arrange
    using var harness = AutoCompleteTestHarness.WithCommand<MyCommand>();
    
    // Act
    harness.TypeText("mycommand --");
    harness.PressTab();
    
    // Assert
    harness.IsMenuVisible.Should().BeTrue("should show argument completions");
    var items = harness.MenuItems!.Select(m => m.InsertText).ToList();
    items.Should().Contain("--Host", "should include Host argument");
}
```

**Multi-step workflow:**

```csharp
[TestMethod]
public void CompleteWorkflow_TypeTabSelectEnter()
{
    // Arrange
    using var harness = AutoCompleteTestHarness.WithCommand<ServerCommand>();
    
    // Act: Type, Tab to open menu, navigate, accept
    harness.TypeText("serv");
    harness.PressTab();
    harness.IsMenuVisible.Should().BeTrue();
    
    harness.PressKey(ConsoleKey.DownArrow);
    var selected = harness.SelectedItem;
    
    harness.PressEnter();
    
    // Assert: Buffer updated with selection
    harness.IsMenuVisible.Should().BeFalse("menu should close after selection");
    harness.Buffer.Should().Contain(selected, "buffer should contain selected item");
}
```

### Test Organization

AutoComplete tests are organized in `BitPantry.CommandLine.Tests/AutoComplete/`:

```text
AutoComplete/
├── GhostTextTests.cs           # Ghost text display (TC-1.x)
├── MenuNavigationTests.cs      # Menu opening, navigation (TC-2.x)
├── MenuFilteringTests.cs       # Typing while menu open (TC-3.x)
├── InputEditingTests.cs        # Buffer manipulation (TC-4.x)
├── CommandGroupTests.cs        # Command hierarchy (TC-5.x)
├── ArgumentNameTests.cs        # Argument completions (TC-6.x)
├── ArgumentValueTests.cs       # Value completions (TC-7.x)
├── PositionalTests.cs          # Positional arguments (TC-8.x)
├── FilePathTests.cs            # File path completion (TC-9.x)
├── ScrollingTests.cs           # Viewport scrolling (TC-10.x)
├── GhostMenuInteractionTests.cs # Ghost/menu interaction (TC-11.x)
├── WorkflowTests.cs            # Multi-step workflows (TC-12.x)
├── EdgeCaseTests.cs            # Edge cases (TC-14.x)
├── CachingTests.cs             # Caching behavior (TC-18.x)
├── ProviderConfigTests.cs      # [Completion] attribute (TC-19.x)
├── MatchRankingTests.cs        # Match ordering (TC-20.x)
└── ...                         # Additional test categories
```

### Test Naming Convention

Test methods follow this pattern:
- `TC_X_Y_DescriptiveTitle()` where X.Y maps to autocomplete-test-cases.md
- Example: `TC_2_1_TabOpensMenu_WithMultipleMatches()`

### Testing Requirements

1. **Every user flow** (happy path AND edge case) MUST have a test
2. **Visual features** can assert on screen buffer content or ANSI sequences
3. **Tests use `AutoCompleteTestHarness`** for complete isolation
4. **Bug fixes MUST include a test** that reproduces the bug scenario
5. **Use strong assertions** - assert specific values, not just "count > 0"

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
