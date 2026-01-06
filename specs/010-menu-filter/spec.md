# Feature Specification: Menu Filtering While Typing

**Feature Branch**: `010-menu-filter`  
**Created**: January 3, 2026  
**Status**: Draft  
**Input**: User description: "Implement menu filtering while typing for autocomplete - once the autocomplete menu is opened, continue typing to filter the options in real-time"

## Pre-Research and Planning

This section documents the research, analysis, and key decisions made prior to specification creation. A new implementer should use this context to understand the design rationale.

### Spectre.Console Built-in Features

**Important Discovery**: Spectre.Console's `SelectionPrompt` has built-in support for many features in this spec:

| Feature | Spectre.Console Support | API |
|---------|------------------------|-----|
| Search/Filter | ✅ Built-in | `.EnableSearch()` |
| Search placeholder | ✅ Built-in | `.SearchPlaceholderText("Type to search...")` |
| Match highlighting | ✅ Built-in | `.SearchHighlightStyle(new Style(...))` |
| Pagination | ✅ Built-in | `.PageSize(n)` |
| Wrap-around nav | ✅ Built-in | `.WrapAround()` |
| Custom highlight | ✅ Built-in | `.HighlightStyle(new Style(...))` |

**However**, `SelectionPrompt` is a **blocking modal prompt** - it takes over the console until selection is made. Our autocomplete system is **non-modal** and integrates with live input editing. We cannot use `SelectionPrompt` directly, but we can:

1. **Reference its behavior** as the design pattern to follow
2. **Potentially extract** rendering logic or styles from Spectre internals
3. **Use Spectre markup** for match highlighting in our custom `AutoCompleteMenuRenderable`

### Research Summary: How Leading CLIs Handle Autocomplete Filtering

Research was conducted on VS Code, fish shell, zsh, PowerShell PSReadLine, and fzf to understand industry best practices:

| Feature | VS Code | fish | zsh | PowerShell | fzf |
|---------|---------|------|-----|------------|-----|
| Real-time filtering | Yes | Yes | Yes | Yes | Yes |
| Match type | Fuzzy | Case-insensitive | Configurable | Case-insensitive | Fuzzy |
| Backspace behavior | Removes char, expands results | Same | Same | Same | Same |
| Backspace past trigger | Closes menu | Closes menu | Closes menu | Closes menu | N/A |
| Empty results | Shows "no matches" | Closes menu | Varies | Keeps open | Shows empty |
| Highlight matches | Yes | Limited | Limited | No | Yes |

### Key Decision: Space Key Behavior

**Question**: How should Space be handled when the menu is open? Completion values may contain spaces (e.g., file paths like `Program Files`).

**Research Finding**: Leading CLIs handle this via context-awareness:
- If cursor is inside quotes (`"Program Files`), Space is a normal character
- If cursor is outside quotes, Space closes the menu (allowing free-form input)

**Decision**: Implement **context-aware space handling**:
- Create an `IsInsideQuotes(buffer, position)` helper that counts unescaped `"` before cursor
- Odd count = inside quotes → Space filters
- Even count = outside quotes → Space closes menu without accepting

### Key Decision: Matching Algorithm

**Options Considered**:
1. **Prefix matching** (current behavior) - simple but limiting
2. **Substring matching** - finds text anywhere in the string
3. **Fuzzy matching** - allows non-contiguous matches (e.g., "mf" matches "my_file")

**Decision**: Implement **case-insensitive substring matching**. This provides good flexibility while being predictable and easy to understand. Fuzzy matching can be added later if needed.

### Key Decision: Empty Results Behavior

**Options Considered**:
1. Close menu when no matches
2. Keep menu open with "no matches" message

**Decision**: **Keep menu open with "(no matches)" message**. This allows the user to backspace and widen the filter without having to re-trigger autocomplete.

### Key Decision: Match Highlighting

**Decision**: **Yes, highlight matching substrings** in menu items. This provides visual feedback showing why each item matched and helps users scan results quickly.

### Key Decision: Trailing Space After Acceptance

**Current behavior**: Menu/Tab acceptance adds a trailing space after the inserted text.

**Problem identified**: 
- Inconsistent with ghost text acceptance (which adds no trailing space)
- Causes issues with directory completion (e.g., `Documents\ ` - space after slash)

**Decision**: **Remove trailing space on acceptance**. Cursor ends at end of inserted text. Benefits:
- Consistency between menu and ghost text acceptance
- Better directory navigation (can continue typing path)
- User can always add space manually if needed (single keystroke)

### Existing Infrastructure Discovered

Research into the codebase revealed significant existing infrastructure:

1. **MenuState.FilterText** property already exists (line 23 of MenuState.cs) but is unused
2. **CompletionOrchestrator.HandleCharacterAsync()** method exists for filtering
3. **AutoCompleteController.HandleCharacterWhileMenuOpenAsync()** exists but is not wired up
4. **InputBuilder default handler** (line ~174) currently calls `_acCtrl.End()` on any character input, closing the menu instead of filtering

**Key Gap**: The filtering infrastructure exists but the `InputBuilder` isn't wired to use it.

### Quote Detection Analysis

**Current state**: The parsing infrastructure (`ParsedInput`, `ParsedCommandElement`) does NOT track quote boundaries. The parser regex `"[^\"]*"` handles quotes for tokenization but doesn't expose position information.

**Decision**: Create a simple `IsInsideQuotes(string buffer, int position)` utility in `StringExtensions.cs` that counts unescaped `"` characters. This is consistent with current parser behavior (which also doesn't handle escape sequences like `\"`).

### Test Infrastructure

The codebase uses `StepwiseTestRunner` with FluentAssertions custom extensions (`HaveBuffer`, `HaveMenuVisible`, `HaveSelectedMenuItem`, etc.). New tests should follow this pattern.

---

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Filter Menu by Typing (Priority: P1)

As a user, after pressing Tab to open the autocomplete menu, I want to continue typing to filter the displayed options so that I can quickly find the completion I need without scrolling through a long list.

**Why this priority**: This is the core functionality. Without filtering, users must scroll/navigate manually through potentially many options.

**Independent Test**: Can be tested by opening menu with Tab, typing filter text, and verifying the menu items are reduced to only matching entries.

**Acceptance Scenarios**:

1. **Given** the autocomplete menu is open showing ["connect", "config", "status"], **When** I type "con", **Then** the menu shows only ["connect", "config"]
2. **Given** the autocomplete menu is open, **When** I type characters that match no items, **Then** the menu displays "(no matches)" message
3. **Given** the menu shows "(no matches)", **When** I press Backspace, **Then** the filter is reduced and matching items reappear

---

### User Story 2 - Backspace Expands Filter (Priority: P1)

As a user, when I've typed too many filter characters, I want to press Backspace to remove characters and see more matching options.

**Why this priority**: Essential companion to filtering. Without this, users would have to close and reopen the menu to reset.

**Independent Test**: Open menu, type filter text, press Backspace, verify more items appear.

**Acceptance Scenarios**:

1. **Given** menu open with filter "conn" showing ["connect"], **When** I press Backspace, **Then** filter becomes "con" and menu shows ["connect", "config"]
2. **Given** menu open at buffer position 7, **When** I backspace to position 6 (before where menu was triggered), **Then** the menu closes

---

### User Story 3 - Space Closes Menu (Context-Aware) (Priority: P2)

As a user typing a command, I want the Space key to close the menu when I'm not inside a quoted string, so I can type custom values not in the completion list.

**Why this priority**: Important for UX but not core filtering functionality. Allows users to bypass autocomplete gracefully.

**Independent Test**: Open menu, press Space, verify menu closes and space is inserted.

**Acceptance Scenarios**:

1. **Given** menu is open and cursor is NOT inside quotes, **When** I press Space, **Then** the menu closes without accepting selection and a space is inserted
2. **Given** menu is open and cursor IS inside quotes (e.g., `--path "Program`), **When** I press Space, **Then** the space is treated as a filter character

---

### User Story 4 - Match Highlighting (Priority: P2)

As a user, I want to see the matching portion of each menu item highlighted so I can quickly understand why each item matched my filter.

**Why this priority**: Visual enhancement that improves usability but is not essential for core functionality.

**Independent Test**: Open menu, type filter, verify matching substring is visually highlighted in each menu item.

**Acceptance Scenarios**:

1. **Given** menu open with filter "fig", **When** viewing the item "config", **Then** the "fig" portion is visually highlighted

---

### User Story 5 - Consistent Cursor Position After Acceptance (Priority: P2)

As a user, when I accept a completion (via Enter or Tab with single match), I want the cursor to be positioned at the end of the inserted text without an automatic trailing space.

**Why this priority**: Consistency improvement. Current behavior differs between ghost text and menu acceptance.

**Independent Test**: Accept a menu selection, verify cursor is at end of text with no trailing space.

**Acceptance Scenarios**:

1. **Given** menu showing "connect" selected, **When** I press Enter, **Then** "connect" is inserted and cursor is immediately after the "t" (no trailing space)
2. **Given** a directory completion "Documents\", **When** I accept it, **Then** cursor is after the backslash, ready to continue the path

---

### Edge Cases

- **Exact match**: When filter text matches a completion exactly, the menu remains open and requires explicit acceptance (Enter/Tab). No auto-accept behavior.
- **Special characters**: Characters like `-`, `_`, `.`, `/` in filter text are matched literally against completion text. No normalization or equivalence mapping.
- **Fast typing**: All keystrokes are buffered and processed sequentially. No input is lost. Menu state updates after all buffered input is applied.
- **Navigation with filtering**: Arrow keys navigate within the currently filtered items only. When filter text changes, selection resets to the first matching item.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST filter menu items in real-time as the user types characters while the menu is open
- **FR-002**: System MUST use case-insensitive substring matching to filter menu items
- **FR-003**: System MUST display "(no matches)" message when filter produces zero matches, keeping the menu open
- **FR-004**: System MUST remove the last filter character and re-filter when Backspace is pressed while menu is open
- **FR-005**: System MUST close the menu when Backspace would move cursor before the position where the menu was triggered
- **FR-006**: System MUST close the menu without accepting when Space is pressed outside of a quoted string context
- **FR-007**: System MUST treat Space as a filter character when cursor is inside a quoted string (odd number of unescaped `"` before cursor)
- **FR-008**: System MUST highlight the matching substring within each menu item using a distinct visual style
- **FR-009**: System MUST NOT add a trailing space after accepting a completion from the menu
- **FR-010**: System MUST position cursor at the end of inserted text after accepting a completion
- **FR-011**: System MUST update the filter as part of the existing input buffer (text appears on the input line as typed)

### Key Entities

- **MenuState**: Tracks current menu items, selected index, viewport, and FilterText
- **InputBuffer**: The current text being composed by the user
- **CompletionItem**: Individual autocomplete option with DisplayText and InsertText

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can reduce a 20-item menu to 3 or fewer items within 2-3 keystrokes of filtering
- **SC-002**: 100% of existing autocomplete tests continue to pass after implementation
- **SC-003**: Filter response time is imperceptible (under 50ms) for lists of up to 100 items
- **SC-004**: Match highlighting is visible and distinguishable from normal menu text
- **SC-005**: Ghost text and menu acceptance produce identical cursor positioning behavior (no trailing space)

## Clarifications

### Session 2026-01-03

- Q: What happens when filter text matches a completion exactly? → A: Continue filtering; require explicit accept (Enter/Tab)
- Q: How does filtering work with special characters in completion text? → A: Literal matching - special chars match literally
- Q: What happens if the user types very fast while menu is animating? → A: Buffer all keystrokes and process sequentially - no input lost
- Q: How does filtering interact with keyboard navigation (up/down arrows)? → A: Navigate within filtered list; selection resets to first item when filter changes

## Assumptions

- The existing `StepwiseTestRunner` test infrastructure is sufficient for testing filtering behavior
- Substring matching (not fuzzy) is the correct initial approach; fuzzy can be added later
- Escape sequences (`\"`) do not need special handling (consistent with current parser behavior)
- Menu filtering does not need to persist across menu close/reopen cycles
