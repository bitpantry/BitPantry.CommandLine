# Feature Specification: VirtualConsole Autocomplete Tests

**Feature Branch**: `012-virtualconsole-autocomplete-tests`  
**Created**: January 6, 2026  
**Status**: Draft  
**Input**: Replace all current autocomplete tests with new tests using VirtualConsole, removing all existing visual/UX and snapshot testing infrastructure.

## Clarifications

### Session 2026-01-06

- Q: What diagnostic information should be included in test failure output? → A: Stack trace + VirtualConsole buffer rendered as ASCII (via existing GetScreenContent()); no additional input history tracking required
- Q: How should mock filesystem be configured for file path completion tests? → A: Use existing System.IO.Abstractions.TestingHelpers MockFileSystem (already in test project via TestableIO.System.IO.Abstractions.TestingHelpers package)
- Q: How should async completion provider tests handle delays and cancellation? → A: Use existing Moq-based Mock<ICompletionProvider> pattern with CancellationTokenSource for timeout/cancellation testing (already established in test project)
- Q: Where should VirtualConsole.Testing extensions live? → A: Separate BitPantry.VirtualConsole.Testing project to preserve zero-dependency core VirtualConsole package

## Overview

This specification defines the complete replacement of the existing autocomplete test suite with a new VirtualConsole-based testing approach. The existing tests use StepwiseTestRunner, ConsolidatedTestConsole, and snapshot/Verify infrastructure that will be completely removed. The new tests will use the `BitPantry.VirtualConsole` project, with a new testing extension layer (`BitPantry.VirtualConsole.Testing`) that provides test-specific utilities while keeping the core VirtualConsole project clean for other use cases.

### Scope

**In Scope:**
- Complete removal of existing autocomplete tests and visual testing infrastructure
- Creation of VirtualConsole.Testing extension project
- Implementation of 283 documented test cases from autocomplete-test-cases.md
- Update to CLAUDE.md and testing documentation

**Out of Scope:**
- Changes to autocomplete production code (tests should validate current behavior)
- Non-autocomplete tests (parsing, resolution, activation, etc.)
- Remote/SignalR integration tests (separate test project)

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Test Developer Validates Autocomplete Ghost Text (Priority: P1)

A test developer needs to verify that ghost text (inline suggestions) appears correctly when users type partial commands. They write a test that simulates typing keystrokes, and asserts that the VirtualConsole screen buffer contains ghost text in the expected position with the correct dim/gray styling.

**Why this priority**: Ghost text is fundamental to the autocomplete experience. If tests cannot verify ghost text appearance, styling, and acceptance, the core feature cannot be validated.

**Independent Test**: Can be fully tested by typing partial text (e.g., "ser") and asserting that ghost text "ver" appears after the cursor in dim gray style.

**Acceptance Scenarios**:

1. **Given** an empty prompt with command "server" registered, **When** the test types "s", **Then** the VirtualConsole buffer shows "s" in normal style followed by "erver" in dim gray style
2. **Given** ghost text is displayed, **When** the test simulates Right Arrow key, **Then** the buffer shows completed text "server" and ghost text is cleared
3. **Given** ghost text is displayed, **When** the test types a non-matching character, **Then** ghost text disappears from the buffer

---

### User Story 2 - Test Developer Validates Autocomplete Menu (Priority: P1)

A test developer needs to verify that the autocomplete menu appears correctly when Tab is pressed, that it displays items with proper highlighting, and that navigation works correctly. They write tests that simulate Tab press, arrow key navigation, and verify menu state through VirtualConsole assertions.

**Why this priority**: The autocomplete menu is the primary discovery mechanism. Tests must verify menu rendering, navigation, selection highlighting, and item acceptance.

**Independent Test**: Can be fully tested by pressing Tab, asserting menu is visible with correct items, navigating with arrow keys, and accepting with Enter.

**Acceptance Scenarios**:

1. **Given** an empty prompt, **When** the test presses Tab, **Then** the VirtualConsole buffer shows a menu with all root commands, first item highlighted with reverse style
2. **Given** menu is open with 10+ items, **When** the test navigates down past viewport, **Then** the buffer shows scroll indicators
3. **Given** menu is open with item selected, **When** the test presses Enter, **Then** selected item is inserted into buffer and menu is cleared from display
4. **Given** menu is open, **When** the test presses Escape, **Then** menu disappears and buffer is unchanged

---

### User Story 3 - Test Developer Validates Menu Filtering (Priority: P1)

A test developer needs to verify that typing while the menu is open filters the displayed items in real-time. Tests simulate typing filter text and assert that the VirtualConsole shows only matching items with the matched portion highlighted.

**Why this priority**: Real-time filtering is essential for usability with large completion sets. Tests must verify filter application, match highlighting, and state updates.

**Independent Test**: Can be fully tested by opening menu, typing filter characters, and asserting filtered results with highlighted match portions.

**Acceptance Scenarios**:

1. **Given** menu is open showing 10 items, **When** the test types "con", **Then** the buffer shows only items containing "con" with that substring highlighted
2. **Given** filtered menu showing 3 items, **When** the test presses Backspace, **Then** more items appear as filter is relaxed
3. **Given** filter produces no matches, **When** the test inspects the buffer, **Then** "(no matches)" message is visible

---

### User Story 4 - Test Developer Validates Argument Completion (Priority: P2)

A test developer needs to verify that argument name completion works after typing "--" or "-", that used arguments are excluded, and that argument values complete correctly based on provider configuration.

**Why this priority**: Argument completion is the second-tier functionality after command completion. Tests must verify the full argument completion workflow.

**Independent Test**: Can be fully tested by typing "command --" and Tab, asserting argument names appear, then verifying used arguments are excluded on subsequent Tab.

**Acceptance Scenarios**:

1. **Given** command with --host and --port arguments, **When** the test types "command --" and Tab, **Then** menu shows "--host" and "--port"
2. **Given** "--host value" already in buffer, **When** the test types "--" and Tab, **Then** "--host" is NOT in the menu
3. **Given** argument with enum type, **When** the test types "--format " and Tab, **Then** enum values appear as completions

---

### User Story 5 - Test Developer Validates Positional Completion (Priority: P2)

A test developer needs to verify that positional argument slots are completed correctly, that subsequent positions complete after prior positions are filled, and that IsRest variadic positionals continue offering completions.

**Why this priority**: Positional arguments are a key usability feature enabling natural CLI syntax.

**Independent Test**: Can be fully tested by typing command with positional provider, Tab for first position, then Tab for second position.

**Acceptance Scenarios**:

1. **Given** command with positional Source argument, **When** the test types "copy " and Tab, **Then** source completions appear
2. **Given** first positional filled, **When** the test types second positional prefix and Tab, **Then** destination completions appear
3. **Given** command with IsRest positional, **When** multiple values provided and Tab pressed, **Then** additional completions still offered

---

### User Story 6 - Test Developer Validates File Path Completion (Priority: P2)

A test developer needs to verify that file path completion shows files/directories from the filesystem, handles subdirectory navigation, and properly escapes paths with spaces.

**Why this priority**: File path completion is a common use case that requires filesystem interaction testing.

**Independent Test**: Can be fully tested using a mock filesystem, typing partial path, and asserting file/directory completions appear.

**Acceptance Scenarios**:

1. **Given** mock filesystem with files, **When** the test types "--file " and Tab, **Then** files and directories appear in menu
2. **Given** subdirectory exists, **When** the test types "src/" and Tab, **Then** contents of src/ appear
3. **Given** file with spaces exists, **When** the test accepts completion, **Then** path is properly quoted in buffer

---

### User Story 7 - Test Developer Validates Edge Cases (Priority: P3)

A test developer needs to verify edge cases like rapid keystrokes, special characters, Unicode handling, and boundary conditions don't cause crashes or corruption.

**Why this priority**: Edge case coverage prevents production bugs but is lower priority than core functionality.

**Independent Test**: Can be fully tested by simulating edge case scenarios and asserting no exceptions and consistent state.

**Acceptance Scenarios**:

1. **Given** rapid Tab presses, **When** state is inspected, **Then** no race conditions or inconsistent state
2. **Given** Unicode command names, **When** menu is displayed, **Then** Unicode renders correctly
3. **Given** very long completion value, **When** menu is displayed, **Then** value is truncated with ellipsis

---

### User Story 8 - Test Developer Removes Legacy Test Infrastructure (Priority: P1)

Before implementing new tests, the existing visual/UX test infrastructure must be completely removed. This includes all files in AutoComplete test directories, VirtualConsole test helpers (StepwiseTestRunner, ConsolidatedTestConsole), and snapshot testing infrastructure.

**Why this priority**: Clean removal is prerequisite to implementing the new test approach without conflicts.

**Independent Test**: Verified by building solution after removal - no orphaned references or build failures.

**Acceptance Scenarios**:

1. **Given** existing AutoComplete test files, **When** removal is complete, **Then** BitPantry.CommandLine.Tests/AutoComplete/ directory is empty or removed
2. **Given** existing VirtualConsole test helpers, **When** removal is complete, **Then** BitPantry.CommandLine.Tests/VirtualConsole/ directory is removed
3. **Given** existing snapshot tests, **When** removal is complete, **Then** BitPantry.CommandLine.Tests/Snapshots/ directory is removed
4. **Given** documentation references to old testing approach, **When** update is complete, **Then** CLAUDE.md reflects new VirtualConsole testing approach

---

### Edge Cases

- What happens when VirtualConsole dimensions are smaller than menu size?
- How does testing handle async completion providers with mocked delays?
- What happens when simulated input arrives faster than typical human typing?
- How are cursor position assertions affected by prompt segments of varying lengths?

---

## Requirements *(mandatory)*

### Functional Requirements

#### Legacy Removal (Phase 1)

- **FR-001**: System MUST remove all files in `BitPantry.CommandLine.Tests/AutoComplete/` directory
- **FR-002**: System MUST remove all files in `BitPantry.CommandLine.Tests/VirtualConsole/` directory
- **FR-003**: System MUST remove all files in `BitPantry.CommandLine.Tests/Snapshots/` directory
- **FR-004**: System MUST remove Verify.MSTest and related snapshot testing packages from test project
- **FR-005**: System MUST update CLAUDE.md to remove references to old testing infrastructure

#### VirtualConsole.Testing Extension (Phase 2)

- **FR-010**: System MUST create a separate `BitPantry.VirtualConsole.Testing` project for testing extensions (preserves zero-dependency core)
- **FR-011**: Testing project MUST reference BitPantry.VirtualConsole, FluentAssertions, and Spectre.Console
- **FR-012**: System MUST provide `IKeyboardSimulator` interface for simulating keyboard input to the autocomplete system
- **FR-013**: System MUST provide `AutoCompleteTestHarness` class that coordinates VirtualConsole, keyboard simulation, and autocomplete controller
- **FR-014**: System MUST provide FluentAssertions-style extension methods for asserting VirtualConsole state (e.g., `console.Should().ContainText("server")`)
- **FR-015**: System MUST provide cell-level style assertions (e.g., `console.Should().HaveCellWithStyle(row, col, CellAttributes.Dim)`)
- **FR-016**: System MUST provide menu-specific assertions (e.g., `harness.Should().HaveMenuVisible()`, `harness.Should().HaveSelectedItem("connect")`)
- **FR-017**: System MUST provide ghost text assertions (e.g., `harness.Should().HaveGhostText("erver")`)
- **FR-018**: System MUST provide input buffer assertions (e.g., `harness.Should().HaveBuffer("server connect")`)
- **FR-019**: Assertion failure messages MUST include ASCII-rendered VirtualConsole buffer via existing GetScreenContent() method

#### Test Implementation (Phase 3)

- **FR-020**: System MUST implement all 16 ghost text behavior tests (TC-1.1 through TC-1.16)
- **FR-021**: System MUST implement all 18 menu display & navigation tests (TC-2.1 through TC-2.18)
- **FR-022**: System MUST implement all 15 menu filtering tests (TC-3.1 through TC-3.15)
- **FR-023**: System MUST implement all 10 input editing tests (TC-4.1 through TC-4.10)
- **FR-024**: System MUST implement all 4 command & group completion tests (TC-5.1 through TC-5.4)
- **FR-025**: System MUST implement all 10 argument name & alias tests (TC-6.1 through TC-6.10)
- **FR-026**: System MUST implement all 10 argument value tests (TC-7.1 through TC-7.10)
- **FR-027**: System MUST implement all 11 positional argument tests (TC-8.1 through TC-8.11)
- **FR-028**: System MUST implement all 12 file path tests (TC-9.1 through TC-9.12)
- **FR-029**: System MUST implement all 5 viewport scrolling tests (TC-10.1 through TC-10.5)
- **FR-030**: System MUST implement all 3 ghost & menu interaction tests (TC-11.1 through TC-11.3)
- **FR-031**: System MUST implement all 4 multi-step workflow tests (TC-12.1 through TC-12.4)
- **FR-032**: System MUST implement all 4 history navigation tests (TC-13.1 through TC-13.4)
- **FR-033**: System MUST implement all 27 edge case tests (TC-14.1 through TC-14.27)
- **FR-034**: System MUST implement all 5 visual rendering tests (TC-15.1 through TC-15.5)
- **FR-035**: System MUST implement all 3 submission behavior tests (TC-16.1 through TC-16.3)
- **FR-036**: System MUST implement caching behavior tests (TC-18.1 through TC-18.7)
- **FR-037**: System MUST implement all 20 provider & attribute configuration tests (TC-19.1 through TC-19.20)
- **FR-038**: System MUST implement all 5 match ranking & ordering tests (TC-20.1 through TC-20.5)
- **FR-039**: System MUST implement all 5 result limiting & truncation tests (TC-21.1 through TC-21.5)
- **FR-040**: System MUST implement all 6 terminal & environment edge case tests (TC-22.1 through TC-22.6)
- **FR-041**: System MUST implement all 8 keyboard variation tests (TC-23.1 through TC-23.8)
- **FR-042**: System MUST implement all 6 context sensitivity tests (TC-24.1 through TC-24.6)
- **FR-043**: System MUST implement all 5 concurrent & async behavior tests (TC-25.1 through TC-25.5)
- **FR-044**: System MUST implement all 8 quoting & escaping tests (TC-26.1 through TC-26.8)
- **FR-045**: System MUST implement all 5 state persistence tests (TC-30.1 through TC-30.5)
- **FR-046**: System MUST implement all 5 completion source interaction tests (TC-31.1 through TC-31.5)
- **FR-047**: System MUST implement all 6 VirtualConsole integration tests (TC-32.1 through TC-32.6)
- **FR-048**: System MUST implement all 5 configuration & settings tests (TC-33.1 through TC-33.5)
- **FR-049**: System MUST implement all 5 error message & feedback tests (TC-34.1 through TC-34.5)
- **FR-050**: System MUST implement all 6 boundary value tests (TC-35.1 through TC-35.6)

#### Documentation (Phase 4)

- **FR-060**: CLAUDE.md MUST document the VirtualConsole testing approach
- **FR-061**: CLAUDE.md MUST include examples of writing VirtualConsole-based autocomplete tests
- **FR-062**: System MUST create testing-focused README in the VirtualConsole.Testing namespace

### Key Entities

- **VirtualConsole**: Core virtual terminal emulator that processes ANSI sequences and maintains screen buffer (existing)
- **AutoCompleteTestHarness**: Coordinates VirtualConsole with autocomplete controller and keyboard simulation
- **KeyboardSimulator**: Provides methods to simulate keypresses and key sequences
- **VirtualConsoleAssertions**: FluentAssertions extension class for screen buffer assertions
- **HarnessAssertions**: FluentAssertions extension class for autocomplete state assertions
- **TestCommand**: Base class for test commands with configurable completions

---

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All 283 documented test cases from autocomplete-test-cases.md are implemented as passing tests
- **SC-002**: Legacy test infrastructure is completely removed (0 files remaining in AutoComplete/Visual, VirtualConsole helpers, Snapshots)
- **SC-003**: Solution builds successfully after legacy removal with no orphaned references
- **SC-004**: Test execution completes in under 60 seconds for the full autocomplete test suite
- **SC-005**: Each test method tests exactly one documented hypothesis from the test case document
- **SC-006**: VirtualConsole.Testing is a separate project from core VirtualConsole (zero-dependency core preserved)
- **SC-007**: CLAUDE.md testing section is updated to reflect VirtualConsole testing approach
- **SC-008**: New tests discover at least 5 bugs in existing autocomplete implementation (tests should test the hypothesis, not the code)
- **SC-009**: 100% of tests that fail do so because the code doesn't match the documented behavior, not because the test is wrong

---

## Design Constraints

- Testing extensions MUST reside in a separate `BitPantry.VirtualConsole.Testing` project (not within core VirtualConsole) to preserve zero-dependency design
- Tests MUST NOT modify autocomplete production code - they validate existing behavior against documented expectations
- Tests MUST use mock/fake filesystems via System.IO.Abstractions for file path completion testing
- Tests MUST NOT require network access - remote completion tests use mocked server proxies
- VirtualConsole.Testing MUST have no dependencies beyond FluentAssertions (for assertion extensions)

---

## Assumptions

- The existing VirtualConsole project is feature-complete for testing needs (processes all required ANSI sequences)
- FluentAssertions is already available as a test dependency
- MSTest is the test framework (existing pattern)
- The autocomplete production code may have bugs - tests should validate against documented behavior, not current implementation
- Some tests may initially fail if the code doesn't match documented behavior - this is expected and desirable

---

## Test Implementation Approach

### Test Case to Test Method Mapping

Each test case in autocomplete-test-cases.md (e.g., TC-1.1, TC-2.5) maps to exactly one test method. Test methods should be named descriptively based on the When/Then hypothesis:

```csharp
[TestMethod]
public async Task TC_1_1_SingleCharacter_ShowsGhostCompletion()
{
    // Given: empty prompt with "server" command registered
    using var harness = CreateHarness();
    
    // When: user types "s"
    await harness.TypeText("s");
    
    // Then: ghost text "erver" appears in dim style
    harness.Should().HaveGhostText("erver");
    harness.VirtualConsole.GetCell(0, 3).Style.Attributes
        .Should().HaveFlag(CellAttributes.Dim);
}
```

### Test Organization

```text
BitPantry.CommandLine.Tests/
└── AutoComplete/
    ├── GhostTextTests.cs          (TC-1.1 through TC-1.16)
    ├── MenuNavigationTests.cs     (TC-2.1 through TC-2.18)
    ├── MenuFilteringTests.cs      (TC-3.1 through TC-3.15)
    ├── InputEditingTests.cs       (TC-4.1 through TC-4.10)
    ├── CommandCompletionTests.cs  (TC-5.1 through TC-5.4)
    ├── ArgumentNameTests.cs       (TC-6.1 through TC-6.10)
    ├── ArgumentValueTests.cs      (TC-7.1 through TC-7.10)
    ├── PositionalTests.cs         (TC-8.1 through TC-8.11)
    ├── FilePathTests.cs           (TC-9.1 through TC-9.12)
    ├── ViewportScrollingTests.cs  (TC-10.1 through TC-10.5)
    ├── GhostMenuInteractionTests.cs (TC-11.1 through TC-11.3)
    ├── WorkflowTests.cs           (TC-12.1 through TC-12.4)
    ├── HistoryNavigationTests.cs  (TC-13.1 through TC-13.4)
    ├── EdgeCaseTests.cs           (TC-14.1 through TC-14.27)
    ├── VisualRenderingTests.cs    (TC-15.1 through TC-15.5)
    ├── SubmissionTests.cs         (TC-16.1 through TC-16.3)
    ├── CachingTests.cs            (TC-18.1 through TC-18.7)
    ├── ProviderConfigTests.cs     (TC-19.1 through TC-19.20)
    ├── MatchRankingTests.cs       (TC-20.1 through TC-20.5)
    ├── ResultLimitingTests.cs     (TC-21.1 through TC-21.5)
    ├── TerminalEdgeCaseTests.cs   (TC-22.1 through TC-22.6)
    ├── KeyboardVariationTests.cs  (TC-23.1 through TC-23.8)
    ├── ContextSensitivityTests.cs (TC-24.1 through TC-24.6)
    ├── AsyncBehaviorTests.cs      (TC-25.1 through TC-25.5)
    ├── QuotingEscapingTests.cs    (TC-26.1 through TC-26.8)
    ├── StatePersistenceTests.cs   (TC-30.1 through TC-30.5)
    ├── ProviderInteractionTests.cs (TC-31.1 through TC-31.5)
    ├── VirtualConsoleIntegrationTests.cs (TC-32.1 through TC-32.6)
    ├── ConfigurationTests.cs      (TC-33.1 through TC-33.5)
    ├── ErrorFeedbackTests.cs      (TC-34.1 through TC-34.5)
    └── BoundaryValueTests.cs      (TC-35.1 through TC-35.6)

BitPantry.VirtualConsole/
└── Testing/
    ├── README.md
    ├── AutoCompleteTestHarness.cs
    ├── IKeyboardSimulator.cs
    ├── KeyboardSimulator.cs
    ├── VirtualConsoleAssertions.cs
    ├── HarnessAssertions.cs
    └── TestCommandBase.cs
```
