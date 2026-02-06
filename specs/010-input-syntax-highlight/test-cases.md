# Test Cases: Input Syntax Highlighting

**Feature**: 010-input-syntax-highlight  
**Created**: 2026-02-05  
**Sources**: spec.md, plan.md

## User Experience Validation

Tests derived from user stories and functional requirements.

| ID | Source | When | Then |
|----|--------|------|------|
| UX-001 | US1, FR-001 | User types complete group name "server" | Text displays in cyan |
| UX-002 | US1, FR-002 | User types complete command name "help" | Text displays in default/white |
| UX-003 | US1, FR-003 | User types argument flag "--host" | Text displays in yellow |
| UX-004 | US1, FR-003 | User types argument alias "-h" | Text displays in yellow |
| UX-005 | US1, FR-004 | User types argument value after flag | Text displays in magenta/purple |
| UX-006 | US2, FR-014 | User types "ser" (uniquely matches group "server") | Partial text displays in cyan |
| UX-007 | US2, FR-013 | User types "c" after "server " (matches both "connect" command and "core" group) | Partial text displays in default |
| UX-008 | US2, FR-014 | User types "con" after "server " (uniquely matches "connect") | Partial text displays in default (command color) |
| UX-009 | US3, FR-006 | User backspaces "server connect" to "server con" | "server" remains cyan, "con" updates to default |
| UX-010 | US3, FR-007 | User edits mid-line | Cursor returns to same position after re-render |
| UX-011 | US4, FR-008 | User types with ghost text showing | Typed text is colored, ghost text is dim, no visual conflict |
| UX-012 | US4, FR-008 | Autocomplete menu is open | Typed input remains colored while menu displays |
| UX-013 | US5, FR-011 | User types "server profile add" | "server" cyan, "profile" cyan, "add" default |
| UX-014 | US5, FR-011 | User types "admin users roles assign" (3 levels) | All groups cyan, command default |
| UX-015 | US6 | User types "nonexistent" (no match) | Text displays in default style |
| UX-016 | FR-012 | User types quoted value "hello world" | Entire quoted section in magenta/purple |

## Component Validation

Tests for individual components in isolation.

### SyntaxColorScheme

| ID | When | Then |
|----|------|------|
| CV-001 | Access SyntaxColorScheme.Group | Returns cyan Style |
| CV-002 | Access SyntaxColorScheme.Command | Returns default Style |
| CV-003 | Access SyntaxColorScheme.ArgumentName | Returns yellow Style |
| CV-004 | Access SyntaxColorScheme.ArgumentAlias | Returns yellow Style |
| CV-005 | Access SyntaxColorScheme.ArgumentValue | Returns purple/magenta Style |
| CV-006 | Access SyntaxColorScheme.GhostText | Returns dim Style |
| CV-007 | Access SyntaxColorScheme.Default | Returns default Style |

### SyntaxHighlighter

| ID | When | Then |
|----|------|------|
| CV-010 | Highlight empty string | Returns empty segment list |
| CV-011 | Highlight null input | Returns empty segment list or handles gracefully |
| CV-012 | Highlight "server" (known group) | Returns single segment with cyan style |
| CV-013 | Highlight "help" (known command, no group) | Returns single segment with default style |
| CV-014 | Highlight "server connect" | Returns two segments: cyan for "server", default for "connect" |
| CV-015 | Highlight "server connect --host" | Returns three segments with appropriate styles |
| CV-016 | Highlight "server connect --host localhost" | Returns four segments: cyan, default, yellow, purple |
| CV-017 | Highlight "server connect -h value" | Returns four segments: cyan, default, yellow, purple |
| CV-018 | Highlight partial "ser" that uniquely matches group | Returns cyan segment |
| CV-019 | Highlight partial that matches nothing | Returns default segment |
| CV-020 | Highlight with whitespace | Whitespace segments have default style |
| CV-021 | Highlight nested groups "server files download" | Returns: cyan, cyan, default |

### TokenMatchResolver

| ID | When | Then |
|----|------|------|
| CV-030 | ResolveMatch("server", null) where "server" is a group | Returns UniqueGroup |
| CV-031 | ResolveMatch("help", null) where "help" is a command | Returns UniqueCommand |
| CV-032 | ResolveMatch("ser", null) where only "server" group matches | Returns UniqueGroup |
| CV-033 | ResolveMatch("s", null) where "server" group and "status" command both match | Returns Ambiguous |
| CV-034 | ResolveMatch("xyz", null) where nothing matches | Returns NoMatch |
| CV-035 | ResolveMatch("connect", serverGroup) within group context | Returns UniqueCommand |
| CV-036 | ResolveMatch("files", serverGroup) where "files" is a subgroup | Returns UniqueGroup |

### ConsoleLineMirror (RenderWithStyles)

| ID | When | Then |
|----|------|------|
| CV-040 | RenderWithStyles with single segment | Console shows text in correct color |
| CV-041 | RenderWithStyles with multiple segments | All segments render with correct colors |
| CV-042 | RenderWithStyles with cursor at end | Cursor positioned after last segment |
| CV-043 | RenderWithStyles with cursor mid-line | Cursor positioned at specified position |
| CV-044 | RenderWithStyles clears old content | Previous line content replaced, not appended |

## Data Flow Validation

Tests for cross-component interactions.

| ID | When | Then |
|----|------|------|
| DF-001 | Character typed in InputBuilder | SyntaxHighlighter called, ConsoleLineMirror re-renders |
| DF-002 | Backspace pressed | Line re-highlighted with updated content |
| DF-003 | Character typed while ghost text showing | Ghost text cleared, line re-highlighted, new ghost text shown |
| DF-004 | Tab pressed to open menu | Input remains highlighted, menu renders below |
| DF-005 | Arrow keys navigate menu | Input highlighting unchanged |
| DF-006 | Menu selection accepted | Line updated and re-highlighted with new content |
| DF-007 | Paste multiple characters | Final state highlighted correctly |

## Error Handling Validation

Tests for edge cases and failure modes.

| ID | When | Then |
|----|------|------|
| EH-001 | SyntaxHighlighter receives malformed input | Gracefully returns default-styled segments |
| EH-002 | Console doesn't support colors | Falls back to plain text (Spectre.Console handles) |
| EH-003 | Very long input line (1000+ chars) | Highlights without hanging or crashing |
| EH-004 | Input with escape sequences | Handled correctly without breaking display |
| EH-005 | Input with only whitespace | Returns empty or default-styled segments |
| EH-006 | Quoted string without closing quote | Partial quote highlighted appropriately |
| EH-007 | Registry is empty (no commands/groups) | All text defaults to default style |
| EH-008 | Argument typed for command that doesn't accept it | Argument displays in default style (unrecognized) |
| EH-009 | Multiple characters typed rapidly before re-render | Final state highlighted correctly (batch behavior) |

## Testing Infrastructure Validation (PREREQUISITE)

Tests for the new color assertion methods in `VirtualConsoleAssertions.cs`.
**These must pass before integration tests can be written.**

### Gap Identified

Current `VirtualConsoleAssertions` only validates `CellAttributes` (dim, bold, reverse), NOT foreground/background colors. `CellStyle` has color properties but no assertions use them.

### Required New Assertions

| ID | Assertion Method | When | Then |
|----|------------------|------|------|
| TI-001 | HaveCellWithForegroundColor | Cell at (row, col) has cyan foreground | Assertion passes |
| TI-002 | HaveCellWithForegroundColor | Cell at (row, col) has yellow but expected cyan | Assertion fails with clear message |
| TI-003 | HaveRangeWithForegroundColor | Range of cells all have cyan foreground | Assertion passes |
| TI-004 | HaveRangeWithForegroundColor | Range has mixed colors but expected all cyan | Assertion fails identifying first mismatch |
| TI-005 | HaveCellWithForeground256 | Cell has 256-color palette value | Assertion validates extended color |
| TI-006 | HaveRangeWithForeground256 | Range has consistent 256-color | Assertion passes |
| TI-007 | HaveCellWithFullStyle | Cell matches exact CellStyle (color + attributes) | Assertion passes |
| TI-008 | HaveCellWithFullStyle | Cell has wrong color but right attributes | Assertion fails with specific difference |
| TI-009 | HaveCellWithForegroundColor | Cell has null foreground (default) | Assertion handles null correctly |
| TI-010 | HaveRangeWithForegroundColor | Range spans beyond screen width | Assertion fails gracefully with bounds error |

### Setup/Scaffolding

| ID | When | Then |
|----|------|------|
| SETUP-001 | ColoredSegment record created with Text, Start, End, Style | Record has all properties accessible and immutable |
| REFACTOR-001 | GhostTextController uses SyntaxColorScheme.GhostText | Ghost text renders with centralized dim style |
| REFACTOR-002 | AutoCompleteMenuRenderer uses SyntaxColorScheme | Menu uses centralized styles for highlight and group |