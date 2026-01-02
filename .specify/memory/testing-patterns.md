# Testing Patterns and Infrastructure

This document describes the testing patterns available in the BitPantry.CommandLine codebase. When writing tests, choose the appropriate level based on what you're testing.

## ⚠️ MANDATORY: User Experience Validation

**All user-facing features MUST be validated through End-to-End (E2E) tests that exercise the full CLI stack.**

Unit and component tests are valuable for validating individual components during implementation, but they are NOT sufficient to confirm the user experience works correctly. Hidden integration bugs, timing issues, and state management problems often only manifest when the full stack is exercised.

### E2E Testing Requirements

1. **Every user flow** (happy path and edge case) documented in a spec MUST have a corresponding E2E test
2. **E2E tests simulate real user input** using `VirtualConsoleInput.PushText()` and `PushKey()` 
3. **E2E tests exercise the full `InputBuilder.GetInput()` loop** - not just individual components
4. **E2E tests assert on both the returned result AND console buffer content** when visual behavior matters
5. **Bug fixes MUST include an E2E test** that reproduces the bug scenario through real user input

### E2E Test Example Pattern

```csharp
[TestMethod]
[Description("GS-006: Backspace updates ghost correctly")]
public async Task Backspace_UpdatesGhost_NoResidualCharacters()
{
    // Arrange - create test context with VirtualAnsiConsole
    var (console, builder, controller) = CreateTestContext();
    
    // Simulate REAL USER INPUT through the queue
    console.Input.PushText("server");    // Type "server"
    console.Input.PushKey(ConsoleKey.Backspace);  // Backspace
    console.Input.PushKey(ConsoleKey.Backspace);  // Now "serve"
    console.Input.PushKey(ConsoleKey.Enter);
    
    // Act - exercise the FULL INPUT LOOP
    var result = await builder.GetInput(CancellationToken.None);
    
    // Assert on RESULT and CONSOLE BUFFER
    result.Should().Be("serve");
    console.Lines[0].Should().NotContain("er "); // No ghost residue
}
```

## Test Levels Overview

| Test Type | When to Use | Key Classes |
|-----------|-------------|-------------|
| **E2E Tests (REQUIRED)** | ALL user flows, happy + edge | `VirtualAnsiConsole`, `InputBuilder`, `VirtualConsoleInput` |
| **StepwiseTestRunner** | Visual UX debugging, cursor/menu state | `StepwiseTestRunner`, `StepwiseTestRunnerAssertions` |
| Unit Tests | Isolated logic during implementation | MSTest + mocks |
| Provider Tests | Completion providers with real registry | `CommandRegistry`, provider classes |
| Orchestrator Tests | Tab/completion flow without console | `CompletionOrchestrator`, `ICompletionCache` |
| Application Tests | Command execution, DI | `CommandLineApplicationBuilder` |
| Output Tests | Rendered text appearance | `StringWriter`, `AnsiConsoleSettings` |

---

## 1. Unit Tests (Isolated Component Tests)

**Pattern**: Test individual classes in isolation with mocked dependencies.

**When to use**: Testing logic in a single class without side effects.

**Example files**:
- `BitPantry.CommandLine.Tests/AutoComplete/Providers/ArgumentNameProviderTests.cs`
- `BitPantry.CommandLine.Tests/AutoComplete/Providers/EnumProviderTests.cs`

**Infrastructure needed**: Just MSTest + FluentAssertions

```csharp
[TestMethod]
public void SomeMethod_Condition_ExpectedResult()
{
    // Arrange - create instance with mocked dependencies
    var sut = new MyClass();
    
    // Act
    var result = sut.DoSomething();
    
    // Assert
    result.Should().Be(expected);
}
```

---

## 2. Provider-Level Tests (Component with Real Dependencies)

**Pattern**: Test a provider with a real `CommandRegistry` but no console interaction.

**When to use**: Testing completion providers with realistic command/group structures.

**Example files**:
- `BitPantry.CommandLine.Tests/AutoComplete/Providers/CommandProviderGroupTests.cs`

**Infrastructure needed**: Real `CommandRegistry` with test commands defined as nested classes.

```csharp
[TestClass]
public class MyProviderTests
{
    // Define test commands as nested classes
    [Group(Name = "server")]
    [CmdDescription("Server commands")]
    public class ServerGroup { }

    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [CmdDescription("Connect to server")]
    public class ConnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    private CommandRegistry CreateRegistry()
    {
        var registry = new CommandRegistry();
        registry.RegisterGroup<ServerGroup>();
        registry.RegisterCommand<ConnectCommand>();
        return registry;
    }

    [TestMethod]
    public async Task Provider_Scenario_ReturnsExpected()
    {
        var registry = CreateRegistry();
        var provider = new CommandCompletionProvider(registry);
        var context = new CompletionContext("server ", 7, registry);
        
        var result = await provider.GetCompletionsAsync(context);
        
        result.Items.Should().Contain(i => i.InsertText == "connect");
    }
}
```

**Important**: Use alias to avoid `Description` attribute conflict:
```csharp
using CmdDescription = BitPantry.CommandLine.API.DescriptionAttribute;
using TestDescription = Microsoft.VisualStudio.TestTools.UnitTesting.DescriptionAttribute;
```

---

## 3. Orchestrator Integration Tests

**Pattern**: Test the full orchestrator flow (multiple providers, cache, registry) without console I/O.

**When to use**: Testing Tab/arrow key handling through the completion pipeline.

**Example files**:
- `BitPantry.CommandLine.Tests/AutoComplete/OrchestratorIntegrationTests.cs`

**Infrastructure needed**: `CompletionOrchestrator`, providers, registry, optional cache.

```csharp
private CompletionOrchestrator CreateOrchestrator(CommandRegistry registry)
{
    var providers = new List<ICompletionProvider>
    {
        new CommandCompletionProvider(registry)
    };
    var cache = new NoOpCache(); // or CompletionCache for real caching
    return new CompletionOrchestrator(providers, cache, registry);
}

[TestMethod]
public async Task HandleTab_Input_ReturnsExpectedAction()
{
    var registry = CreateRegistry();
    var orchestrator = CreateOrchestrator(registry);
    
    var action = await orchestrator.HandleTabAsync("server ", 7);
    
    action.Type.Should().Be(CompletionActionType.OpenMenu);
    action.MenuState.Items.Should().HaveCount(4);
}
```

---

## 4. Full CLI Integration Tests (VirtualConsole)

**Pattern**: Test actual keyboard input → controller → console output using `VirtualAnsiConsole`.

**When to use**: Testing end-to-end behavior that involves console I/O, cursor movement, menu rendering.

**Example files**:
- `BitPantry.CommandLine.Tests/AutoComplete/FullIntegrationTests.cs`
- `BitPantry.CommandLine.Tests/AutoComplete/Visual/VisualUxTests.cs`

**Infrastructure located in**: `BitPantry.CommandLine.Tests/VirtualConsole/`
- `VirtualAnsiConsole` - Mock `IAnsiConsole` that captures output to 2D buffer
- `VirtualConsoleInput` - Queue keypresses with `PushText()`, `PushKey()`
- `VirtualAnsiConsoleExtensions` - Fluent configuration methods
- `VirtualCursor` - Tracks cursor position with `Move*` and `SetPosition` methods

**VirtualAnsiConsole Key Features**:
- **2D Buffer**: Maintains a list of strings representing console lines
- **Cursor Tracking**: `VirtualCursor` tracks column/line position
- **Line Ending Support**: Handles `\r\n`, `\n`, and `\r` (carriage return) correctly
  - `\r\n` and `\n`: Move to column 0 of next line
  - `\r` alone: Move to column 0 of current line (true carriage return behavior)
- **ANSI Sequence Filtering**: Can strip or preserve ANSI codes via `EmitAnsiSequences`

### 4.1 Standard InputBuilder Tests

```csharp
[TestMethod]
public async Task InputBuilder_TabKey_WorksCorrectly()
{
    // Arrange - create virtual console
    var virtualConsole = new VirtualAnsiConsole().Interactive();
    var registry = CreateTestRegistry();
    var orchestrator = CreateOrchestrator(registry);
    var controller = new AutoCompleteController(orchestrator, virtualConsole);
    
    // Queue keystrokes
    virtualConsole.Input.PushText("server ");
    virtualConsole.Input.PushKey(ConsoleKey.Tab);
    virtualConsole.Input.PushKey(ConsoleKey.Escape);
    virtualConsole.Input.PushKey(ConsoleKey.Enter);
    
    var prompt = new TestPrompt();
    var inputBuilder = new InputBuilder(virtualConsole, prompt, controller);
    
    // Act
    var result = await inputBuilder.GetInput(CancellationToken.None);
    
    // Assert
    result.Should().Be("server ");
    // Can also check virtualConsole.Buffer for output
}
```

**Testing AutoCompleteController directly**:
```csharp
[TestMethod]
public async Task Controller_Begin_SetsEngaged()
{
    var virtualConsole = new VirtualAnsiConsole();
    var controller = new AutoCompleteController(orchestrator, virtualConsole);
    var inputLine = new ConsoleLineMirror(virtualConsole, "server ", 7);
    
    await controller.Begin(inputLine);
    
    controller.IsEngaged.Should().BeTrue();
}
```

### 4.2 StepwiseTestRunner (Visual UX Testing)

**Pattern**: Process keystrokes ONE AT A TIME with state assertions between each keystroke.

**When to use**: Testing visual console state at each step of user interaction. Ideal for debugging complex autocomplete, cursor positioning, and menu rendering issues.

**Example files**:
- `BitPantry.CommandLine.Tests/AutoComplete/Visual/VisualUxTests.cs`

**Infrastructure located in**: `BitPantry.CommandLine.Tests/AutoComplete/Visual/`
- `StepwiseTestRunner` - Processes keystrokes individually with full state inspection
- `StepwiseTestRunnerAssertions` - FluentAssertions extensions for visual state

**Key Properties of StepwiseTestRunner**:
- `Buffer` - Current input buffer contents (what user has typed)
- `BufferPosition` - Cursor position within the buffer (0-indexed)
- `CursorColumn` - Actual console cursor column (includes prompt)
- `DisplayedLine` - The full first line of console output (prompt + input)
- `DisplayedInput` - Just the input portion (without prompt)
- `IsMenuVisible` - Whether autocomplete menu is currently displayed
- `SelectedMenuItem` - Currently highlighted menu item text

**FluentAssertions Extensions**:
```csharp
runner.Should().HaveBuffer("server ");           // Assert buffer content
runner.Should().HaveState("server ", 7);         // Assert buffer + position
runner.Should().HaveInputCursorAt(7);            // Assert buffer position
runner.Should().HaveMenuVisible();               // Assert menu is showing
runner.Should().HaveMenuHidden();                // Assert menu is hidden
runner.Should().HaveSelectedMenuItem("connect"); // Assert menu selection
```

**Example - Step-by-step Visual Testing**:
```csharp
[TestMethod]
public async Task TabEnter_InsertsCompletionAtCorrectPosition()
{
    using var runner = CreateRunner();
    runner.Initialize();

    // Step 1: Type "server "
    await runner.TypeText("server ");
    runner.Should().HaveState("server ", 7);
    runner.DisplayedLine.Should().Be("> server ");

    // Step 2: Press Tab to open menu
    await runner.PressKey(ConsoleKey.Tab);
    runner.Should().HaveMenuVisible();
    runner.SelectedMenuItem.Should().Be("connect");

    // Step 3: Press Enter to accept
    await runner.PressKey(ConsoleKey.Enter);
    runner.Should().HaveMenuHidden();
    runner.Buffer.Should().Be("server connect ");
    runner.DisplayedLine.Should().Be("> server connect ");
}
```

**Creating a StepwiseTestRunner**:
```csharp
private StepwiseTestRunner CreateRunner()
{
    var console = new VirtualAnsiConsole().Interactive();
    var registry = CreateTestRegistry();
    var orchestrator = CreateOrchestrator(registry);
    var prompt = new TestPrompt();  // Returns "> "
    var controller = new AutoCompleteController(orchestrator, console, prompt);
    var inputBuilder = new InputBuilder(console, prompt, controller);
    
    return new StepwiseTestRunner(console, inputBuilder, controller, prompt);
}
```

---

## 5. Application-Level Tests

**Pattern**: Build a full `CommandLineApplication` and run commands.

**When to use**: Testing command execution, dependency injection, return values, command chaining.

**Example files**:
- `BitPantry.CommandLine.Tests/CommandLineApplicationTests.cs`
- `BitPantry.CommandLine.Tests/Groups/GroupInvocationTests.cs`

```csharp
[ClassInitialize]
public static void Initialize(TestContext ctx)
{
    _app = new CommandLineApplicationBuilder()
        .RegisterCommand<TestCommand>()
        .RegisterGroup<TestGroup>()
        .Build();
}

[TestMethod]
public void ExecuteCommand_Success()
{
    var result = _app.Run("mycommand --arg value").GetAwaiter().GetResult();
    
    result.ResultCode.Should().Be(RunResultCode.Success);
}
```

---

## 6. Help/Output Formatting Tests

**Pattern**: Capture console output to `StringWriter` and verify formatting.

**When to use**: Testing rendered output appearance, help text, error messages.

**Example files**:
- `BitPantry.CommandLine.Tests/Help/HelpFormatterTests.cs`

```csharp
[TestMethod]
public void FormatHelp_ShowsExpectedContent()
{
    var writer = new StringWriter();
    var console = AnsiConsole.Create(new AnsiConsoleSettings 
    { 
        Out = new AnsiConsoleOutput(writer) 
    });
    
    var formatter = new HelpFormatter();
    formatter.DisplayHelp(console, command);
    
    var output = writer.ToString();
    output.Should().Contain("--myoption");
    output.Should().Contain("Description text");
}
```

---

## Test Command Definition Patterns

When defining test commands, use nested classes within the test class:

```csharp
[TestClass]
public class MyTests
{
    #region Test Commands and Groups

    [Group(Name = "server")]
    [CmdDescription("Server management commands")]
    public class ServerGroup
    {
        // Nested groups use inner classes
        [Group(Name = "profile")]
        [CmdDescription("Profile management")]
        public class ProfileGroup { }
    }

    [Command(Group = typeof(ServerGroup), Name = "connect")]
    [CmdDescription("Connect to server")]
    public class ConnectCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    // Commands in nested groups reference the inner class
    [Command(Group = typeof(ServerGroup.ProfileGroup), Name = "list")]
    [CmdDescription("List profiles")]
    public class ListProfilesCommand : CommandBase
    {
        public void Execute(CommandExecutionContext ctx) { }
    }

    #endregion
    
    // ... tests ...
}
```

---

## Choosing the Right Test Level

Use this decision tree:

1. **Is this a user-facing feature or user flow?**
   - Yes → **MUST have E2E Test** using `VirtualAnsiConsole` + `InputBuilder`
   - Also write unit/component tests for implementation details if helpful

2. **Does it involve console I/O or keyboard input?**
   - Yes → E2E Test (VirtualConsole) is REQUIRED
   - **Need step-by-step debugging?** → Use `StepwiseTestRunner` for visual state inspection
   - No → Continue

3. **Is this a visual/cursor positioning bug?**
   - Yes → Use `StepwiseTestRunner` to trace `DisplayedLine`, `CursorColumn`, buffer state at each keystroke
   - Check `VirtualAnsiConsole` handles `\r` (carriage return) correctly for cursor positioning

4. **Does it involve multiple components working together?**
   - Yes, completion pipeline → Orchestrator Integration Test (PLUS E2E for user flows)
   - Yes, command execution → Application-Level Test
   - No → Continue

5. **Does it need a real CommandRegistry?**
   - Yes → Provider-Level Test
   - No → Unit Test

6. **Does it test rendered output appearance?**
   - Yes → Output Formatting Test

### Test Coverage Checklist

For any feature, ensure:

- [ ] Every happy path has an E2E test
- [ ] Every edge case has an E2E test  
- [ ] Every error condition has an E2E test
- [ ] Bug fixes include an E2E test that reproduces the bug
- [ ] E2E tests use `VirtualConsoleInput.PushText()/PushKey()` for real input simulation
- [ ] E2E tests assert on `builder.GetInput()` result AND console buffer content when relevant
- [ ] Visual/cursor bugs use `StepwiseTestRunner` to validate `DisplayedLine` and `CursorColumn` at each step

### Known VirtualAnsiConsole Behaviors

- **Carriage Return (`\r`)**: Moves cursor to column 0 of current line (not next line)
- **Newline (`\n` or `\r\n`)**: Moves cursor to column 0 of next line
- **2D Buffer**: Access via `console.Buffer` or `console.GetLine(lineNumber)`
- **Cursor Position**: Track with `console.GetCursorPosition()` returns `(column, line)`
