# Feature Specification: BitPantry.VirtualConsole

**Feature Branch**: `011-virtual-console`  
**Created**: 2026-01-04  
**Status**: Draft  
**Input**: User description: "BitPantry.VirtualConsole - A virtual console package for automated testing of .NET CLI applications. Provides screen buffer simulation with ANSI escape sequence processing to enable point-in-time visual state assertions in unit tests."

## Clarifications

### Session 2026-01-04

- Q: What is the maximum expected screen buffer size the VirtualConsole must support? → A: No package-imposed limit; fully configurable by implementer based on their hardware/requirements.

## Overview

BitPantry.VirtualConsole is a standalone package that provides a virtual terminal emulator for automated testing of .NET command-line applications. Unlike traditional console output capture (which accumulates all output ever written), this package maintains a true 2D screen buffer that processes ANSI escape sequences—including cursor movement, color codes, and text overwrites—to represent what a user would actually see on screen at any point in time.

**The Gap**: Web developers have Selenium for full functional testing—a headless browser that renders pages, accepts simulated input, and enables assertions on visual/DOM state. CLI developers have no equivalent. They're stuck checking stdout strings, like testing web apps with regex on saved HTML.

**VirtualConsole fills this gap**: It's the "Selenium for CLI apps"—a headless terminal that renders output, will accept simulated input (future), and enables assertions on visual screen state at any point in time.

**Primary Use Case**: Enable CLI test authors to write assertions against the current visual state of the console, not just accumulated output. This catches bugs where correct output is overwritten by incorrect output—scenarios that cumulative output testing cannot detect.

**Package Strategy**: Initially built within the BitPantry.CommandLine solution as an internal tool, then extracted to a standalone NuGet package for consumption by other .NET CLI implementers.

## Architecture Vision

### Current Focus: Testing Infrastructure

The immediate goal is enabling point-in-time visual state assertions for CLI testing. The VirtualConsole receives output, processes ANSI sequences, and exposes a query API for test validation.

### Future Vision: Full Headless Terminal

The VirtualConsole is architected to eventually become a **complete headless terminal** that can:

1. **BE the console**: Implement `IAnsiConsole` (Spectre.Console) so applications can use VirtualConsole as their output target instead of the real console
2. **Accept input**: Provide an input queue API (`QueueKeyPress()`, `QueueText()`) so automation software can drive CLI applications programmatically
3. **Enable CLI automation**: Scripts and bots driving CLI apps without needing a real terminal
4. **Support end-to-end testing**: Full user session simulation, not just output verification

```
                    ┌─────────────────────────┐
  Automation ──────►│    VirtualConsole       │◄────── CLI App
  (sends input)     │   (Headless Terminal)   │        (writes output)
                    │                         │
                    │  ┌─────────────────┐    │
                    │  │  Input Queue    │    │
                    │  └─────────────────┘    │
                    │  ┌─────────────────┐    │
                    │  │  Screen Buffer  │    │
                    │  └─────────────────┘    │
                    │  ┌─────────────────┐    │
                    │  │  Query API      │    │
                    │  └─────────────────┘    │
                    └─────────────────────────┘
```

### Architectural Constraints

Every implementation decision MUST consider the future headless terminal capability:

- **AC-001**: Core classes MUST be designed with input handling extension points (even if not implemented)
- **AC-002**: Screen buffer MUST be separable from output processing (future input handling may affect screen state)
- **AC-003**: Public API MUST NOT preclude later implementation of `IAnsiConsole`
- **AC-004**: Architecture MUST support future input queue without breaking changes to existing consumers
- **AC-005**: Design reviews MUST include "does this block headless terminal?" checkpoint

**Package Boundaries**: 

- **BitPantry.VirtualConsole** (this spec): Core virtual terminal with screen buffer, ANSI processing, and query APIs. Zero dependencies on test frameworks.
- **BitPantry.VirtualConsole.Tests**: Comprehensive test suite for the VirtualConsole package. Built as a separate project that can be extracted alongside the main package.
- **Assertion helpers** (out of scope): FluentAssertions extensions and test utilities are the responsibility of consuming projects. The CommandLine solution will build these in its test project, but they are not part of the VirtualConsole package.

## Development Methodology

### Strict TDD Requirement

All features in BitPantry.VirtualConsole MUST be developed using strict Test-Driven Development:

1. **RED**: Write a failing test that describes the expected behavior
2. **GREEN**: Write the minimum code to make the test pass
3. **REFACTOR**: Improve the code while keeping tests green

No production code may be written without a corresponding failing test first. This is non-negotiable because:
- The entire purpose of this package is to enable reliable automated testing
- A testing library that isn't thoroughly tested would be ironic and untrustworthy
- TDD ensures the API is designed from the consumer's perspective

### Test Comprehensiveness Requirements

Tests MUST cover:

1. **Individual sequences**: Each supported ANSI sequence tested in isolation
2. **Sequence combinations**: Multiple sequences interacting (e.g., cursor move + color change + write + reset)
3. **Real-world complexity**: Tests simulating actual CLI output patterns including:
   - Menu rendering with selection highlighting and filter matches
   - Progress bars with in-place updates
   - Multi-line layouts with styled regions
   - Spectre.Console-style output (tables, panels, trees)
4. **Edge cases**: Boundary conditions, malformed input, screen wrapping
5. **State consistency**: Verify screen state is correct after complex operation sequences

### Complexity Milestone Tests

The test suite MUST include integration tests that validate handling of complex real-world scenarios:

- **Menu Test**: Render a multi-item menu with selection inversion, filter highlighting, viewport scrolling
- **Progress Bar Test**: Simulate a progress bar updating in-place with percentage, bar graphic, and ETA
- **Table Test**: Render a Spectre.Console-style table with borders, headers, and styled cells
- **Multi-Region Test**: Multiple styled regions updating independently (like a dashboard layout)

These milestone tests prove the VirtualConsole can handle production CLI output, not just toy examples.

### First Use Case: Detecting Overwrite Regressions

The core value VirtualConsole provides is detecting when an overwrite changes styling that should have persisted. Current test infrastructure cannot detect this because it accumulates all output - old (correct) output masks new (buggy) output.

**The Problem** (what we can't test today):

```
// Right hand writes blue text
console.Write("\x1b[34mHello\x1b[0m");  

// Left hand checks - PASS (blue text exists in output log)
Assert.Contains(console.Output, blueAnsiSequence);

// Right hand overwrites same location with plain text (a bug!)
console.Write("\x1b[H");  // cursor home
console.Write("Hello");   // no blue - this is wrong!

// Left hand checks - STILL PASSES (old blue output still in log)
Assert.Contains(console.Output, blueAnsiSequence);  // ← BUG NOT DETECTED
```

**The Solution** (what VirtualConsole enables):

```
// Right hand writes blue text
virtualConsole.Write("\x1b[34mHello\x1b[0m");

// Left hand checks current screen state - PASS
Assert.Equal(ConsoleColor.Blue, virtualConsole.GetCell(0, 0).ForegroundColor);

// Right hand overwrites same location with plain text (a bug!)
virtualConsole.Write("\x1b[H");  // cursor home  
virtualConsole.Write("Hello");   // no blue

// Left hand checks current screen state - FAILS (bug detected!)
Assert.Equal(ConsoleColor.Blue, virtualConsole.GetCell(0, 0).ForegroundColor);  // ← CAUGHT!
```

**Key Insight**: VirtualConsole's 2D screen buffer reflects the CURRENT visual state, not cumulative history. When code overwrites a location, the old content is gone - just like a real terminal.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Basic Screen State Verification (Priority: P1)

As a test author, I want to query the current visual state of the console after a series of operations, so that I can verify what the user would actually see on screen—not just what was ever written.

**Why this priority**: This is the core value proposition. Without point-in-time screen state, the package provides no benefit over existing solutions.

**Independent Test**: Can be fully tested by writing output, moving cursor, overwriting text, then querying screen content. Delivers the fundamental capability of "what does the screen look like now?"

**Acceptance Scenarios**:

1. **Given** an empty virtual console, **When** I write "Hello" then move cursor left 3 positions and write "XYZ", **Then** querying the screen shows "HeXYZ" (not "HelloXYZ")
2. **Given** output has been written to the console, **When** I query a specific row, **Then** I receive the current content of that row with any applied styling information
3. **Given** a virtual console with content, **When** I clear the screen, **Then** subsequent queries return empty content

---

### User Story 2 - ANSI Color and Style Tracking (Priority: P1)

As a test author, I want the virtual console to track text styling (colors, bold, invert, etc.), so that I can verify styled output like highlighted filter matches or selected menu items.

**Why this priority**: Testing CLI applications requires verifying not just text content but visual styling. This is essential for the immediate use case (menu filter highlighting bug).

**Independent Test**: Can be tested by writing styled text and querying both content and style attributes for specific screen positions.

**Acceptance Scenarios**:

1. **Given** text written with blue foreground color, **When** I query that screen position, **Then** I can verify the text has blue foreground styling
2. **Given** text written with inverted styling (for selection highlight), **When** I query that position, **Then** I can verify the invert attribute is set
3. **Given** a style reset code is processed, **When** subsequent text is written, **Then** it has default styling

---

### User Story 3 - Cursor Movement Processing (Priority: P1)

As a test author, I want the virtual console to correctly process cursor movement ANSI sequences, so that re-renders and in-place updates are handled accurately.

**Why this priority**: CLI applications frequently use cursor movement for menus, progress bars, and dynamic updates. Without this, the screen buffer would be incorrect.

**Independent Test**: Can be tested by writing text, sending cursor movement codes, writing more text, and verifying final screen state reflects correct cursor behavior.

**Acceptance Scenarios**:

1. **Given** content on the screen, **When** cursor-up sequences are processed, **Then** subsequent writes overwrite content at the higher row
2. **Given** content on the screen, **When** cursor-forward/back sequences are processed, **Then** subsequent writes occur at the correct column
3. **Given** a carriage return is processed, **When** text is written, **Then** it overwrites from the beginning of the current line

---

### User Story 4 - Row and Cell Queries with Style (Priority: P2)

As a test author, I want to query individual cells or entire rows and get both content and styling, so that I can make specific assertions about what appears and how it's styled.

**Why this priority**: Builds on P1 stories to provide the query API needed for test assertions.

**Independent Test**: Can be tested by writing styled content and using query methods to retrieve content with styling metadata.

**Acceptance Scenarios**:

1. **Given** a row with mixed styling, **When** I query that row, **Then** I receive an object that contains both the text and styling information for each character
2. **Given** a specific cell position, **When** I query that cell, **Then** I receive the character and its complete style (foreground, background, attributes)
3. **Given** an empty region of the screen, **When** I query cells there, **Then** I receive space characters with default styling

---

### User Story 5 - Clean API for External Consumers (Priority: P2)

As a library consumer (test framework, IDE tool, or other application), I want the VirtualConsole to expose a clean query API without test framework dependencies, so that I can build my own tooling on top of it.

**Why this priority**: The package must be general-purpose and not tied to any specific test framework. Assertion helpers belong in consuming code or a separate companion package.

**Independent Test**: Can be tested by verifying the public API surface has no dependencies on test frameworks and provides sufficient data for external tools to build assertions.

**Acceptance Scenarios**:

1. **Given** the VirtualConsole package, **When** I inspect its dependencies, **Then** it has no references to MSTest, xUnit, NUnit, FluentAssertions, or similar test libraries
2. **Given** I want to build custom assertions, **When** I query the screen state, **Then** I receive sufficient data (content, styles, positions) to implement any assertion logic externally
3. **Given** I'm building an IDE integration, **When** I use the VirtualConsole API, **Then** I can access all screen state without needing test framework concepts

---

### User Story 6 - Extensible ANSI Sequence Handling (Priority: P3)

As a package maintainer, I want the ANSI sequence processing to be extensible, so that additional escape sequences can be supported over time without breaking changes.

**Why this priority**: Important for long-term maintainability, but initial release can support a focused subset of sequences.

**Independent Test**: Can be tested by registering a custom sequence handler and verifying it is invoked for matching sequences.

**Acceptance Scenarios**:

1. **Given** a custom sequence handler is registered, **When** that sequence is encountered, **Then** the custom handler is invoked
2. **Given** an unrecognized sequence is encountered, **When** processing continues, **Then** the system throws an exception with details about the unrecognized sequence
3. **Given** the package is updated with new sequence support, **Then** existing tests continue to work without modification

---

### User Story 7 - Configurable Screen Dimensions (Priority: P3)

As a test author, I want to configure the virtual console dimensions, so that I can test scenarios that depend on specific terminal sizes (line wrapping, viewport calculations).

**Why this priority**: Nice to have for completeness, but most tests work with default dimensions.

**Independent Test**: Can be tested by creating consoles with different dimensions and verifying content wraps and clips correctly.

**Acceptance Scenarios**:

1. **Given** a virtual console with 80x25 dimensions, **When** content is written past column 80, **Then** it wraps to the next line
2. **Given** a virtual console with custom dimensions, **When** I query the dimensions, **Then** they match the configured values
3. **Given** content written beyond the screen height, **When** querying, **Then** content scrolls appropriately (or clips, per configuration)

---

### User Story 8 - Handles Real-World CLI Complexity (Priority: P2)

As a test author working with complex CLI applications, I want the VirtualConsole to correctly handle the output complexity of real applications (progress bars, tables, multi-region layouts), so that I can trust it for production testing scenarios.

**Why this priority**: The package must prove it handles real complexity, not just simple examples. Without this confidence, adoption would be risky.

**Independent Test**: Can be tested by capturing actual Spectre.Console output (tables, progress bars, layouts) and verifying VirtualConsole produces the correct screen state.

**Acceptance Scenarios**:

1. **Given** Spectre.Console table output with borders and styled cells, **When** processed by VirtualConsole, **Then** the screen buffer contains the correct characters and styles at each position
2. **Given** a progress bar updating in-place (cursor up, overwrite, cursor restore), **When** processed through multiple update cycles, **Then** the final screen state shows only the latest progress (no ghosting from previous states)
3. **Given** a complex menu with inverted selection, blue filter highlighting, and viewport scrolling, **When** the user navigates and filters, **Then** each intermediate screen state is queryable and correct

---

### Edge Cases

- What happens when cursor is moved beyond screen boundaries? (Clamp to edges)
- How does the system handle malformed or unrecognized ANSI sequences? (Throw exception with sequence details)
- What happens with very long lines without wrapping configured? (Truncate at width)
- How are null characters or control characters (non-ANSI) handled? (Treat as single-width characters or ignore per standard terminal behavior)
- What happens when screen buffer is queried before any writes? (Return empty/space-filled content)

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST maintain a 2D character buffer representing the current screen state
- **FR-002**: System MUST track cursor position (row and column) and update it correctly for all operations
- **FR-003**: System MUST process ANSI CSI cursor movement sequences (CUU, CUD, CUF, CUB - cursor up/down/forward/back)
- **FR-004**: System MUST process carriage return (\r) to move cursor to column 0
- **FR-005**: System MUST process newline (\n) to move cursor to next row
- **FR-006**: System MUST process ANSI SGR sequences for foreground color (including 256-color mode)
- **FR-007**: System MUST process ANSI SGR sequences for background color (including 256-color mode)
- **FR-008**: System MUST process ANSI SGR attribute sequences (bold, dim, italic, underline, blink, invert, hidden, strikethrough)
- **FR-009**: System MUST process SGR reset (code 0) to return to default styling
- **FR-010**: System MUST allow querying individual cell content and style by row/column
- **FR-011**: System MUST allow querying entire row content with associated styles
- **FR-012**: System MUST allow querying the full screen as a string (with or without ANSI codes)
- **FR-013**: System MUST support configurable screen dimensions with sensible defaults (80x25); no package-imposed maximum limits
- **FR-014**: System MUST clamp cursor to screen boundaries (no negative positions, no exceeding dimensions)
- **FR-015**: System MUST handle line wrapping when content exceeds screen width
- **FR-016**: System MUST throw an exception when an unrecognized ANSI sequence is encountered, including the sequence in the error message
- **FR-017**: System MUST expose current cursor position via public API
- **FR-018**: System MUST handle screen clear sequences (ED - Erase Display)
- **FR-019**: System MUST handle line clear sequences (EL - Erase Line)
- **FR-020**: System MUST NOT have dependencies on test frameworks (assertion helpers are out of scope for core package)

### Test Project Requirements

- **TR-001**: Test project MUST be named BitPantry.VirtualConsole.Tests and structured for standalone extraction
- **TR-002**: All production code MUST have corresponding tests written BEFORE the implementation (TDD)
- **TR-003**: Tests MUST cover individual ANSI sequences in isolation
- **TR-004**: Tests MUST cover combinations of sequences (cursor + color + write + reset patterns)
- **TR-005**: Tests MUST include complexity milestone tests: menu rendering, progress bar, table layout, multi-region dashboard
- **TR-006**: Tests MUST verify screen state correctness after complex operation sequences (not just final state)
- **TR-007**: Tests MUST validate that unknown sequences throw exceptions with helpful error messages
- **TR-008**: Tests MUST cover edge cases: screen boundaries, line wrapping, empty screen queries
- **TR-009**: Architecture reviews MUST verify that changes don't block future headless terminal capability (AC-001 through AC-005)

### Key Entities

- **VirtualConsole**: The main entry point. Accepts input (text with embedded ANSI sequences), processes it through the screen buffer, and provides query methods. Architected to later support `IAnsiConsole` implementation and input queue.
- **ScreenBuffer**: The 2D grid of cells representing the virtual screen. Has dimensions (width, height) and a cursor position. Designed as a separable component for future input handling integration.
- **ScreenCell**: A single character position on screen. Contains a character and associated style information.
- **CellStyle**: The visual styling for a cell. Includes foreground color, background color, and attribute flags (bold, invert, etc.).
- **ScreenRow**: A single row of the screen buffer. Provides convenient access to row content and per-cell styling.
- **InputQueue** (future): Will hold queued keystrokes and text for programmatic input. Not implemented in current release, but architecture accommodates it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Test authors can query current screen content via API and build assertions (vs. parsing cumulative output manually)
- **SC-002**: The query API provides sufficient data for 100% of BitPantry.CommandLine visual test scenarios
- **SC-003**: The menu filter highlighting bug can be reproduced and caught by a test using VirtualConsole queries (proving point-in-time value)
- **SC-004**: Package has zero dependencies on test frameworks (MSTest, xUnit, NUnit, FluentAssertions)
- **SC-005**: Package can be extracted to a standalone NuGet package without requiring changes to its public API
- **SC-006**: New ANSI sequences can be added without breaking existing consumer code (backward compatibility)
- **SC-007**: Query API returns complete style information (colors, attributes) enabling consumers to build any assertion
- **SC-008**: 100% of production code is covered by tests written before the implementation (TDD compliance)
- **SC-009**: Test suite includes at least 4 "complexity milestone" tests proving real-world CLI handling (menu, progress bar, table, multi-region)
- **SC-010**: BitPantry.VirtualConsole.Tests project is structured for extraction alongside the main package

## Assumptions

- Initial release targets the ANSI sequences used by Spectre.Console (the rendering library used by BitPantry.CommandLine)
- Scrollback buffer is not required for initial release (screen is fixed height, content scrolls off)
- Mouse sequences and other advanced terminal features are out of scope for initial release
- The package will be published under MIT license to match BitPantry.CommandLine
- Documentation will be maintained in the main solution initially, structured for easy extraction later

## Out of Scope (Current Release)

### Intentionally Deferred

These features are part of the future vision but not in scope for the initial release:

- **Input queue API**: `QueueKeyPress()`, `QueueText()` for programmatic input - architecture will support, not implemented
- **IAnsiConsole implementation**: Allowing apps to use VirtualConsole as their console - architecture will support, not implemented
- **Input event synchronization**: Coordinating input/output for deterministic automation
- **ReadKey/ReadLine simulation**: Blocking input operations for interactive CLI simulation

### Not Planned

These features are outside the package vision entirely:

- Full VT100/VT220/xterm terminal emulation (only sequences needed for testing)
- Scrollback buffer / history
- Mouse event sequences
- Window resize sequences
- Alternative character sets
- Terminal multiplexing features
- **Test framework assertion helpers** - consuming projects build their own assertions using the query API
