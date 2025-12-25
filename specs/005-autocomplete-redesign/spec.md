# Feature Specification: Autocomplete Redesign

**Feature Branch**: `005-autocomplete-redesign`  
**Created**: December 24, 2025  
**Status**: Draft  
**Input**: User description: "Redesign autocomplete system to support inline ghost suggestions, completion menus, async-aware remote command support with caching, and modern CLI best practices"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Tab Completion with Visible Feedback (Priority: P1)

A user is typing a command and wants to see available completions. When they press Tab, a completion menu appears showing all matching options with descriptions. They can navigate with arrow keys, select with Enter, or continue typing to filter results. The menu updates in real-time as they type.

**Why this priority**: Core autocomplete interaction—without this, users cannot discover or complete commands. This is the fundamental user flow.

**Independent Test**: Can be fully tested by typing partial command text, pressing Tab, observing the menu, navigating options, and accepting a selection. Delivers immediate value for command discovery.

**Acceptance Scenarios**:

1. **Given** user is at an empty prompt, **When** they press Tab, **Then** a menu appears showing all available top-level commands and groups with descriptions
2. **Given** user has typed "ma", **When** they press Tab, **Then** menu shows only commands/groups starting with "ma" (e.g., "math") with matched portion highlighted
3. **Given** menu is open with multiple options, **When** user presses Down Arrow, **Then** selection moves to next item; Up Arrow moves to previous item
4. **Given** menu is open with an item selected, **When** user presses Enter, **Then** selected value is inserted into input and menu closes
5. **Given** menu is open, **When** user presses Escape, **Then** menu closes and original input is preserved
6. **Given** menu is open, **When** user continues typing, **Then** menu filters in real-time to show matching options

---

### User Story 2 - Inline Ghost Suggestions While Typing (Priority: P2)

As a user types, a ghost suggestion appears in muted text showing the best matching completion. They can accept the ghost with the Right Arrow key, or ignore it by continuing to type normally. The ghost updates after each keystroke.

**Why this priority**: Improves typing speed for experienced users who know what they want, following the Fish shell pattern. Less disruptive than Tab menu but still provides guidance.

**Independent Test**: Can be tested by typing characters and observing ghost text appearing, then pressing Right Arrow to accept or continuing to type to see it update.

**Acceptance Scenarios**:

1. **Given** user types "con", **When** a matching command "connect" exists, **Then** "nect" appears as ghost text (muted color) after cursor
2. **Given** ghost suggestion is visible, **When** user presses Right Arrow, **Then** ghost text is accepted and cursor moves to end of completed word
3. **Given** ghost suggestion is visible, **When** user types another character, **Then** ghost updates to reflect new best match (or disappears if no match)
4. **Given** ghost suggestion is visible, **When** user presses any non-accepting key (letters, backspace), **Then** ghost updates but doesn't interfere with typing
5. **Given** no matching completions exist, **When** user types, **Then** no ghost text appears

---

### User Story 3 - Argument Name and Alias Completion (Priority: P2)

After typing a command name, user wants to complete argument names. When they type "--" and press Tab, they see available argument names. When they type "-" and press Tab, they see single-character aliases. Already-used arguments are excluded from suggestions.

**Why this priority**: Equally important as command completion for usability—users need to discover and complete argument names without memorizing them.

**Independent Test**: Can be tested by typing a valid command, then "--" followed by Tab, observing argument name options, and selecting one.

**Acceptance Scenarios**:

1. **Given** user has typed "connect --", **When** they press Tab, **Then** menu shows all available argument names for "connect" command (e.g., "--server", "--port")
2. **Given** user has typed "connect -", **When** they press Tab, **Then** menu shows single-character aliases (e.g., "-s", "-p")
3. **Given** user has already typed "connect --server localhost --", **When** they press Tab, **Then** "--server" is excluded from suggestions
4. **Given** user types "connect --ser", **When** they press Tab, **Then** menu shows only arguments starting with "ser"

---

### User Story 4 - Argument Value Completion (Priority: P2)

After typing an argument name (e.g., "--server "), user wants to see available values. When they press Tab, the system invokes the argument's registered completion provider and shows matching values. For remote commands, values may come from the server.

**Why this priority**: Completes the argument entry workflow. Without value completion, users must know valid values from memory or documentation.

**Independent Test**: Can be tested with a command that has value completion configured, typing the argument name, then Tab to see value suggestions.

**Acceptance Scenarios**:

1. **Given** command has a "--color" argument with values ["red", "green", "blue"], **When** user types "--color " and presses Tab, **Then** menu shows "red", "green", "blue"
2. **Given** user types "--color gr", **When** they press Tab, **Then** menu shows only "green"
3. **Given** argument has no completion provider, **When** user presses Tab after argument name, **Then** no menu appears (silent, no error)

---

### User Story 5 - Command Implementer Adds Autocomplete (Priority: P2)

As a command implementer, I want to add autocomplete to my command's arguments with minimal code and a consistent pattern, whether using built-in providers (file paths, directories) or creating custom domain-specific providers.

**Why this priority**: The autocomplete system is only as good as its adoption. If adding autocomplete is tedious or inconsistent, implementers will skip it—degrading end-user experience.

**Independent Test**: Can be tested by creating a new command with arguments, adding autocomplete using both built-in and custom providers, and verifying completions appear for end users.

**Acceptance Scenarios**:

1. **Given** I'm implementing a command with a file path argument, **When** I specify the built-in file path provider, **Then** end users get file/directory completion with no additional code
2. **Given** I need domain-specific completions (e.g., server names from config), **When** I create a custom provider implementing the standard interface, **Then** it works identically to built-in providers
3. **Given** I have an argument with no autocomplete configured, **When** end user presses Tab after that argument, **Then** system silently skips (no error, no crash)
4. **Given** I specify an autocomplete provider for an argument, **When** the command is registered, **Then** autocomplete works with zero additional wiring or configuration

---

### User Story 6 - Remote Command Autocomplete with Loading Feedback (Priority: P3)

When completing argument values for remote commands, the system fetches completions from the server. While fetching, user sees a loading indicator. If the network is slow or fails, user receives appropriate feedback rather than a frozen interface.

**Why this priority**: Critical for remote command usability, but builds on the local completion foundation. Without async handling, remote autocomplete feels broken.

**Independent Test**: Can be tested by connecting to a remote server, typing a remote command with argument, pressing Tab, and observing loading indicator followed by results (or error message).

**Acceptance Scenarios**:

1. **Given** user is connected to a remote server, **When** they press Tab for a remote argument value, **Then** a loading indicator appears immediately
2. **Given** remote fetch is in progress, **When** results arrive, **Then** loading indicator is replaced with completion menu showing results
3. **Given** remote fetch is in progress, **When** user types additional characters, **Then** current fetch is cancelled and new fetch starts with updated query (after debounce)
4. **Given** remote fetch fails due to network error, **When** error occurs, **Then** user sees a brief error message and can continue typing
5. **Given** user is disconnected from server, **When** they press Tab for remote values, **Then** system shows "(offline)" indicator and uses cached results if available

---

### User Story 7 - Completion Result Caching (Priority: P3)

To improve performance, completion results are cached for the session. When user presses Tab with a query that was previously fetched, cached results are used immediately. Cache is invalidated after command execution or after a timeout.

**Why this priority**: Performance optimization that significantly improves perceived responsiveness, especially for remote commands. Not required for basic functionality.

**Independent Test**: Can be tested by pressing Tab twice for the same argument—second Tab should be noticeably faster. Execute a command, then Tab again should re-fetch.

**Acceptance Scenarios**:

1. **Given** user pressed Tab for "--server" values and received results, **When** they press Tab again for same argument, **Then** cached results appear instantly (no loading indicator)
2. **Given** cached results exist, **When** user types to filter, **Then** filtering happens locally on cached data (no network call)
3. **Given** cached results exist, **When** user executes a command, **Then** cache for that command's arguments is invalidated
4. **Given** cached results are older than 5 minutes, **When** user presses Tab, **Then** fresh results are fetched

---

### User Story 8 - No-Match Feedback (Priority: P3)

When user presses Tab and no completions match, they receive clear feedback rather than silent failure. This helps users understand when they've typed something unrecognizable.

**Why this priority**: Quality-of-life improvement that reduces confusion. Lower priority because it's not blocking any workflow.

**Independent Test**: Can be tested by typing gibberish and pressing Tab, observing feedback message.

**Acceptance Scenarios**:

1. **Given** user types "xyzabc123" (no matching commands), **When** they press Tab, **Then** brief "(no matches)" indicator appears
2. **Given** no matches found, **When** indicator appears, **Then** it auto-dismisses after 1-2 seconds without user action

---

### User Story 9 - Match Count Indicator (Priority: P4)

When completion menu is open, user can see how many matches exist (e.g., "3 of 12"). This helps users know when to keep typing to narrow results versus when to scroll.

**Why this priority**: Nice-to-have discoverability feature. Users can function without it by scrolling.

**Independent Test**: Can be tested by opening completion menu and observing count indicator at bottom or in header.

**Acceptance Scenarios**:

1. **Given** menu is open with 12 matching options, **When** user selects 3rd item, **Then** indicator shows "3 of 12" (or similar)
2. **Given** user types to filter results, **When** matches reduce to 3, **Then** indicator updates to reflect new count

---

### Edge Cases

- What happens when completion source returns thousands of items? *System caches first 100 items and displays in 10-row scrollable viewport with "showing 100 of N" truncation indicator*
- What happens when user presses Tab rapidly multiple times? *Debounce prevents multiple fetches; current fetch is reused*
- What happens if remote server disconnects mid-fetch? *Timeout after reasonable period, show error, allow user to continue*
- What happens with very long completion values that exceed terminal width? *Truncate with ellipsis, show full value on selection*
- What happens when user resizes terminal while menu is open? *Menu should reposition or close gracefully*

## Requirements *(mandatory)*

### Functional Requirements

#### Completion Triggers & Navigation

- **FR-001**: System MUST show completion menu when user presses Tab
- **FR-002**: System MUST allow navigation through menu using Up/Down arrow keys
- **FR-003**: System MUST accept selected completion when user presses Enter
- **FR-004**: System MUST close menu and preserve original input when user presses Escape
- **FR-005**: System MUST filter menu results in real-time as user types while menu is open
- **FR-006**: System MUST close menu when user moves cursor away from completion context (e.g., left arrow past start of word)

#### Ghost Suggestions

- **FR-007**: System MUST display best-match completion as muted ghost text after cursor while typing, sourced from command history (prioritized) and registered commands (fallback)
- **FR-008**: System MUST accept ghost suggestion when user presses Right Arrow or End key
- **FR-009**: System MUST update ghost suggestion after each keystroke
- **FR-010**: System MUST hide ghost when no matches exist

#### Completion Sources

- **FR-011**: System MUST complete command names and group names at the start of input
- **FR-012**: System MUST complete argument names after "--" prefix
- **FR-013**: System MUST complete argument aliases after "-" prefix
- **FR-014**: System MUST complete argument values when cursor is after an argument name
- **FR-015**: System MUST exclude already-used arguments from name/alias completion
- **FR-016**: System MUST support completion via a unified `[Completion]` attribute with three modes:
  - `[Completion("val1", "val2")]` - static values
  - `[Completion(nameof(MethodName))]` - method on command class
  - `[Completion(typeof(ProviderType))]` - provider type resolved from DI
- **FR-016a**: Built-in providers and custom providers MUST implement the same `ICompletionProvider` interface
- **FR-016b**: Shortcut attributes (e.g., `[FilePath]`, `[DirectoryPath]`) MUST inherit from `CompletionAttribute` pointing to their respective providers
- **FR-016c**: System MUST include built-in providers for: file paths, directory paths
- **FR-016d**: System MUST automatically provide completion for enum-typed arguments (no attribute required)
- **FR-016e**: Completion methods MUST support DI injection of services as method parameters
- **FR-017**: System MUST support remote completion providers for commands registered from remote servers

#### Matching & Ranking

- **FR-018**: System MUST support case-insensitive matching
- **FR-019**: System MUST prioritize prefix matches over contains matches
- **FR-020**: System MUST highlight matched portion of each suggestion in the menu
- **FR-021**: System SHOULD support fuzzy matching as fallback when prefix/contains yield no results

#### Async & Performance

- **FR-022**: System MUST show loading indicator immediately when fetching remote completions
- **FR-023**: System MUST cancel in-progress fetch when user types new characters (debounced)
- **FR-024**: System MUST debounce fetch requests while user is actively typing (100ms recommended)
- **FR-025**: System MUST cache completion results for the session to avoid redundant fetches
- **FR-026**: System MUST invalidate cache for a command's arguments after that command executes
- **FR-027**: System MUST handle network failures gracefully with user feedback; no automatic retry—user presses Tab again to retry manually

#### Visual Feedback

- **FR-028**: System MUST visually distinguish selected item in menu (e.g., inverted colors)
- **FR-029**: System MUST display descriptions alongside completion values when available
- **FR-030**: System MUST show match count indicator (e.g., "2 of 5") in menu
- **FR-030a**: System MUST display menu in a scrollable viewport of 10 visible rows with scroll indicators when results exceed viewport
- **FR-030b**: System MUST limit cached results to 100 items and show truncation indicator (e.g., "showing 100 of N") when source returns more
- **FR-031**: System MUST show "(no matches)" feedback when Tab produces no results
- **FR-032**: System MUST show connection state indicator for remote completions (connected/offline)

### Key Entities

- **CompletionSource**: A provider of completion suggestions (commands, argument names, argument values, file paths). Has properties: name, isAsync, fetch method
- **CompletionItem**: A single suggestion with value, optional description, match score, and source identifier
- **CompletionContext**: The current input state including: query text, cursor position, parsed command/arguments, connection state
- **CompletionCache**: Session-scoped storage of previously fetched results, keyed by (commandPath, argumentName, queryPrefix)
- **CompletionMenu**: Visual component rendering the list of suggestions with selection state and navigation

## Success Criteria *(mandatory)*

### Measurable Outcomes

#### End-User Experience

- **SC-001**: Local completions (commands, argument names) appear in under 50 milliseconds
- **SC-002**: Users can accept a completion with a single keystroke (Tab→Enter or Right Arrow for ghost)
- **SC-003**: 90% of users can discover available commands without consulting documentation (via Tab exploration)
- **SC-004**: Remote completion requests complete or show timeout within 3 seconds
- **SC-005**: Cached completions display instantly (under 10ms) on repeat access
- **SC-006**: Users can continue typing without interruption while remote fetch is in progress
- **SC-007**: No visible UI flicker or cursor jumping during completion menu display
- **SC-008**: Completion menu is readable on terminals with at least 80 column width

#### Command Implementer Experience

- **SC-009**: Adding file path completion to an argument requires specifying only the provider type (one attribute or parameter)
- **SC-010**: Custom providers follow the same pattern as built-in providers—no special registration or wiring required
- **SC-011**: Enum-typed arguments receive completion automatically without any additional attributes
- **SC-012**: Static value lists can be specified via `[CompletionValues]` attribute

## Design Constraints

- This design **completely replaces** the existing autocomplete system—all existing autocomplete code is removed
- The `AutoCompleteFunctionName` property will be removed from `ArgumentAttribute`
- Reuse existing infrastructure (input handling, command registry, SignalR proxy) where it aligns with the new design; replace where it doesn't
- Uses Spectre.Console `SelectionPrompt` for menu rendering (no custom menu implementation)

## Assumptions

- Terminal supports ANSI escape codes for colors and cursor positioning (standard for modern terminals)
- Users are familiar with Tab-completion conventions from other shells (Bash, PowerShell, Fish)
- Completion menu can render below the input line (terminal has available rows)
- Remote connections use the existing SignalR infrastructure
- Ghost suggestions use a visually distinct but readable color (dark gray on default background)
- Completion menu displays 10 visible rows at a time in a scrollable viewport; system caches up to 100 items from sources returning more, with indicator showing truncation (e.g., "showing 100 of 500 results")

## Clarifications

### Session 2025-12-24

- Q: How should the system handle completion sources that return more items than can be displayed? → A: Limit to first 100 results with scrollable 10-row viewport and "showing 100 of N" indicator
- Q: Should the system retry automatically after network failure, or require user action? → A: No automatic retry; user presses Tab again to retry
- Q: What sources should ghost suggestions use and in what priority? → A: Both history and registered commands, with history matches prioritized
- Q: Should file path completion be included in scope? → A: Yes, as a built-in provider implementing the same interface as custom providers; uniform mechanism for all providers

## Test Scenarios

This section defines comprehensive test scenarios for validating the autocomplete system. Tests are organized by functional area with coverage of happy paths, edge cases, and error conditions.

### Menu Completion Tests

#### Basic Menu Behavior

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| MC-001 | Tab opens menu at empty prompt | Empty input line | User presses Tab | Menu appears with all top-level commands/groups |
| MC-002 | Tab opens menu with partial input | Input "hel" with command "help" registered | User presses Tab | Menu shows "help" with "hel" highlighted |
| MC-003 | Down arrow navigates forward | Menu open, first item selected | User presses Down Arrow | Second item becomes selected |
| MC-004 | Up arrow navigates backward | Menu open, second item selected | User presses Up Arrow | First item becomes selected |
| MC-005 | Down arrow wraps at bottom | Menu open, last item selected | User presses Down Arrow | First item becomes selected |
| MC-006 | Up arrow wraps at top | Menu open, first item selected | User presses Up Arrow | Last item becomes selected |
| MC-007 | Enter accepts selection | Menu open, "connect" selected | User presses Enter | "connect" inserted into input, menu closes |
| MC-008 | Escape cancels without change | Input "con", menu open | User presses Escape | Menu closes, input remains "con" |
| MC-009 | Typing filters menu | Menu open showing 5 items | User types "s" | Menu filters to items containing/starting with "s" |
| MC-010 | Menu closes on left arrow past start | Input "con" at position 3, menu open | User presses Left Arrow 4 times | Menu closes after cursor passes position 0 |
| MC-011 | Tab with no matches | Input "xyznonexistent" | User presses Tab | "(no matches)" indicator appears |
| MC-012 | Tab on already complete command | Input "help" (exact match) | User presses Tab | Menu shows "help" selected (or advances to args) |

#### Menu Viewport & Scrolling

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| MC-020 | Viewport shows 10 rows max | 25 matching items | User presses Tab | Menu shows 10 items with scroll indicator |
| MC-021 | Scroll down reveals more | 25 items, viewing 1-10, item 10 selected | User presses Down Arrow | Viewport scrolls to show items 2-11 |
| MC-022 | Scroll up reveals earlier | 25 items, viewing 5-14, item 5 selected | User presses Up Arrow | Viewport scrolls to show items 4-13 |
| MC-023 | Match count updates | 25 items, "5 of 25" shown | User types to filter to 8 items | Indicator updates to "1 of 8" |

#### Menu with Descriptions

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| MC-030 | Description shown alongside value | Command "help" has description "Show help" | Menu displays "help" | "Show help" displayed next to "help" |
| MC-031 | Missing description handled | Command "test" has no description | Menu displays "test" | "test" shown without description (no error) |
| MC-032 | Long description truncated | Description exceeds terminal width | Menu displays item | Description truncated with ellipsis |

### Ghost Suggestion Tests

#### Basic Ghost Behavior

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| GS-001 | Ghost appears on typing | Command "connect" registered | User types "con" | Ghost shows "nect" in muted color after cursor |
| GS-002 | Right arrow accepts ghost | Ghost showing "nect" after "con" | User presses Right Arrow | Input becomes "connect", cursor at end |
| GS-003 | End key accepts ghost | Ghost showing "nect" after "con" | User presses End | Input becomes "connect", cursor at end |
| GS-004 | Typing updates ghost | Ghost showing "nect", "connect" and "config" registered | User types "f" (now "conf") | Ghost updates to "ig" (for "config") |
| GS-005 | Typing removes ghost when no match | Ghost showing "nect" | User types "x" (now "conx") | Ghost disappears |
| GS-006 | Backspace updates ghost | Input "conf" with ghost "ig" | User presses Backspace (now "con") | Ghost updates to "nect" |
| GS-007 | Ghost doesn't interfere with typing | Ghost visible | User types normally | Characters insert correctly, ghost updates |
| GS-008 | No ghost when no matches | No commands starting with "xyz" | User types "xyz" | No ghost text appears |

#### Ghost Source Priority

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| GS-010 | History prioritized over commands | History has "connect --server prod", command "connect" registered | User types "con" | Ghost shows "nect --server prod" (from history) |
| GS-011 | Command used when no history match | No history starting with "hel", command "help" registered | User types "hel" | Ghost shows "p" (from command) |
| GS-012 | Most recent history preferred | History has "connect A" then "connect B" | User types "con" | Ghost shows completion from "connect B" |

#### Ghost + Menu Interaction

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| GS-020 | Ghost hidden when menu open | Ghost showing "nect" | User presses Tab | Menu opens, ghost hidden |
| GS-021 | Ghost returns after menu close | Menu open, then closed with Escape | Input still "con" | Ghost reappears showing "nect" |
| GS-022 | Ghost updates after menu accept | Menu accepts "connect" | Input is now "connect " | Ghost shows next suggestion (if any) |

### Argument Completion Tests

#### Argument Name Completion

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AC-001 | Double-dash shows all arg names | Command "connect" with args --server, --port, --timeout | User types "connect --" + Tab | Menu shows "--server", "--port", "--timeout" |
| AC-002 | Partial arg name filters | Command with --server, --silent | User types "connect --s" + Tab | Menu shows "--server", "--silent" |
| AC-003 | Single-dash shows aliases | Command with -s, -p, -t aliases | User types "connect -" + Tab | Menu shows "-s", "-p", "-t" |
| AC-004 | Used argument excluded | Already typed "--server localhost" | User types "connect --server localhost --" + Tab | "--server" not in menu |
| AC-005 | Multiple used args excluded | Already typed "--server x --port 80" | User types "... --" + Tab | Neither "--server" nor "--port" in menu |
| AC-006 | Alias of used arg excluded | Used "--server", alias is "-s" | User types "... -" + Tab | "-s" not in menu |

#### Argument Value Completion

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AC-010 | Value completion invokes provider | Arg --color has provider returning [red, green, blue] | User types "--color " + Tab | Menu shows "red", "green", "blue" |
| AC-011 | Partial value filters | Provider returns [red, green, blue] | User types "--color g" + Tab | Menu shows "green" only |
| AC-012 | No provider = silent skip | Arg --name has no provider | User types "--name " + Tab | No menu, no error |
| AC-013 | Provider returns empty list | Provider returns [] | User types "--items " + Tab | "(no matches)" indicator |
| AC-014 | Provider context includes prior args | --env depends on --server value | User types "--server prod --env " + Tab | Provider receives context with server=prod |

#### Positional Argument Completion

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| AC-020 | First positional arg completed | Command expects file path as first arg | User types "process " + Tab | File path completions shown |
| AC-021 | Second positional after first | First positional filled | User types "copy source.txt " + Tab | Destination completions shown |

### Built-in Provider Tests

#### File Path Provider

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| FP-001 | Current directory files listed | CWD has file1.txt, file2.txt | User types "--file " + Tab | Menu shows "file1.txt", "file2.txt" |
| FP-002 | Partial path filters | CWD has file1.txt, folder/ | User types "--file f" + Tab | Menu shows "file1.txt", "folder/" |
| FP-003 | Subdirectory navigation | User typed "src/" | Tab pressed | Menu shows contents of src/ |
| FP-004 | Parent directory navigation | User typed "../" | Tab pressed | Menu shows contents of parent directory |
| FP-005 | Absolute path works | User typed "C:/" (Windows) or "/" (Unix) | Tab pressed | Menu shows root contents |
| FP-006 | Hidden files included | CWD has .gitignore | User types "--file ." + Tab | ".gitignore" appears in menu |
| FP-007 | Spaces in paths handled | File "my file.txt" exists | Tab pressed | "my file.txt" appears (properly escaped/quoted) |

#### Directory Path Provider

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| DP-001 | Only directories shown | CWD has file.txt, folder/ | User types "--dir " + Tab | Menu shows "folder/" only |
| DP-002 | Subdirectories navigable | User typed "src/" | Tab pressed | Menu shows only subdirectories of src/ |

### Matching & Ranking Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| MR-001 | Case-insensitive matching | Command "Help" registered | User types "hel" + Tab | "Help" appears in menu |
| MR-002 | Prefix matches ranked first | Commands: "server", "laser", "reserve" | User types "ser" + Tab | "server" appears before "laser", "reserve" |
| MR-003 | Contains matches after prefix | Commands: "server", "reserve" | User types "serv" + Tab | "server" first (prefix), then "reserve" (contains) |
| MR-004 | Matched portion highlighted | Command "connect", user typed "con" | Menu displays | "con" portion visually highlighted |
| MR-005 | Fuzzy match fallback (if enabled) | Command "configure", no prefix/contains match for "cnfg" | User types "cnfg" + Tab | "configure" suggested as fuzzy match |
| MR-006 | Exact match prioritized | Commands: "test", "testing", "attest" | User types "test" + Tab | "test" appears first |

### Remote Command Tests

#### Remote Completion - Happy Path

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| RC-001 | Loading indicator shown | Connected to remote, arg needs server fetch | User presses Tab | Loading indicator appears immediately |
| RC-002 | Results replace loading | Server responds with 5 items | Response received | Menu shows 5 items, loading gone |
| RC-003 | Remote values filtered locally | Cached remote results exist | User types partial match | Filtering happens locally (no network) |

#### Remote Completion - Error Handling

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| RC-010 | Network timeout | Server doesn't respond within 3 seconds | Timeout occurs | Error message shown, user can continue |
| RC-011 | Network error | Connection fails | Error occurs | Brief error message, input still usable |
| RC-012 | Server returns error | Server returns 500 | Error received | Error message shown, user can retry |
| RC-013 | Disconnected state | Not connected to server | User presses Tab on remote arg | "(offline)" indicator, cached results if available |
| RC-014 | Reconnection during fetch | Connection drops mid-fetch | Reconnecting | Timeout after reasonable period, error shown |
| RC-015 | Manual retry after error | Previous Tab failed | User presses Tab again | New fetch attempted |

#### Remote Completion - Async Behavior

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| RC-020 | Typing during fetch cancels | Fetch in progress (300ms elapsed) | User types character | Previous fetch cancelled, new debounced fetch starts |
| RC-021 | Rapid typing debounced | User types 5 characters quickly | Debounce period | Only 1 fetch after 100ms idle |
| RC-022 | Escape during fetch cancels | Fetch in progress | User presses Escape | Fetch cancelled, no menu shown |
| RC-023 | Non-blocking during fetch | Fetch in progress | User continues typing | Characters appear immediately, no lag |

### Caching Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| CA-001 | Cache hit - instant results | Previous Tab for "--env" returned [dev, prod] | User presses Tab on "--env" again | Results appear instantly (no loading) |
| CA-002 | Different arg = cache miss | Cache has "--env" results | User presses Tab on "--region" | New fetch triggered |
| CA-003 | Cache invalidated on execute | Cached results for "deploy" command | User executes "deploy" command | Cache for "deploy" args cleared |
| CA-004 | Cache TTL expiry | Cache entry is 6 minutes old | User presses Tab | Fresh fetch triggered |
| CA-005 | Cache respects context | Cache has "--env" with --server=prod | User changes to --server=dev, Tab on "--env" | New fetch (context changed) |
| CA-006 | Prefix reuses cache | Cached empty query results [dev, staging, prod] | User types "d" + Tab | Filters cached to [dev] locally |

### Result Limiting Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| RL-001 | 100 item limit applied | Source returns 500 items | Tab pressed | Menu shows 100 items max |
| RL-002 | Truncation indicator shown | Source returns 500 items | Menu displayed | Shows "showing 100 of 500" |
| RL-003 | Exactly 100 items = no indicator | Source returns 100 items | Menu displayed | No truncation indicator |
| RL-004 | Under 100 items = no indicator | Source returns 50 items | Menu displayed | Shows "1 of 50", no truncation |
| RL-005 | Filtering reduces count | 500 items, showing 100 | User types to filter to 5 | Shows "1 of 5", no truncation |

### Visual Feedback Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| VF-001 | Selected item highlighted | Menu open, 3rd item selected | Visual inspection | 3rd item has inverted colors |
| VF-002 | Selection moves with arrow | 3rd item selected | Down arrow pressed | 4th item now highlighted |
| VF-003 | Scroll indicator visible | 25 items, viewing 1-10 | Menu displayed | Down arrow/indicator visible |
| VF-004 | No flicker on update | Menu filtering | User types quickly | Menu updates smoothly without flicker |
| VF-005 | Cursor position stable | Menu opens | Visual inspection | Cursor doesn't jump |
| VF-006 | Connection indicator - online | Connected to remote server | Menu for remote arg shown | "connected" or no indicator (normal) |
| VF-007 | Connection indicator - offline | Disconnected from server | Tab on remote arg | "(offline)" indicator visible |

### Command Implementer Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| CI-001 | Static values via attribute | `[Completion("a", "b", "c")]` on argument | User presses Tab | "a", "b", "c" appear in menu |
| CI-002 | Method-based completion | `[Completion(nameof(GetValues))]` on argument | User presses Tab | Method called, results shown |
| CI-003 | Provider type completion | `[Completion(typeof(MyProvider))]` on argument | User presses Tab | Provider resolved from DI, results shown |
| CI-004 | Method with DI parameters | Completion method has service parameters | User presses Tab | Services injected, method executes |
| CI-005 | Shortcut attribute [FilePath] | `[FilePath]` on argument | User presses Tab | File completions work |
| CI-006 | Shortcut attribute [DirectoryPath] | `[DirectoryPath]` on argument | User presses Tab | Directory completions work |
| CI-007 | Custom shortcut attribute | Custom `[ConfigFile]` inherits `CompletionAttribute` | User presses Tab | ConfigFileProvider used |
| CI-008 | Enum auto-completion | Argument is enum type, no attribute | User presses Tab | Enum values appear |
| CI-009 | Provider resolved via DI | Custom provider has constructor dependencies | User presses Tab | Provider instantiated with dependencies |
| CI-010 | No completion = graceful skip | Arg has no `[Completion]` and not enum | User presses Tab | Nothing happens (no error) |
| CI-011 | Provider exception handled | Provider throws exception | User presses Tab | Error logged, graceful failure shown |
| CI-012 | Async method completion | Completion method is async | User presses Tab | Loading shown, then results |
| CI-013 | Cancellation respected | User presses Escape during fetch | Completion method | CancellationToken triggers |

### Boundary & Edge Case Tests

| ID | Scenario | Given | When | Then |
|----|----------|-------|------|------|
| EC-001 | Zero results | No commands registered | Tab at empty prompt | "(no matches)" indicator |
| EC-002 | One result | Only one matching command | Tab pressed | Menu shows 1 item (or auto-accepts?) |
| EC-003 | Exactly 10 results | 10 matching items | Tab pressed | Menu shows 10, no scroll needed |
| EC-004 | Exactly 11 results | 11 matching items | Tab pressed | Menu shows 10 with scroll indicator |
| EC-005 | Very long command name | Command name 200 chars | Menu displayed | Name truncated with ellipsis |
| EC-006 | Unicode in command names | Command "日本語コマンド" registered | Tab pressed | Unicode displays correctly |
| EC-007 | Special chars in values | File "file[1].txt" exists | Tab with file provider | Properly escaped in menu |
| EC-008 | Empty string in results | Provider returns ["", "valid"] | Tab pressed | Empty string handled (skipped or shown) |
| EC-009 | Null in results | Provider returns [null, "valid"] | Tab pressed | Null handled gracefully |
| EC-010 | Rapid Tab presses | User presses Tab 10 times in 100ms | Rapid input | Debounce prevents issues |
| EC-011 | Tab during command execution | Command running | Tab pressed | Autocomplete works or gracefully ignored |
| EC-012 | Terminal resize during menu | Menu open, 10 items visible | Terminal shrinks to 5 rows | Menu repositions or closes gracefully |
| EC-013 | Very narrow terminal | Terminal is 40 columns wide | Menu displayed | Menu renders without breaking |
| EC-014 | Minimum terminal width | Terminal is 80 columns | Menu with descriptions | All content readable |
