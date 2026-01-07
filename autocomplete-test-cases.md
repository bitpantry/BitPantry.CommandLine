# Autocomplete Test Cases

This document catalogs all discrete application scenarios (test cases) for the autocomplete feature set. Each test case represents a hypothesis (when X, then Y) with high-level validation steps at the business/data-flow level, agnostic of testing approach.

---

## Table of Contents

1. [Ghost Text Behavior](#1-ghost-text-behavior)
2. [Menu Display & Navigation](#2-menu-display--navigation)
3. [Menu Filtering](#3-menu-filtering)
4. [Input Editing](#4-input-editing)
5. [Command & Group Completion](#5-command--group-completion)
6. [Argument Name & Alias Completion](#6-argument-name--alias-completion)
7. [Argument Value Completion](#7-argument-value-completion)
8. [Positional Argument Completion](#8-positional-argument-completion)
9. [File Path Completion](#9-file-path-completion)
10. [Viewport Scrolling](#10-viewport-scrolling)
11. [Ghost & Menu Interaction](#11-ghost--menu-interaction)
12. [Multi-Step Workflows](#12-multi-step-workflows)
13. [History Navigation](#13-history-navigation)
14. [Edge Cases & Error Handling](#14-edge-cases--error-handling)
15. [Visual Rendering](#15-visual-rendering)
16. [Submission Behavior](#16-submission-behavior)
17. [Remote Completion](#17-remote-completion)
18. [Caching Behavior](#18-caching-behavior)
19. [Provider & Attribute Configuration](#19-provider--attribute-configuration)
20. [Match Ranking & Ordering](#20-match-ranking--ordering)
21. [Result Limiting & Truncation](#21-result-limiting--truncation)
22. [Terminal & Environment Edge Cases](#22-terminal--environment-edge-cases)
23. [Keyboard Variations](#23-keyboard-variations)
24. [Context Sensitivity](#24-context-sensitivity)
25. [Concurrent & Async Behavior](#25-concurrent--async-behavior)
26. [Quoting & Escaping Behavior](#26-quoting--escaping-behavior)
27. [Accessibility & Screen Reader Behavior](#27-accessibility--screen-reader-behavior)
28. [Multi-Command & Pipeline Completion](#28-multi-command--pipeline-completion)
29. [Fuzzy & Advanced Matching](#29-fuzzy--advanced-matching)
30. [State Persistence & Recovery](#30-state-persistence--recovery)
31. [Completion Source Interactions](#31-completion-source-interactions)
32. [VirtualConsole Integration Testing](#32-virtualconsole-integration-testing)
33. [Configuration & Settings](#33-configuration--settings)
34. [Error Messages & User Feedback](#34-error-messages--user-feedback)
35. [Boundary Value Testing](#35-boundary-value-testing)

---

## 1. Ghost Text Behavior

### TC-1.1: Single Character Shows Ghost Completion
**When** the user types a single character that matches the beginning of a command,  
**Then** ghost text appears showing the remainder of the best matching command.

**Steps:**
1. Start with empty prompt
2. Type "s"
3. Verify ghost text "erver" appears (completing "server")
4. Verify ghost text is styled distinctly (dim/gray)

### TC-1.2: Partial Word Shows Remainder
**When** the user types a partial command prefix,  
**Then** ghost text shows only the remaining characters to complete the word.

**Steps:**
1. Start with empty prompt
2. Type "ser"
3. Verify ghost text shows "ver" (not "server")

### TC-1.3: Exact Match Hides Ghost
**When** the user types a complete command that exactly matches a registered command,  
**Then** no ghost text appears.

**Steps:**
1. Start with empty prompt
2. Type "server"
3. Verify no ghost text is visible

### TC-1.4: No Match Shows No Ghost
**When** the user types text that doesn't match any command or completion,  
**Then** no ghost text appears.

**Steps:**
1. Start with empty prompt
2. Type "xyznonexistent"
3. Verify no ghost text appears

### TC-1.5: Subcommand Ghost After Command Space
**When** the user types a complete command followed by a space,  
**Then** ghost text shows the first available subcommand or argument.

**Steps:**
1. Type "server "
2. Verify ghost text shows first subcommand (e.g., "profile")
3. Type "d"
4. Verify ghost text updates to complete "disconnect"

### TC-1.6: Deep Nested Subcommand Ghost
**When** navigating into nested command groups,  
**Then** ghost text reflects available completions at each level.

**Steps:**
1. Type "server profile "
2. Verify ghost shows first subcommand in profile group (e.g., "add")
3. Type "a"
4. Verify ghost shows "dd" to complete "add"

### TC-1.7: Right Arrow Accepts Ghost Text
**When** ghost text is displayed and user presses Right Arrow,  
**Then** the ghost text is accepted and inserted into the buffer.

**Steps:**
1. Type "s" (ghost shows "erver")
2. Press Right Arrow
3. Verify buffer contains "server"
4. Verify ghost text is cleared

### TC-1.8: End Key Accepts Ghost Text
**When** ghost text is displayed and user presses End key,  
**Then** the ghost text is accepted and inserted into the buffer.

**Steps:**
1. Type "ser" (ghost shows "ver")
2. Press End key
3. Verify buffer contains "server"

### TC-1.9: Cursor Movement Left Hides Ghost
**When** the user moves cursor left (away from end of line),  
**Then** ghost text disappears.

**Steps:**
1. Type "s" (ghost shows "erver")
2. Press Left Arrow
3. Verify ghost text is no longer visible

### TC-1.10: Typing Matching Character Shrinks Ghost
**When** the user types a character that matches the next ghost character,  
**Then** ghost text shrinks by one character.

**Steps:**
1. Type "s" (ghost shows "erver")
2. Type "e"
3. Verify buffer is "se", ghost is "rver"

### TC-1.11: Typing Non-Matching Character Clears Ghost
**When** the user types a character that doesn't match the ghost,  
**Then** ghost text disappears.

**Steps:**
1. Type "s" (ghost shows "erver")
2. Type "x"
3. Verify buffer is "sx", ghost is empty

### TC-1.12: Backspace Updates Ghost for New Prefix
**When** the user presses Backspace,  
**Then** ghost text updates based on new prefix.

**Steps:**
1. Type "ser" (ghost shows "ver")
2. Press Backspace
3. Verify buffer is "se", ghost is "rver"

### TC-1.13: Ghost Source Priority - History Over Commands
**When** both history and registered commands match the typed prefix,  
**Then** history matches are prioritized for ghost text.

**Steps:**
1. Have "connect --server prod" in history, "connect" as registered command
2. Type "con"
3. Verify ghost shows "nect --server prod" (from history, not just "nect")

### TC-1.14: Ghost Uses Most Recent History Entry
**When** multiple history entries match the prefix,  
**Then** the most recent entry is used for ghost text.

**Steps:**
1. Execute "connect A", then "connect B" (in that order)
2. Type "con"
3. Verify ghost shows completion from "connect B" (most recent)

### TC-1.15: Ghost for Argument Values After Argument Name
**When** user has typed an argument name followed by space,  
**Then** ghost shows first available value completion.

**Steps:**
1. Command has --color with values [red, green, blue]
2. Type "paint --color "
3. Verify ghost shows "red" (first value)

### TC-1.16: Alt+Right Accepts First Word of Ghost
**When** ghost text contains multiple words and user presses Alt+Right Arrow,  
**Then** only the first word of ghost is accepted.

**Steps:**
1. Type "s" (ghost shows "erver connect")
2. Press Alt+Right Arrow
3. Verify buffer is "server" (only first word accepted)
4. Verify ghost updates for remaining context

---

## 2. Menu Display & Navigation

### TC-2.1: Tab Opens Menu with Multiple Matches
**When** Tab is pressed and multiple completions are available,  
**Then** a menu appears showing all matching options with first item selected.

**Steps:**
1. Type "server "
2. Press Tab
3. Verify menu is visible
4. Verify first item is highlighted/selected
5. Verify all subcommands are listed

### TC-2.2: Tab with Single Match Auto-Completes
**When** Tab is pressed and only one completion matches,  
**Then** the completion is auto-inserted without showing a menu.

**Steps:**
1. Type "server disc"
2. Press Tab
3. Verify "disconnect" is inserted
4. Verify no menu is shown

### TC-2.3: Tab with No Matches Does Nothing
**When** Tab is pressed with no available completions,  
**Then** nothing happens (no menu, no change).

**Steps:**
1. Type "xyznonexistent "
2. Press Tab
3. Verify buffer unchanged
4. Verify no menu appears

### TC-2.4: Tab at Empty Prompt Shows Root Commands
**When** Tab is pressed at an empty prompt,  
**Then** menu shows all available root-level commands.

**Steps:**
1. Start with empty prompt
2. Press Tab
3. Verify menu shows root commands (server, help, config, etc.)

### TC-2.5: Down Arrow Moves Selection Down
**When** menu is open and user presses Down Arrow,  
**Then** selection moves to the next item without changing buffer.

**Steps:**
1. Type "server ", press Tab (menu opens)
2. Note first selected item
3. Press Down Arrow
4. Verify second item is now selected
5. Verify buffer remains "server " (unchanged during navigation)

### TC-2.6: Up Arrow Moves Selection Up
**When** menu is open and user presses Up Arrow,  
**Then** selection moves to the previous item.

**Steps:**
1. Open menu, navigate down twice
2. Press Up Arrow
3. Verify selection moved to previous item

### TC-2.7: Menu Wraps at Bottom
**When** navigating past the last menu item,  
**Then** selection wraps to the first item.

**Steps:**
1. Open menu with N items
2. Press Down Arrow N times
3. Verify selection wrapped back to first item

### TC-2.8: Menu Wraps at Top
**When** navigating up from the first menu item,  
**Then** selection wraps to the last item.

**Steps:**
1. Open menu (first item selected)
2. Press Up Arrow
3. Verify selection jumped to last item

### TC-2.9: Enter Accepts Selection and Closes Menu
**When** user presses Enter with menu open,  
**Then** selected item is inserted and menu closes.

**Steps:**
1. Type "server ", Tab, Down Arrow
2. Note selected item
3. Press Enter
4. Verify buffer contains "server {selected-item}"
5. Verify menu is closed
6. Verify cursor is at end of inserted text

### TC-2.10: Escape Closes Menu Without Changing Buffer
**When** user presses Escape with menu open,  
**Then** menu closes but buffer remains at its pre-Tab state.

**Steps:**
1. Type "server "
2. Press Tab (menu opens)
3. Press Escape
4. Verify menu is closed
5. Verify buffer is still exactly "server "

### TC-2.11: Tab Advances to Next Item When Menu Open
**When** menu is already open and user presses Tab,  
**Then** selection advances to the next item (same as Down Arrow).

**Steps:**
1. Open menu
2. Note first selected item
3. Press Tab
4. Verify selection moved to second item

### TC-2.12: Shift+Tab Goes to Previous Item
**When** menu is open and user presses Shift+Tab,  
**Then** selection moves to the previous item.

**Steps:**
1. Open menu, navigate down
2. Press Shift+Tab
3. Verify selection moved back

### TC-2.13: Completion Inserted at Correct Position
**When** accepting a completion from menu,  
**Then** the text is inserted at the correct position (appending to command).

**Steps:**
1. Type "server "
2. Tab to open menu (first item "profile" selected)
3. Enter to accept
4. Verify buffer is "server profile" (NOT "profile server")

### TC-2.14: No Trailing Space After Completion Acceptance
**When** accepting a completion from menu,  
**Then** no trailing space is added after the completion.

**Steps:**
1. Type "server ", Tab, Enter
2. Verify buffer ends with completion text, not with extra space

### TC-2.15: Menu Shows Descriptions Alongside Values
**When** completion items have descriptions,  
**Then** descriptions are displayed next to values in menu.

**Steps:**
1. Command "help" has description "Show help information"
2. Open menu showing "help"
3. Verify "Show help information" displayed next to "help"

### TC-2.16: Missing Description Handled Gracefully
**When** a completion item has no description,  
**Then** item displays without description (no error or placeholder).

**Steps:**
1. Command "test" has no description
2. Open menu showing "test"
3. Verify "test" shown without description, no error

### TC-2.17: Long Description Truncated with Ellipsis
**When** description exceeds terminal width,  
**Then** description is truncated with ellipsis.

**Steps:**
1. Item has description exceeding 80 characters
2. Open menu in 80-column terminal
3. Verify description truncated with "..."

### TC-2.18: Match Count Indicator Shows Position
**When** menu is open,  
**Then** indicator shows current position and total count.

**Steps:**
1. Open menu with 12 items
2. Navigate to 5th item
3. Verify indicator shows "5 of 12" or similar

---

## 3. Menu Filtering

### TC-3.1: Typing While Menu Open Filters Items
**When** user types characters while menu is open,  
**Then** menu items are filtered to show only matches.

**Steps:**
1. Type "server ", Tab (menu shows all subcommands)
2. Note item count
3. Type "con"
4. Verify menu still open
5. Verify fewer items shown (only containing "con")
6. Verify typed text appears in buffer

### TC-3.2: Filter is Case-Insensitive
**When** filtering with uppercase characters,  
**Then** matches are found regardless of case.

**Steps:**
1. Open menu with items
2. Type "CON" (uppercase)
3. Verify matches found (e.g., "connect")

### TC-3.3: Filter Uses Substring Matching
**When** typing a filter that appears in the middle of a word,  
**Then** items containing that substring anywhere are matched.

**Steps:**
1. Open menu containing "config"
2. Type "fig"
3. Verify "config" is still visible (contains "fig")

### TC-3.4: Filtering Resets Selection to First Item
**When** user types a filter character,  
**Then** selection resets to the first matching item.

**Steps:**
1. Open menu
2. Navigate to index 2
3. Type a filter character
4. Verify selection is now at index 0

### TC-3.5: Backspace Removes Filter Character and Expands Results
**When** user presses Backspace on a filtered menu,  
**Then** filter is shortened and more results may appear.

**Steps:**
1. Open menu, type "conn" (filtered)
2. Note filtered count
3. Press Backspace
4. Verify filter is now "con"
5. Verify more or equal items shown

### TC-3.6: Backspace Past Trigger Position Closes Menu
**When** user backspaces past the position where menu was triggered,  
**Then** menu closes.

**Steps:**
1. Type "server " (7 chars), Tab at position 7
2. Backspace once (past trigger position)
3. Verify menu is closed

### TC-3.7: Space Closes Menu (Outside Quotes)
**When** user types space while menu is open (not in quotes),  
**Then** menu closes without accepting selection.

**Steps:**
1. Open menu, navigate to item 2
2. Type space
3. Verify menu closes
4. Verify buffer has original text plus space (selected item NOT inserted)

### TC-3.8: Filter Highlighting Shows Match Position
**When** items are filtered,  
**Then** the matching substring is visually highlighted in each menu item.

**Steps:**
1. Open menu, type "con"
2. Verify "con" portion is highlighted (e.g., blue background) in matching items
3. Verify non-matching portions use normal styling

### TC-3.9: Filter Highlighting Persists During Navigation
**When** navigating with arrows after filtering,  
**Then** filter highlighting remains visible on all matching items.

**Steps:**
1. Filter menu with "con"
2. Press Down Arrow
3. Verify highlighting still present on all matching items

### TC-3.10: No Matches Shows "(no matches)" Message
**When** filter produces zero matching items,  
**Then** menu stays open with "(no matches)" message.

**Steps:**
1. Open menu, type "xyz" (no matches)
2. Verify menu still visible
3. Verify menu shows "(no matches)" message
4. Verify item count is 0

### TC-3.11: Backspace from No Matches Restores Results
**When** backspacing from a "no matches" state,  
**Then** menu restores to showing matching items.

**Steps:**
1. Filter menu to "no matches" state
2. Backspace all filter characters
3. Verify original items are restored

### TC-3.12: Space Inside Quotes Filters Instead of Closing
**When** user types space while menu is open and cursor is inside quotes,  
**Then** space is added to filter and menu stays open.

**Steps:**
1. Type `--path "Program`
2. Open menu
3. Type space
4. Verify menu stays open (space is part of quoted path)

### TC-3.13: Exact Match Keeps Menu Open
**When** filter text exactly matches one completion value,  
**Then** menu stays open and requires explicit acceptance.

**Steps:**
1. Open menu with "help", "helper", "helpful"
2. Type "help" (exact match)
3. Verify menu still open, showing all three items
4. Verify user must press Enter/Tab to accept

### TC-3.14: Navigation Resets to First After Filter Change
**When** user types a filter character after navigating,  
**Then** selection resets to first item.

**Steps:**
1. Open menu, navigate to item 3
2. Type a filter character
3. Verify selection is now at first filtered item

### TC-3.15: Special Characters in Filter Matched Literally
**When** user types special characters like "-", "_", ".",  
**Then** they match literally against completion text.

**Steps:**
1. Open menu with items containing "-" (e.g., "server-dev")
2. Type "-"
3. Verify only items containing "-" are shown

---

## 4. Input Editing

### TC-4.1: Typing Updates Buffer and Cursor
**When** user types characters,  
**Then** they appear in buffer and cursor advances.

**Steps:**
1. Type "h", "e", "l", "p"
2. Verify buffer is "help"
3. Verify cursor is at position 4

### TC-4.2: Backspace Removes Character Before Cursor
**When** user presses Backspace,  
**Then** character before cursor is removed and cursor moves back.

**Steps:**
1. Type "help"
2. Press Backspace
3. Verify buffer is "hel"
4. Verify cursor is at position 3

### TC-4.3: Delete Removes Character at Cursor
**When** user presses Delete,  
**Then** character at cursor position is removed (cursor stays).

**Steps:**
1. Type "help", move cursor to start
2. Press Delete
3. Verify buffer is "elp"
4. Verify cursor is still at position 0

### TC-4.4: Backspace at Start Does Nothing
**When** cursor is at position 0 and user presses Backspace,  
**Then** nothing happens.

**Steps:**
1. Start with empty buffer
2. Press Backspace
3. Verify buffer still empty
4. Verify cursor still at 0

### TC-4.5: Left Arrow Moves Cursor Left
**When** user presses Left Arrow,  
**Then** cursor moves one position left.

**Steps:**
1. Type "hello"
2. Press Left Arrow twice
3. Verify cursor is at position 3

### TC-4.6: Right Arrow Moves Cursor Right
**When** user presses Right Arrow (cursor not at end, no ghost),  
**Then** cursor moves one position right.

**Steps:**
1. Type "hello", move to start
2. Press Right Arrow twice
3. Verify cursor is at position 2

### TC-4.7: Home Key Moves Cursor to Start
**When** user presses Home key,  
**Then** cursor moves to position 0.

**Steps:**
1. Type "hello world"
2. Press Home
3. Verify cursor is at position 0

### TC-4.8: End Key Moves Cursor to End
**When** user presses End key (no ghost text),  
**Then** cursor moves to end of buffer.

**Steps:**
1. Type "hello world", move to middle
2. Press End
3. Verify cursor is at buffer length

### TC-4.9: Insert Text in Middle of Buffer
**When** cursor is in middle of buffer and user types,  
**Then** new text is inserted at cursor position.

**Steps:**
1. Type "helo"
2. Move cursor left 2 positions (after "he")
3. Type "l"
4. Verify buffer is "hello"

### TC-4.10: Delete While Menu Open Closes Menu
**When** Delete is pressed while menu is open,  
**Then** menu closes and character is deleted.

**Steps:**
1. Type "server ", move cursor left
2. Press Tab (if menu opens)
3. Press Delete
4. Verify menu closes

---

## 5. Command & Group Completion

### TC-5.1: Tab After Group Shows Subcommands
**When** Tab is pressed after typing a group name and space,  
**Then** menu shows subcommands and nested groups within that group.

**Steps:**
1. Type "server "
2. Press Tab
3. Verify menu shows subcommands (connect, disconnect, status) and groups (profile)
4. Verify menu does NOT show sibling groups or root commands

### TC-5.2: Tab After Subcommand with Args Shows Arguments
**When** Tab is pressed after a subcommand that has arguments,  
**Then** menu shows available argument names (not sibling commands).

**Steps:**
1. Type "server connect "
2. Press Tab
3. Verify menu shows arguments (--host, --port, --ApiKey, etc.)
4. Verify menu does NOT show sibling commands (disconnect, status)

### TC-5.3: Tab After Subcommand with No Args Shows Nothing
**When** Tab is pressed after a subcommand that has no arguments,  
**Then** no menu appears.

**Steps:**
1. Type "server disconnect " (command has no arguments)
2. Press Tab
3. Verify no menu appears or buffer unchanged

### TC-5.4: Nested Group Navigation
**When** completing through multiple levels of groups,  
**Then** each level shows appropriate completions.

**Steps:**
1. Type "server ", Tab, select "profile", Enter
2. Type space, Tab
3. Verify menu shows profile subcommands (add, remove, etc.)

---

## 6. Argument Name & Alias Completion

### TC-6.1: Double Dash Shows Argument Names
**When** user types "--" after a command,  
**Then** ghost/menu shows available argument names.

**Steps:**
1. Type "version --"
2. Verify ghost shows first argument name (e.g., "Full")
3. Tab shows menu with all argument names

### TC-6.2: Single Dash Shows Argument Aliases
**When** user types "-" after a command,  
**Then** ghost/menu shows available argument aliases.

**Steps:**
1. Type "version -"
2. Verify ghost shows first alias (e.g., "f")
3. Tab shows menu with all aliases

### TC-6.3: Ghost After Dash Shows Only Remainder (Not Full Prefix)
**When** ghost text is shown after a dash prefix,  
**Then** ghost shows only the name/alias remainder, not the prefix.

**Steps:**
1. Type "version -"
2. Verify ghost is "f", NOT "-f"
3. Type additional dash ("version --")
4. Verify ghost is "Full", NOT "--Full"

### TC-6.4: Used Argument Excluded from Menu
**When** an argument has already been used (with value),  
**Then** it does not appear in subsequent completion menus.

**Steps:**
1. Type "server connect --ApiKey value "
2. Press Tab
3. Verify menu does NOT contain --ApiKey
4. Verify other arguments (--host) are still available

### TC-6.5: Used Flag Excluded from Menu
**When** a boolean flag has been used,  
**Then** it does not appear in subsequent completion menus.

**Steps:**
1. Type "server connect --ConfirmDisconnect "
2. Press Tab
3. Verify --ConfirmDisconnect is NOT in menu

### TC-6.6: Boolean Flag Does Not Show Value Completion
**When** a boolean/Option flag is entered and Tab pressed,  
**Then** menu shows other arguments, not file paths or values.

**Steps:**
1. Type "version --Full "
2. Press Tab
3. Verify menu does NOT show directory paths
4. Verify menu shows OTHER arguments (if any), or nothing if no more args

### TC-6.7: Alias Usage Excludes Full Argument Name
**When** an argument alias is used (e.g., -F),  
**Then** the full argument name (--force) is excluded from menu.

**Steps:**
1. Type "deploy -F --"
2. Press Tab
3. Verify --force is NOT in menu (because -F was used)

### TC-6.8: Partial Argument Name Completion
**When** user types partial argument name after "--",  
**Then** Tab completes to matching argument.

**Steps:**
1. Type "server connect --ho"
2. Press Tab
3. Verify buffer becomes "server connect --host"

### TC-6.9: Ghost After Partial Argument Shows Remainder
**When** user types partial argument name,  
**Then** ghost shows remainder of argument.

**Steps:**
1. Type "version --F"
2. Verify ghost shows "ull" (completing "--Full")

### TC-6.10: Command With No Arguments Shows No Argument Completions
**When** Tab is pressed after a command with no defined arguments,  
**Then** no argument completions are shown.

**Steps:**
1. Type "ping " (command has no arguments)
2. Press Tab
3. Verify no menu or empty completions

---

## 7. Argument Value Completion

### TC-7.1: Static Values from Completion Attribute
**When** an argument has [Completion("a", "b", "c")] attribute,  
**Then** those values are offered as completions.

**Steps:**
1. Command has argument with [Completion("dev", "staging", "prod")]
2. Type "deploy --Environment "
3. Tab shows menu with dev, staging, prod

### TC-7.2: Enum Values as Completions
**When** an argument is an enum type,  
**Then** enum values are offered as completions.

**Steps:**
1. Command has Format argument of type OutputFormat enum
2. Type "format --Format "
3. Tab shows menu with Json, Xml, Csv, Text

### TC-7.3: File Path Completion for Marked Arguments
**When** an argument has [FilePathCompletion] attribute,  
**Then** file system paths are offered as completions.

**Steps:**
1. Type "load --Path "
2. Tab shows menu with files/directories from current path

### TC-7.4: Method-Based Completion Provider
**When** an argument uses [Completion(nameof(MethodName))],  
**Then** that method is invoked to provide completions.

**Steps:**
1. Argument has [Completion(nameof(GetEnvironments))]
2. Type "deploy --env "
3. Tab shows values returned by GetEnvironments()

### TC-7.5: Type-Based Completion Provider
**When** an argument uses [Completion(typeof(ProviderType))],  
**Then** provider is resolved from DI and used for completions.

**Steps:**
1. Argument has [Completion(typeof(DatabaseProvider))]
2. Type "query --database "
3. Tab shows databases from provider

### TC-7.6: No Provider Returns No Completions
**When** an argument has no completion attribute and is not enum,  
**Then** Tab produces no completions (silent skip).

**Steps:**
1. Argument has no [Completion] attribute, is string type
2. Type "--name " and Tab
3. Verify no menu appears, no error

### TC-7.7: Provider Returns Empty List
**When** a completion provider returns empty results,  
**Then** "(no matches)" indicator is shown.

**Steps:**
1. Provider returns []
2. Type "--items " and Tab
3. Verify "(no matches)" indicator appears

### TC-7.8: Nullable Enum Argument Completion
**When** argument is nullable enum type (e.g., Format?),  
**Then** enum values are still shown as completions.

**Steps:**
1. Argument is OutputFormat? (nullable)
2. Type "--format "
3. Tab shows enum values (Json, Xml, Csv)

### TC-7.9: Flags Enum Shows Individual Values
**When** argument is a [Flags] enum,  
**Then** individual flag values are shown as completions.

**Steps:**
1. Argument is [Flags] enum Permissions { Read, Write, Execute }
2. Type "--permissions "
3. Tab shows Read, Write, Execute

### TC-7.10: Explicit Attribute Overrides Enum Auto-Completion
**When** enum argument has explicit [Completion] attribute,  
**Then** explicit values are used instead of enum values.

**Steps:**
1. Enum argument has [Completion("custom1", "custom2")]
2. Type "--format "
3. Tab shows "custom1", "custom2" (NOT enum values)

---

## 8. Positional Argument Completion

### TC-8.1: First Positional Slot with Custom Completion
**When** a command has a positional argument with [Completion(nameof(Method))] attribute,  
**Then** Tab after command invokes that method for completions.

**Steps:**
1. Copy command has Source at position 0 with custom completion function
2. Type "copy "
3. Tab shows menu with items from GetSourceCompletions()

### TC-8.2: Second Positional Slot Completion
**When** first positional is filled and Tab pressed,  
**Then** completions for second positional slot are shown.

**Steps:**
1. Type "copy file1.txt "
2. Press Tab
3. Verify menu shows position 1 completions (backup/, archive/, output.txt)

### TC-8.3: Positional Argument Provider Type
**When** a positional argument has [Completion(typeof(Provider))] attribute,  
**Then** that provider type is used for completions.

**Steps:**
1. Connect command has Profile with TestProfileProvider
2. Type "connect "
3. Tab shows profiles from provider (prod, staging, dev, local)

### TC-8.4: Positional Without Completion Falls Back to Arguments
**When** a positional argument has no completion attribute,  
**Then** Tab shows named arguments instead.

**Steps:**
1. Process command has Input (positional, no completion)
2. Type "process "
3. Tab shows --OutputFormat, --Quiet, etc. (named arguments)

### TC-8.5: Partial Positional Value Filtering
**When** user types partial text for a positional slot,  
**Then** Tab filters completions by that prefix.

**Steps:**
1. Type "copy fi"
2. Press Tab
3. Verify completions filtered to "file1.txt", "file2.txt" (matching "fi")
4. Verify "data.csv" is NOT shown

### TC-8.6: IsRest Variadic Positional Continues Completing
**When** a positional argument has IsRest=true,  
**Then** completion continues to offer values for additional positions.

**Steps:**
1. Delete command has Files with IsRest=true
2. Type "delete file1.txt "
3. Tab still shows file completions for next position
4. Type "file2.txt "
5. Tab still shows file completions (continues indefinitely)

### TC-8.7: Double-Dash Switches to Options Mode
**When** user types "--" while in positional context,  
**Then** completion switches to show named arguments instead of positional values.

**Steps:**
1. Type "copy --"
2. Press Tab
3. Verify menu shows --Force, --Verbose (named options, not file completions)

### TC-8.8: Single-Dash Switches to Aliases Mode
**When** user types "-" while in positional context,  
**Then** completion switches to show argument aliases.

**Steps:**
1. Type "copy -"
2. Press Tab
3. Verify menu shows -f, -v (aliases)

### TC-8.9: All Positionals Filled Shows Options Only
**When** all positional slots are filled,  
**Then** Tab shows remaining named options only.

**Steps:**
1. Type "copy file1.txt backup/ " (both positionals filled)
2. Press Tab
3. Verify menu shows --Force, --Verbose
4. Verify menu does NOT show positional completions

### TC-8.10: Named Argument Satisfies Positional Slot
**When** positional slot is filled via named argument syntax,  
**Then** that slot is considered filled for completion purposes.

**Steps:**
1. Type "copy --Source file1.txt "
2. Press Tab
3. Verify menu shows position 1 completions (backup/, archive/) since pos0 is satisfied

### TC-8.11: Positional Filled Excludes Corresponding Named Argument
**When** a positional slot is filled by position,  
**Then** the corresponding --ArgName is excluded from completion.

**Steps:**
1. Type "copy file1.txt backup/ " (both positionals filled)
2. Press Tab
3. Verify --Source is NOT in menu (filled by first positional)
4. Verify --Destination is NOT in menu (filled by second positional)

---

## 9. File Path Completion

### TC-9.1: File Upload Source Shows Local Paths
**When** completing file upload Source argument with [FilePathCompletion],  
**Then** local file system paths are shown.

**Steps:**
1. Type "file upload "
2. Tab shows local files and directories

### TC-9.2: Directory Entries Shown with Trailing Slash
**When** file path completion activates,  
**Then** directories are shown with trailing slash (e.g., "bin/").

**Steps:**
1. Type "file upload "
2. Tab shows directories (bin/, obj/) and files (config.json)

### TC-9.3: Remote File Path Completion
**When** argument has [RemoteFilePathCompletion] attribute and connected to server,  
**Then** remote server paths are queried for completion.

**Steps:**
1. Connect to server
2. Type "file download "
3. Tab queries server for remote file paths

### TC-9.4: File Path Partial Filtering
**When** user types partial file name,  
**Then** file completions are filtered by prefix.

**Steps:**
1. Type "file upload con"
2. Tab shows only files matching "con" (e.g., config.json)

### TC-9.5: Hidden Files Included in Completion
**When** file path completion is triggered,  
**Then** hidden files (starting with ".") are included.

**Steps:**
1. CWD has .gitignore file
2. Type "--file ."
3. Tab shows ".gitignore" in menu

### TC-9.6: Subdirectory Navigation
**When** user types partial path ending in directory,  
**Then** Tab shows contents of that subdirectory.

**Steps:**
1. Type "--file src/"
2. Tab shows contents of src/ directory

### TC-9.7: Parent Directory Navigation
**When** user types "../",  
**Then** Tab shows contents of parent directory.

**Steps:**
1. Type "--file ../"
2. Tab shows contents of parent directory

### TC-9.8: Absolute Path Completion
**When** user types absolute path,  
**Then** completion works from that root.

**Steps:**
1. Type "--file C:/" (Windows) or "/" (Unix)
2. Tab shows root directory contents

### TC-9.9: Spaces in Paths Handled
**When** file name contains spaces,  
**Then** completion properly escapes or quotes the path.

**Steps:**
1. File "my file.txt" exists
2. Tab shows "my file.txt" properly handled
3. Accepting inserts quoted or escaped path

### TC-9.10: Network Path Completion (Windows)
**When** user types UNC path on Windows,  
**Then** completion attempts with timeout.

**Steps:**
1. Type "--file \\\\server\\share\\"
2. Tab attempts to list with reasonable timeout
3. Either shows contents or times out gracefully

### TC-9.11: Permission Denied Returns Empty
**When** user has no permission to read directory,  
**Then** completion returns empty (no error shown).

**Steps:**
1. Type "--file /root/" (as non-root user)
2. Tab shows empty or "(no matches)"
3. No error message or crash

### TC-9.12: DirectoryPathCompletion Shows Only Directories
**When** argument has [DirectoryPathCompletion],  
**Then** only directories are shown, not files.

**Steps:**
1. Argument has [DirectoryPathCompletion]
2. CWD has file.txt and folder/
3. Tab shows "folder/" only, not "file.txt"

---

## 10. Viewport Scrolling

### TC-10.1: Scroll When Navigating Past Viewport
**When** menu has more items than visible viewport and user navigates past visible area,  
**Then** menu scrolls to keep selected item visible.

**Steps:**
1. Open menu with 15+ items (viewport shows 10)
2. Navigate down 11 times
3. Verify item 11 is visible on screen
4. Verify selected item is always in view

### TC-10.2: Scroll Indicators Show More Items Above
**When** menu has scrolled past the first items,  
**Then** "↑ N more" indicator shows count of hidden items above.

**Steps:**
1. Open long menu
2. Navigate past viewport
3. Verify "↑ N more" indicator appears

### TC-10.3: Scroll Indicators Show More Items Below
**When** menu has items below the viewport,  
**Then** "↓ N more" indicator shows count of hidden items below.

**Steps:**
1. Open long menu (not scrolled)
2. Verify "↓ N more" indicator appears if more items exist

### TC-10.4: Selection Highlighted After Scroll
**When** menu scrolls to show new items,  
**Then** selected item is still visually highlighted (inverted).

**Steps:**
1. Scroll menu to item 11
2. Verify item 11 has selection styling

### TC-10.5: Scroll Back with Up Arrow
**When** navigating back up from scrolled position,  
**Then** viewport scrolls back to show earlier items.

**Steps:**
1. Scroll down to item 11
2. Press Up Arrow
3. Verify item 10 is visible and selected

---

## 11. Ghost & Menu Interaction

### TC-11.1: Ghost Hidden When Menu Opens
**When** Tab opens a menu while ghost text is visible,  
**Then** ghost text is cleared from display.

**Steps:**
1. Type "server " (ghost shows "profile")
2. Press Tab (menu opens)
3. Verify ghost text is no longer visible
4. Verify menu is displayed

### TC-11.2: Ghost Returns After Menu Escape
**When** menu is closed with Escape,  
**Then** ghost text reappears (if still applicable).

**Steps:**
1. Type "server " (ghost shows "profile")
2. Press Tab (menu opens, ghost hidden)
3. Press Escape (menu closes)
4. Verify ghost text "profile" reappears

### TC-11.3: Ghost Updated After Menu Accept
**When** menu selection is accepted,  
**Then** ghost text updates based on new buffer content.

**Steps:**
1. Type "server ", Tab, select "connect", Enter
2. Type " "
3. Verify new ghost text for argument context appears

---

## 12. Multi-Step Workflows

### TC-12.1: Complete Type-Tab-Navigate-Enter Flow
**When** user completes a full interaction workflow,  
**Then** each step behaves correctly and final command is correct.

**Steps:**
1. Type "server "
2. Tab (menu opens)
3. Down Arrow (select second item)
4. Enter (accept)
5. Verify buffer contains selected subcommand
6. Verify cursor at end

### TC-12.2: Chain of Completions
**When** completing multiple levels in sequence,  
**Then** each level properly transitions to the next.

**Steps:**
1. Type "serv", Tab (completes to "server")
2. Type space, Tab (menu shows subcommands)
3. Accept selection
4. Type space, Tab (shows arguments)
5. Verify each transition is smooth

### TC-12.3: Cache Invalidation After Completion
**When** an argument is completed and used,  
**Then** subsequent completions properly exclude it.

**Steps:**
1. Type "server connect ", Tab (cache populated, shows --ApiKey, --host, etc.)
2. Select --ApiKey, Enter
3. Type "value ", Tab again
4. Verify --ApiKey is NOT in second menu (cache updated)

### TC-12.4: Tab-Escape-Tab Reopens Menu
**When** menu is closed with Escape and Tab pressed again,  
**Then** menu reopens.

**Steps:**
1. Type "server ", Tab (menu opens)
2. Escape (menu closes)
3. Tab again
4. Verify menu reopens

---

## 13. History Navigation

### TC-13.1: Up Arrow Navigates History When Menu Closed
**When** menu is closed and user presses Up Arrow,  
**Then** previous command from history is loaded into buffer.

**Steps:**
1. Execute several commands to build history
2. At empty prompt, press Up Arrow
3. Verify previous command appears in buffer

### TC-13.2: Up Arrow in Menu Navigates Menu, Not History
**When** menu is open and user presses Up Arrow,  
**Then** it navigates menu selection (not history).

**Steps:**
1. Type "server ", Tab (menu opens)
2. Down Arrow to index 1
3. Press Up Arrow
4. Verify selection moved to index 0 (not history loaded)
5. Verify buffer still shows "server "

### TC-13.3: Down Arrow Returns Through History
**When** navigating back through history with Up Arrow,  
**Then** Down Arrow returns toward most recent entry.

**Steps:**
1. Go up through history twice
2. Press Down Arrow
3. Verify more recent command appears

### TC-13.4: History Navigation Then Tab Works
**When** user loads command from history then presses Tab,  
**Then** completion works on the loaded command.

**Steps:**
1. Navigate to history entry
2. Press Tab
3. Verify completion works on loaded buffer

---

## 14. Edge Cases & Error Handling

### TC-14.1: Double Space Handling
**When** user types multiple consecutive spaces,  
**Then** completion handles gracefully without errors.

**Steps:**
1. Type "server  " (double space)
2. Press Tab
3. Verify no crash, graceful behavior

### TC-14.2: Tab Mid-Word
**When** cursor is in the middle of a word and Tab pressed,  
**Then** completion context is determined by cursor position.

**Steps:**
1. Type "serverxxx"
2. Move cursor back 3 positions (after "server")
3. Press Tab
4. Verify behavior is based on "server" context, not "serverxxx"

### TC-14.3: Multiple Escape Presses
**When** user presses Escape multiple times,  
**Then** no errors occur.

**Steps:**
1. Press Escape 5 times in succession
2. Verify no crash or unexpected state

### TC-14.4: Left Arrow at Position Zero
**When** cursor is at position 0 and user presses Left Arrow,  
**Then** nothing happens (no error, cursor stays at 0).

**Steps:**
1. Move cursor to position 0
2. Press Left Arrow
3. Verify cursor still at 0, no error

### TC-14.5: Right Arrow at End with No Ghost
**When** cursor is at end with no ghost text and user presses Right Arrow,  
**Then** nothing happens.

**Steps:**
1. Type "server" (exact match, no ghost)
2. Press Right Arrow
3. Verify buffer unchanged, position unchanged

### TC-14.6: Home Key While Menu Open Closes Menu
**When** user presses Home while menu is open,  
**Then** menu closes and cursor moves to start.

**Steps:**
1. Type "server ", Tab
2. Press Home
3. Verify menu closed, cursor at position 0

### TC-14.7: End Key While Menu Open Closes Menu
**When** user presses End while menu is open,  
**Then** menu closes and cursor moves to end.

**Steps:**
1. Type "server ", Tab
2. Press End
3. Verify menu closed, cursor at buffer end

### TC-14.8: Cursor in Middle - Arguments After Cursor Excluded
**When** cursor is in middle of input and argument exists after cursor,  
**Then** that argument is still excluded from completions.

**Steps:**
1. Type "server connect --ApiKey "
2. Move cursor back to after "connect"
3. Type space, Tab
4. Verify --ApiKey is NOT in menu (even though it's after cursor)

### TC-14.9: Escape With No Menu Is No-Op
**When** Escape is pressed with no menu open,  
**Then** nothing happens.

**Steps:**
1. Type "hello"
2. Press Escape
3. Verify buffer unchanged

### TC-14.10: Rapid Tab and Arrow Keys Maintains State
**When** user rapidly presses Tab and arrows,  
**Then** state remains consistent.

**Steps:**
1. Type "server "
2. Rapidly: Tab, Down, Down, Up, Escape, Tab
3. Verify menu visible, state consistent

### TC-14.11: Rapid Backspace Works Correctly
**When** user rapidly presses Backspace,  
**Then** characters are deleted correctly.

**Steps:**
1. Type "server connect"
2. Rapidly press Backspace 7 times
3. Verify buffer is "server "

### TC-14.12: Special Characters Handled
**When** user types special characters (@, =, etc.),  
**Then** they are handled correctly.

**Steps:**
1. Type "test@value --param=value"
2. Verify buffer contains "test@value --param=value"

### TC-14.13: Quoted Strings Handled
**When** user types quoted strings,  
**Then** they are handled correctly.

**Steps:**
1. Type `server connect --host "my host"`
2. Verify buffer contains the quoted string

### TC-14.14: Long Input Near Console Width
**When** user types text approaching console width,  
**Then** display handles correctly without corruption.

**Steps:**
1. Type 60+ characters
2. Verify buffer contains full text
3. Verify display is correct

### TC-14.15: Backspace While Menu Open Closes Menu
**When** Backspace is pressed while menu is open,  
**Then** menu closes and character is removed.

**Steps:**
1. Type "server ", Tab
2. Press Backspace
3. Verify menu closed
4. Verify buffer is "server"

### TC-14.16: Unicode in Command Names
**When** command names contain Unicode characters,  
**Then** completion displays correctly.

**Steps:**
1. Command "日本語コマンド" registered
2. Tab pressed
3. Verify Unicode displays correctly in menu

### TC-14.17: Special Characters in Completion Values
**When** completion values contain special characters like [ ] @ =,  
**Then** they are properly escaped/quoted in menu and on acceptance.

**Steps:**
1. File "file[1].txt" exists
2. Tab pressed with file provider
3. Verify proper escaping in menu
4. Verify acceptance properly escapes

### TC-14.18: Empty String in Provider Results
**When** provider returns empty string as a value,  
**Then** it is handled gracefully (skipped or shown safely).

**Steps:**
1. Provider returns ["", "valid"]
2. Tab pressed
3. Verify no crash, empty string handled

### TC-14.19: Null in Provider Results
**When** provider returns null in results array,  
**Then** null is handled gracefully.

**Steps:**
1. Provider returns [null, "valid"]
2. Tab pressed
3. Verify no crash, null filtered out

### TC-14.20: Very Long Completion Value Truncated
**When** a completion value exceeds terminal width,  
**Then** value is truncated with ellipsis.

**Steps:**
1. Completion value is 200 characters
2. Menu displayed in 80-column terminal
3. Verify value truncated with "..."

### TC-14.21: Provider Throws Exception
**When** a completion provider throws an exception,  
**Then** error is logged and graceful failure shown.

**Steps:**
1. Provider throws InvalidOperationException
2. Tab pressed
3. Verify no crash, graceful error handling
4. Verify error logged

### TC-14.22: Provider Cancellation Token Respected
**When** user presses Escape during async completion,  
**Then** cancellation token is triggered.

**Steps:**
1. Provider is async, takes time
2. Tab pressed, then Escape
3. Verify provider cancellation triggered

### TC-14.23: End-of-Options Separator (--)
**When** user types bare "--" separator,  
**Then** subsequent tokens are treated as positional values.

**Steps:**
1. Type "rm -- -rf.txt"
2. Verify "-rf.txt" treated as filename, not option
3. Tab after "--" shows positional completions

### TC-14.24: Excess Positional Arguments Error
**When** more positional values provided than defined (no IsRest),  
**Then** appropriate error is indicated.

**Steps:**
1. Command has 2 positional args, no IsRest
2. Type value1 value2 value3
3. Verify error for excess positional

### TC-14.25: Missing Required Positional Error
**When** required positional argument not provided,  
**Then** appropriate error is indicated.

**Steps:**
1. Command has required positional at position 0
2. Submit without providing value
3. Verify error indicating missing positional

### TC-14.26: Ctrl+C During Menu Cancels
**When** user presses Ctrl+C while menu is open,  
**Then** menu closes and current operation is cancelled.

**Steps:**
1. Open menu
2. Press Ctrl+C
3. Verify menu closed, operation cancelled

### TC-14.27: Deletion of Autosuggestion from History
**When** user presses Shift+Delete on autosuggestion,  
**Then** that entry is removed from history.

**Steps:**
1. Ghost shows completion from history
2. Press Shift+Delete
3. Verify entry removed from history
4. Verify ghost updates

---

## 15. Visual Rendering

### TC-15.1: Menu Shows Selection with Inverted Style
**When** menu is displayed,  
**Then** selected item has inverted (reverse video) styling.

**Steps:**
1. Open menu
2. Verify first item has inverted foreground/background

### TC-15.2: Ghost Text Styled Dim/Gray
**When** ghost text is displayed,  
**Then** it uses dim styling to distinguish from user input.

**Steps:**
1. Type "s" (ghost shows "erver")
2. Verify ghost portion is styled dim/gray
3. Verify user-typed "s" is normal styling

### TC-15.3: Menu Updates In-Place (No Duplicate Lines)
**When** navigating through menu,  
**Then** menu updates in place without leaving duplicate lines.

**Steps:**
1. Open menu
2. Navigate up and down several times
3. Verify only one menu is visible (no leftover/duplicate lines)

### TC-15.4: Menu Cleared on Close
**When** menu closes,  
**Then** menu lines are completely cleared from display.

**Steps:**
1. Open menu (takes N lines)
2. Press Escape
3. Verify those N lines are cleared, not left behind

### TC-15.5: Scroll Indicator Styling
**When** menu has scroll indicators,  
**Then** they are styled to be visible but distinct from items.

**Steps:**
1. Open long menu, scroll down
2. Verify "↑ N more" indicator is visible and readable

---

## 16. Submission Behavior

### TC-16.1: Enter With No Menu Submits Input
**When** Enter is pressed with no menu open,  
**Then** the current buffer is submitted.

**Steps:**
1. Type "server connect"
2. Press Enter (no menu open)
3. Verify buffer content is submitted

### TC-16.2: Empty Buffer Submission
**When** Enter is pressed with empty buffer,  
**Then** empty string is submitted (no crash).

**Steps:**
1. Start with empty buffer
2. Press Enter
3. Verify empty submission handled gracefully

### TC-16.3: Type-Menu-Escape-Submit Workflow
**When** user opens menu, escapes, then submits,  
**Then** original buffer (before menu) is submitted.

**Steps:**
1. Type "server "
2. Tab (menu opens)
3. Escape (menu closes)
4. Enter (submit)
5. Verify "server " is submitted

---

## 17. Remote Completion

### TC-17.1: Loading Indicator Shown During Remote Fetch
**When** Tab triggers remote completion,  
**Then** loading indicator appears immediately.

**Steps:**
1. Connected to remote server
2. Type "--remoteArg " and Tab
3. Verify loading indicator appears immediately
4. Verify indicator disappears when results arrive

### TC-17.2: Remote Results Replace Loading Indicator
**When** remote server responds with results,  
**Then** loading indicator is replaced with menu.

**Steps:**
1. Trigger remote completion
2. Wait for server response
3. Verify menu shows results, loading gone

### TC-17.3: Remote Fetch Cancelled on Additional Typing
**When** user types while remote fetch is in progress,  
**Then** current fetch is cancelled, new debounced fetch starts.

**Steps:**
1. Trigger remote completion (fetch in progress)
2. Wait 100ms, type additional character
3. Verify previous fetch cancelled
4. Verify new fetch started after debounce

### TC-17.4: Network Timeout After 3 Seconds
**When** server doesn't respond within 3 seconds,  
**Then** timeout error is shown, user can retry.

**Steps:**
1. Trigger remote completion
2. Server doesn't respond for 3+ seconds
3. Verify timeout message shown
4. Verify user can press Tab to retry

### TC-17.5: Network Error Graceful Handling
**When** network connection fails,  
**Then** error message shown, input still usable.

**Steps:**
1. Connected but network fails
2. Trigger remote completion
3. Verify brief error message
4. Verify user can continue typing

### TC-17.6: Server Returns Error (500)
**When** server returns HTTP 500 error,  
**Then** error shown, user can retry manually.

**Steps:**
1. Server returns 500 Internal Server Error
2. Trigger remote completion
3. Verify error message
4. Verify Tab allows manual retry

### TC-17.7: Disconnected State Shows Offline Indicator
**When** not connected to server,  
**Then** "(offline)" indicator shown with cached results if available.

**Steps:**
1. Not connected to server
2. Tab on remote argument
3. Verify "(offline)" indicator
4. Verify cached results used if available

### TC-17.8: User Can Retry After Error
**When** previous Tab failed,  
**Then** pressing Tab again attempts new fetch.

**Steps:**
1. Remote completion fails
2. Press Tab again
3. Verify new fetch attempted

### TC-17.9: Rapid Typing Debounced (100ms)
**When** user types 5 characters quickly,  
**Then** only one fetch occurs after 100ms idle.

**Steps:**
1. Type 5 characters rapidly (within 100ms each)
2. Verify single fetch after typing stops
3. Verify not 5 separate fetches

### TC-17.10: Escape During Fetch Cancels Request
**When** remote fetch is in progress and user presses Escape,  
**Then** fetch is cancelled, no menu shown.

**Steps:**
1. Trigger remote completion
2. Press Escape during loading
3. Verify fetch cancelled
4. Verify no menu appears

### TC-17.11: Non-Blocking During Remote Fetch
**When** remote fetch is in progress,  
**Then** user can continue typing without lag.

**Steps:**
1. Trigger remote completion
2. Continue typing while loading
3. Verify characters appear immediately
4. Verify no input lag

### TC-17.12: Connection Indicator Shows Status
**When** completing remote argument,  
**Then** visual indicator shows connected/offline status.

**Steps:**
1. Connected to server
2. Tab on remote argument
3. Verify connection status visible

---

## 18. Caching Behavior

### TC-18.1: Cache Hit Returns Instant Results
**When** same completion was fetched previously,  
**Then** cached results appear instantly (no loading).

**Steps:**
1. Tab on "--env", get results
2. Tab on "--env" again
3. Verify results appear instantly
4. Verify no loading indicator

### TC-18.2: Different Argument Is Cache Miss
**When** different argument is completed,  
**Then** new fetch is triggered.

**Steps:**
1. Tab on "--env", cache populated
2. Tab on "--region" (different arg)
3. Verify new fetch triggered

### TC-18.3: Cache Invalidated After Command Execution
**When** command is executed,  
**Then** cache for that command's arguments is cleared.

**Steps:**
1. Tab on "--env", cache populated
2. Execute the command
3. Tab on "--env" again
4. Verify fresh fetch (not cached)

### TC-18.4: Cache TTL Expiry (5 minutes)
**When** cache entry is older than 5 minutes,  
**Then** fresh fetch is triggered.

**Steps:**
1. Tab on "--env", cache populated
2. Wait 5+ minutes
3. Tab on "--env" again
4. Verify fresh fetch triggered

### TC-18.5: Cache Context Sensitivity
**When** prior arguments change completion context,  
**Then** cache accounts for context.

**Steps:**
1. Type "--server prod --env ", Tab (cache for prod context)
2. Clear, type "--server dev --env ", Tab
3. Verify new fetch (context changed)

### TC-18.6: Local Filtering on Cached Results
**When** typing to filter cached remote results,  
**Then** filtering happens locally (no network).

**Steps:**
1. Tab, get cached results [dev, staging, prod]
2. Type "d"
3. Verify filters to [dev] locally
4. Verify no network call for filtering

### TC-18.7: Prefix Query Reuses Cache
**When** completing with partial prefix that extends cached empty query,  
**Then** cached full results are filtered locally.

**Steps:**
1. Tab with empty query, cache [dev, staging, prod]
2. Type "st", Tab
3. Verify cached results filtered to [staging]
4. Verify no new fetch

---

## 19. Provider & Attribute Configuration

### TC-19.1: Single String Interpreted as Method Name
**When** [Completion("GetValues")] with single string,  
**Then** it's treated as method name, not static value.

**Steps:**
1. [Completion("GetEnvironments")] on argument
2. Tab pressed
3. Verify GetEnvironments() method called
4. Verify NOT showing literal "GetEnvironments"

### TC-19.2: Two Strings Interpreted as Static Values
**When** [Completion("a", "b")] with two strings,  
**Then** they're treated as static values.

**Steps:**
1. [Completion("dev", "prod")] on argument
2. Tab pressed
3. Verify menu shows "dev", "prod"

### TC-19.3: Three or More Strings as Static Values
**When** [Completion("a", "b", "c", "d")] with 3+ strings,  
**Then** all are treated as static values.

**Steps:**
1. [Completion("a", "b", "c", "d")] on argument
2. Tab pressed
3. Verify menu shows all four values

### TC-19.4: Type Interpreted as Provider
**When** [Completion(typeof(MyProvider))],  
**Then** MyProvider is resolved from DI.

**Steps:**
1. [Completion(typeof(DatabaseProvider))] on argument
2. Tab pressed
3. Verify DatabaseProvider resolved and invoked

### TC-19.5: nameof() Resolves to Method Name
**When** [Completion(nameof(GetItems))],  
**Then** it resolves to method name string.

**Steps:**
1. [Completion(nameof(GetItems))] where GetItems exists
2. Tab pressed
3. Verify GetItems() method called

### TC-19.6: Method Returns Empty Enumerable
**When** completion method returns empty enumerable,  
**Then** "(no matches)" shown.

**Steps:**
1. Method returns Enumerable.Empty<string>()
2. Tab pressed
3. Verify "(no matches)" indicator

### TC-19.7: Method Returns Null
**When** completion method returns null,  
**Then** treated as empty, no crash.

**Steps:**
1. Method returns null
2. Tab pressed
3. Verify treated as empty
4. Verify no crash

### TC-19.8: Method Not Found at Registration
**When** [Completion("NonExistent")] with non-existent method,  
**Then** error at registration/startup.

**Steps:**
1. [Completion("NonExistent")] where method doesn't exist
2. Application starts
3. Verify error at startup, not runtime

### TC-19.9: Method Wrong Return Type
**When** completion method returns int instead of IEnumerable<string>,  
**Then** error at registration/startup.

**Steps:**
1. Method returns int
2. Application starts
3. Verify error at startup

### TC-19.10: Static Method Works
**When** completion method is static,  
**Then** it's invoked correctly.

**Steps:**
1. Static method GetEnvironments()
2. Tab pressed
3. Verify static method called successfully

### TC-19.11: Instance Method Works
**When** completion method is instance method on command,  
**Then** it's invoked on command instance.

**Steps:**
1. Instance method GetEnvironments() on command class
2. Tab pressed
3. Verify instance method called

### TC-19.12: Method with DI Parameters
**When** method has service parameters (ILogger, IFileSystem),  
**Then** services are injected.

**Steps:**
1. Method GetEnvs(ILogger log, IFileSystem fs)
2. Tab pressed
3. Verify services injected correctly

### TC-19.13: Method DI Parameter Not Registered
**When** method requires unregistered service,  
**Then** graceful error, logged.

**Steps:**
1. Method requires unregistered ICustomService
2. Tab pressed
3. Verify graceful error handling
4. Verify error logged

### TC-19.14: Provider Not Registered in DI
**When** [Completion(typeof(UnregisteredProvider))],  
**Then** graceful error message.

**Steps:**
1. UnregisteredProvider not in DI
2. Tab pressed
3. Verify graceful error message
4. Verify error logged

### TC-19.15: Multiple Arguments Same Provider
**When** two arguments use same provider type,  
**Then** both work independently.

**Steps:**
1. Two args both use [FilePathCompletion]
2. Tab on first, then Tab on second
3. Verify both work correctly

### TC-19.16: Provider Registered as Singleton
**When** provider is AddSingleton,  
**Then** same instance used across invocations.

**Steps:**
1. Provider registered as singleton
2. Tab twice
3. Verify same instance used

### TC-19.17: Provider Registered as Transient
**When** provider is AddTransient,  
**Then** new instance each invocation.

**Steps:**
1. Provider registered as transient
2. Tab twice
3. Verify different instances

### TC-19.18: Shortcut Attribute [FilePathCompletion]
**When** using [FilePathCompletion] shortcut,  
**Then** FilePathProvider is used.

**Steps:**
1. [FilePathCompletion] on argument
2. Tab pressed
3. Verify file paths shown

### TC-19.19: Shortcut Attribute [DirectoryPathCompletion]
**When** using [DirectoryPathCompletion] shortcut,  
**Then** DirectoryPathProvider is used.

**Steps:**
1. [DirectoryPathCompletion] on argument
2. Tab pressed
3. Verify only directories shown

### TC-19.20: Custom Shortcut Attribute
**When** custom attribute inherits CompletionAttribute,  
**Then** its provider type is used.

**Steps:**
1. [ConfigFileCompletion] : CompletionAttribute
2. Tab pressed
3. Verify ConfigFileProvider used

---

## 20. Match Ranking & Ordering

### TC-20.1: Case-Insensitive Matching
**When** typing lowercase to match uppercase,  
**Then** matches are found.

**Steps:**
1. Command "Help" registered
2. Type "hel"
3. Tab shows "Help" in menu

### TC-20.2: Prefix Matches Ranked First
**When** both prefix and contains matches exist,  
**Then** prefix matches appear first.

**Steps:**
1. Commands: "server", "laser", "reserve"
2. Type "ser"
3. Tab shows "server" first (prefix), then "laser", "reserve"

### TC-20.3: Exact Match Prioritized in Ordering
**When** exact match exists among multiple matches,  
**Then** exact match appears first.

**Steps:**
1. Commands: "test", "testing", "attest"
2. Type "test"
3. Tab shows "test" first (exact match)

### TC-20.4: Multiple Prefix Matches Show Menu
**When** multiple items have the same prefix,  
**Then** menu shows all matches (no auto-accept).

**Steps:**
1. Commands: "help", "helper", "helpful"
2. Type "help"
3. Tab shows menu with all 3 items

### TC-20.5: Matched Portion Highlighted
**When** items are matched,  
**Then** matched portion is visually distinct.

**Steps:**
1. Type "con"
2. Tab shows "connect"
3. Verify "con" portion is highlighted

---

## 21. Result Limiting & Truncation

### TC-21.1: Maximum 100 Items Cached
**When** source returns 500 items,  
**Then** only 100 are cached/displayed.

**Steps:**
1. Provider returns 500 items
2. Tab pressed
3. Verify menu shows max 100 items

### TC-21.2: Truncation Indicator Shown
**When** results are truncated,  
**Then** "showing 100 of 500" indicator appears.

**Steps:**
1. Provider returns 500 items
2. Tab pressed
3. Verify truncation indicator visible

### TC-21.3: Exactly 100 Items No Indicator
**When** exactly 100 items returned,  
**Then** no truncation indicator.

**Steps:**
1. Provider returns exactly 100 items
2. Tab pressed
3. Verify no truncation indicator

### TC-21.4: Under 100 Items No Indicator
**When** fewer than 100 items returned,  
**Then** no truncation indicator.

**Steps:**
1. Provider returns 50 items
2. Tab pressed
3. Verify shows "1 of 50", no truncation

### TC-21.5: Filtering Reduces Truncation Impact
**When** user filters truncated results,  
**Then** count updates appropriately.

**Steps:**
1. 500 items, showing 100
2. Type filter reducing to 5 matches
3. Verify shows "1 of 5", no truncation

---

## 22. Terminal & Environment Edge Cases

### TC-22.1: Terminal Resize During Menu
**When** terminal is resized while menu is open,  
**Then** menu repositions or closes gracefully.

**Steps:**
1. Open menu
2. Resize terminal window
3. Verify menu handles gracefully (no crash/corruption)

### TC-22.2: Very Narrow Terminal (40 columns)
**When** terminal is only 40 columns wide,  
**Then** menu renders without breaking.

**Steps:**
1. Set terminal to 40 columns
2. Open menu
3. Verify menu renders correctly

### TC-22.3: Minimum Terminal Width (80 columns)
**When** terminal is standard 80 columns,  
**Then** all content is readable.

**Steps:**
1. Set terminal to 80 columns
2. Open menu with descriptions
3. Verify all content readable

### TC-22.4: Tab During Command Execution
**When** command is running and Tab is pressed,  
**Then** autocomplete works or is gracefully ignored.

**Steps:**
1. Start long-running command
2. Press Tab
3. Verify no crash, graceful handling

### TC-22.5: Terminal Without ANSI Support
**When** terminal doesn't support ANSI codes,  
**Then** completion still works (degrades gracefully).

**Steps:**
1. Disable ANSI support
2. Open menu
3. Verify functional without colors

### TC-22.6: Very Long Path Exceeds 260 Characters (Windows)
**When** file path exceeds Windows MAX_PATH,  
**Then** completion handles with extended path prefix.

**Steps:**
1. Create deeply nested path >260 chars
2. Navigate into path with Tab
3. Verify completion works

---

## 23. Keyboard Variations

### TC-23.1: Ctrl+Space Inserts Space Without Accepting
**When** Ctrl+Space is pressed,  
**Then** space is inserted without expanding abbreviation/accepting.

**Steps:**
1. Type partial text
2. Press Ctrl+Space
3. Verify space inserted, no completion accepted

### TC-23.2: Alt+Enter Inserts Newline
**When** Alt+Enter is pressed,  
**Then** newline is inserted at cursor position.

**Steps:**
1. Type partial command
2. Press Alt+Enter
3. Verify newline inserted for multi-line command

### TC-23.3: Ctrl+W Removes Path Component
**When** Ctrl+W is pressed,  
**Then** previous path component is removed.

**Steps:**
1. Type "src/components/Button"
2. Press Ctrl+W
3. Verify buffer is "src/components/"

### TC-23.4: Alt+D Moves Next Word to Kill Ring
**When** Alt+D is pressed,  
**Then** next word is removed and stored.

**Steps:**
1. Type "hello world", cursor at start
2. Press Alt+D
3. Verify "hello" removed, "world" remains

### TC-23.5: Ctrl+K Deletes to End of Line
**When** Ctrl+K is pressed,  
**Then** text from cursor to end is removed.

**Steps:**
1. Type "hello world", cursor at position 5
2. Press Ctrl+K
3. Verify buffer is "hello"

### TC-23.6: Ctrl+U Deletes to Start of Line
**When** Ctrl+U is pressed,  
**Then** text from start to cursor is removed.

**Steps:**
1. Type "hello world", cursor at position 5
2. Press Ctrl+U
3. Verify buffer is " world"

### TC-23.7: Ctrl+T Transposes Characters
**When** Ctrl+T is pressed,  
**Then** last two characters are swapped.

**Steps:**
1. Type "teh"
2. Press Ctrl+T
3. Verify buffer is "the"

### TC-23.8: Ctrl+L Clears Screen
**When** Ctrl+L is pressed,  
**Then** screen is cleared, prompt redrawn.

**Steps:**
1. Type partial command
2. Press Ctrl+L
3. Verify screen cleared, input preserved

---

## 24. Context Sensitivity

### TC-24.1: Completion Context Includes Prior Arguments
**When** argument value depends on prior argument,  
**Then** completion provider receives prior argument value.

**Steps:**
1. --env depends on --server value
2. Type "--server prod --env "
3. Tab pressed
4. Verify provider receives server=prod in context

### TC-24.2: PropertyType Available in Context
**When** provider needs to know argument type,  
**Then** context.PropertyType is available.

**Steps:**
1. Enum argument type
2. EnumProvider checks context
3. Verify context.PropertyType is the enum type

### TC-24.3: PropertyType Null for Command Completion
**When** completing command names (not argument values),  
**Then** context.PropertyType is null.

**Steps:**
1. Tab at empty prompt (command completion)
2. Provider checks context
3. Verify context.PropertyType is null

### TC-24.4: CompletionContext Includes Partial Value
**When** user has typed partial value,  
**Then** context.PartialValue contains it.

**Steps:**
1. Type "--color gr"
2. Tab pressed
3. Verify context.PartialValue is "gr"

### TC-24.5: Cursor Position Context
**When** cursor is in middle of command line,  
**Then** completion considers only context at cursor.

**Steps:**
1. Type "command arg1 arg2"
2. Move cursor to after "command"
3. Tab pressed
4. Verify completion based on "command" context only

### TC-24.6: Already-Entered Values in Context for IsRest
**When** IsRest positional has some values entered,  
**Then** context includes those values.

**Steps:**
1. Command has Files with IsRest=true
2. Type "delete file1.txt file2.txt "
3. Tab pressed
4. Verify context includes already-entered files

---

## 25. Concurrent & Async Behavior

### TC-25.1: Rapid Tab During Async Fetch
**When** user presses Tab multiple times rapidly during an async fetch,  
**Then** debounce prevents multiple concurrent fetches.

**Steps:**
1. Trigger remote completion (fetch starts)
2. Press Tab 5 times in 100ms
3. Verify only one fetch occurs after debounce
4. Verify no race conditions or duplicate results

### TC-25.2: Simultaneous Ghost and Menu Fetch
**When** ghost text fetch and Tab fetch would overlap,  
**Then** they are coordinated without conflicts.

**Steps:**
1. Type partial command rapidly
2. Press Tab immediately after last keystroke
3. Verify ghost state is consistent
4. Verify menu shows correct completions

### TC-25.3: Provider Returns Results After Menu Closed
**When** async provider returns results after user pressed Escape,  
**Then** results are discarded, no menu shown.

**Steps:**
1. Press Tab (slow provider starts fetch)
2. Press Escape before fetch completes
3. Verify fetch result is discarded
4. Verify no late-appearing menu

### TC-25.4: Multiple Sequential Completions
**When** user completes one argument then immediately Tabs for next,  
**Then** context is updated correctly for new completion.

**Steps:**
1. Tab on --server, accept "localhost"
2. Type space, Tab immediately for --port
3. Verify --server excluded from --port completions
4. Verify --port shows correct values

### TC-25.5: State Consistency After Rapid Operations
**When** user performs rapid Tab→type→Backspace→Tab sequence,  
**Then** menu and ghost states remain consistent.

**Steps:**
1. Tab (menu opens)
2. Type "c" (filter)
3. Backspace (expand filter)
4. Tab (should cycle)
5. Verify state is consistent at each step

---

## 26. Quoting & Escaping Behavior

### TC-26.1: Single Quotes Preserve Literal Text
**When** user types text in single quotes,  
**Then** no escaping or special character handling occurs.

**Steps:**
1. Type `--value 'hello world'`
2. Verify single-quoted string kept as-is

### TC-26.2: Double Quotes Allow Space in Values
**When** user types value with spaces in double quotes,  
**Then** entire quoted value is treated as one argument.

**Steps:**
1. Type `--file "my file.txt"`
2. Verify file path treated as single value

### TC-26.3: Escape Character Before Quote
**When** backslash precedes a quote,  
**Then** quote is treated literally.

**Steps:**
1. Type `--name \"literal\"`
2. Verify quotes are part of value

### TC-26.4: Tab Inside Unclosed Quote
**When** Tab pressed while in an unclosed quote,  
**Then** completion treats partial text as value prefix.

**Steps:**
1. Type `--file "my par`
2. Press Tab
3. Verify completions match "my par" prefix
4. Verify menu stays open (quote context preserved)

### TC-26.5: Accept Completion Adds Closing Quote
**When** accepting completion started inside quotes,  
**Then** closing quote is added if needed.

**Steps:**
1. Type `--file "my par`
2. Tab, select "my partial file.txt"
3. Accept
4. Verify buffer has closing quote

### TC-26.6: Nested Quotes in Completion Value
**When** completion value contains quotes,  
**Then** inserted text is properly escaped.

**Steps:**
1. Completion value is `he said "hello"`
2. Accept from menu
3. Verify proper escaping in buffer

### TC-26.7: Mixed Quote Styles
**When** input mixes single and double quotes,  
**Then** each quote context is tracked independently.

**Steps:**
1. Type `--arg1 "value1" --arg2 'value2'`
2. Verify both values parsed correctly

### TC-26.8: Backslash Escaping in Paths
**When** file path contains backslashes (Windows),  
**Then** path is handled correctly.

**Steps:**
1. Type `--file C:\Users\test\`
2. Tab shows subdirectories
3. Verify path separators preserved

---

## 27. Accessibility & Screen Reader Behavior

### TC-27.1: Menu Items Have Announced Text
**When** screen reader is active and menu is navigated,  
**Then** each item's text and description are announced.

**Steps:**
1. Enable screen reader mode
2. Open menu, navigate items
3. Verify item text and description announced

### TC-27.2: Match Count Announced
**When** menu filtering changes match count,  
**Then** new count is announced.

**Steps:**
1. Open menu with 10 items
2. Filter to 3 items
3. Verify "3 of 10" or similar announced

### TC-27.3: Ghost Text Not Read as Input
**When** ghost text is displayed,  
**Then** it is not read as if user typed it.

**Steps:**
1. Type "ser" (ghost shows "ver")
2. Verify screen reader only reads "ser"

### TC-27.4: Error States Announced
**When** "(no matches)" or "(offline)" state occurs,  
**Then** state is announced to screen reader.

**Steps:**
1. Type non-matching text, Tab
2. Verify "(no matches)" announced

---

## 28. Multi-Command & Pipeline Completion

### TC-28.1: Completion After Pipe Character
**When** user types pipe character followed by partial command,  
**Then** new command completion context starts.

**Steps:**
1. Type `list | gre`
2. Tab pressed
3. Verify "grep" command completions shown
4. Verify context is for new command, not argument

### TC-28.2: Semicolon Starts New Command Context
**When** user types semicolon followed by partial command,  
**Then** completion context resets for new command.

**Steps:**
1. Type `cd /home; ls -l`
2. Tab after "ls -l"
3. Verify completion context is for ls command

### TC-28.3: Subshell Context Tracked
**When** user types command substitution,  
**Then** inner command context is used for completion.

**Steps:**
1. Type `echo $(get`
2. Tab pressed
3. Verify commands starting with "get" are shown

### TC-28.4: Redirect Target File Completion
**When** user types > or >> redirect,  
**Then** file path completion activates.

**Steps:**
1. Type `command > my`
2. Tab pressed
3. Verify file completions for "my*" shown

---

## 29. Fuzzy & Advanced Matching

### TC-29.1: Typo Tolerance in Command Names
**When** user types command with single character typo,  
**Then** close matches are still offered.

**Steps:**
1. Type "servre" (typo of "server")
2. Tab pressed
3. Verify "server" appears in suggestions (if fuzzy enabled)

### TC-29.2: Abbreviation Expansion
**When** user types common abbreviation,  
**Then** expansion is offered.

**Steps:**
1. Configure abbreviation "sv" → "server connect"
2. Type "sv"
3. Verify ghost shows expansion

### TC-29.3: Camel Case Matching
**When** user types uppercase letters matching camelCase,  
**Then** matches are found.

**Steps:**
1. Command "ServerConnection" exists
2. Type "SC"
3. Verify "ServerConnection" matches

### TC-29.4: Path-Style Partial Match
**When** user types path segments with wildcards,  
**Then** matching paths are found.

**Steps:**
1. Paths: "src/components/Button.tsx", "src/containers/Home.tsx"
2. Type "src/*/B"
3. Verify "src/components/Button.tsx" matches

---

## 30. State Persistence & Recovery

### TC-30.1: Menu State After Terminal Focus Loss
**When** terminal loses focus while menu is open,  
**Then** menu remains visible when focus returns.

**Steps:**
1. Open menu
2. Switch to another window
3. Switch back
4. Verify menu still visible and navigable

### TC-30.2: History Persists Across Sessions
**When** application restarts,  
**Then** command history is preserved.

**Steps:**
1. Execute several commands
2. Restart application
3. Navigate history with Up arrow
4. Verify previous commands available

### TC-30.3: Cache Persists Appropriately
**When** session includes completion cache,  
**Then** cache follows configured TTL.

**Steps:**
1. Tab on argument (cache populated)
2. Wait TTL period
3. Tab again
4. Verify fresh fetch occurs

### TC-30.4: Undo Completion Acceptance
**When** user accepts completion then presses Ctrl+Z,  
**Then** buffer reverts to pre-completion state.

**Steps:**
1. Type "ser", Tab (completes to "server")
2. Press Ctrl+Z
3. Verify buffer reverts to "ser"

### TC-30.5: Menu Position After Scroll
**When** terminal is scrolled while menu is open,  
**Then** menu remains in correct position.

**Steps:**
1. Open menu
2. Scroll terminal (if supported)
3. Verify menu position is correct

---

## 31. Completion Source Interactions

### TC-31.1: Multiple Providers Same Priority
**When** two providers have same priority and both can handle,  
**Then** first registered provider wins.

**Steps:**
1. Register Provider A and Provider B with same priority
2. Tab on argument both can handle
3. Verify consistent provider wins

### TC-31.2: Provider Priority Ordering
**When** multiple providers can handle with different priorities,  
**Then** highest priority provider handles first.

**Steps:**
1. Remote provider (priority 200), Local provider (priority 100)
2. Tab on connected remote argument
3. Verify remote provider handles

### TC-31.3: Fallback to Next Provider on Empty
**When** primary provider returns empty,  
**Then** system falls back to positional → arguments.

**Steps:**
1. Positional provider returns empty for position 0
2. Tab pressed
3. Verify argument names shown as fallback

### TC-31.4: Provider Short-Circuits on First Result
**When** high priority provider returns results,  
**Then** lower priority providers are not called.

**Steps:**
1. History provider returns results
2. Tab pressed
3. Verify command provider not invoked

### TC-31.5: All Providers Fail Gracefully
**When** every registered provider throws exception,  
**Then** "(no matches)" shown, no crash.

**Steps:**
1. Configure providers to all throw
2. Tab pressed
3. Verify graceful failure message
4. Verify errors logged

---

## 32. VirtualConsole Integration Testing

### TC-32.1: Menu Renders Correctly in VirtualConsole
**When** menu is opened in VirtualConsole test environment,  
**Then** ANSI sequences produce expected screen state.

**Steps:**
1. Create VirtualConsole (80x24)
2. Trigger menu display
3. Assert screen buffer contains menu items
4. Assert selection highlighting visible

### TC-32.2: Ghost Text Color in VirtualConsole
**When** ghost text renders in VirtualConsole,  
**Then** cells have correct dim/gray style.

**Steps:**
1. Type partial command triggering ghost
2. Query ghost text cells
3. Assert style is dim gray

### TC-32.3: Cursor Position Tracking
**When** user types and menu updates,  
**Then** cursor position is tracked accurately.

**Steps:**
1. Simulate keystrokes
2. Assert cursor position matches expected
3. Assert cursor visible after menu close

### TC-32.4: Screen Buffer Clear After Menu Close
**When** menu closes,  
**Then** menu area is properly cleared.

**Steps:**
1. Open menu (takes 10 lines)
2. Close menu
3. Assert those 10 lines are cleared
4. Assert no visual artifacts

### TC-32.5: Line Wrapping with Long Completions
**When** completion text exceeds terminal width,  
**Then** wrapping is handled correctly.

**Steps:**
1. Terminal width 40 columns
2. Completion value is 50 characters
3. Assert truncation or wrap is correct

### TC-32.6: Assert Keystroke Sequences
**When** complex keystroke sequences are simulated,  
**Then** each intermediate state is verifiable.

**Steps:**
1. Record keystrokes: Tab, Down, Down, Enter
2. Capture screen state after each
3. Assert expected progression

---

## 33. Configuration & Settings

### TC-33.1: Disable Ghost Text via Configuration
**When** ghost text is disabled in settings,  
**Then** no ghost text appears.

**Steps:**
1. Set GhostTextEnabled = false
2. Type partial command
3. Verify no ghost text shown

### TC-33.2: Configure Menu Size
**When** menu page size is configured to 5,  
**Then** menu shows 5 items with scroll.

**Steps:**
1. Set MenuPageSize = 5
2. Open menu with 10 items
3. Verify 5 items visible
4. Verify scroll indicators

### TC-33.3: Configure Debounce Delay
**When** debounce delay is configured to 200ms,  
**Then** fetches use that delay.

**Steps:**
1. Set DebounceDelayMs = 200
2. Type rapidly
3. Verify fetch occurs after 200ms idle

### TC-33.4: Case Sensitivity Configuration
**When** case sensitivity is enabled,  
**Then** matching is case-sensitive.

**Steps:**
1. Set CaseSensitive = true
2. Type "Help" to match "help"
3. Verify no match found

### TC-33.5: History Limit Configuration
**When** history limit is set to 100,  
**Then** only 100 entries are kept.

**Steps:**
1. Set HistoryLimit = 100
2. Execute 150 commands
3. Verify only last 100 in history

---

## 34. Error Messages & User Feedback

### TC-34.1: Timeout Error Message Content
**When** remote completion times out,  
**Then** message indicates timeout and retry option.

**Steps:**
1. Remote server is slow (>3s)
2. Tab pressed
3. Verify message: "Request timed out - press Tab to retry"

### TC-34.2: Offline Error Message Content
**When** not connected to remote server,  
**Then** "(offline)" indicator is shown.

**Steps:**
1. Disconnect from server
2. Tab on remote argument
3. Verify "(offline)" indicator visible

### TC-34.3: Provider Error Logged But Not Shown
**When** provider throws exception,  
**Then** error is logged, user sees graceful message.

**Steps:**
1. Provider throws InvalidOperationException
2. Tab pressed
3. Verify "(no matches)" shown to user
4. Verify exception logged

### TC-34.4: Missing Method Error at Startup
**When** [Completion("NonExistent")] references missing method,  
**Then** error occurs at registration, not runtime.

**Steps:**
1. Command with invalid completion method
2. Register command
3. Verify error during registration
4. Verify clear error message

### TC-34.5: Type Mismatch Warning
**When** [FilePathCompletion] on int property,  
**Then** warning at registration or clear error at conversion.

**Steps:**
1. [FilePathCompletion] on int argument
2. User selects file path
3. Verify conversion error is clear

---

## 35. Boundary Value Testing

### TC-35.1: Empty Command Registry
**When** no commands are registered,  
**Then** Tab at empty prompt shows "(no matches)".

**Steps:**
1. Initialize with no registered commands
2. Tab pressed
3. Verify "(no matches)" shown

### TC-35.2: Single Character Command
**When** command name is single character "x",  
**Then** completion works correctly.

**Steps:**
1. Register command "x"
2. Type "x"
3. Verify completion/execution works

### TC-35.3: Very Long Command Name
**When** command name is 200 characters,  
**Then** display truncates appropriately.

**Steps:**
1. Register command with 200 char name
2. Tab pressed
3. Verify truncation with ellipsis

### TC-35.4: Maximum Argument Count
**When** command has 50 arguments,  
**Then** all are available in completion.

**Steps:**
1. Command with 50 arguments
2. Tab on "--"
3. Verify all 50 in menu (with scroll)

### TC-35.5: Zero-Width Characters in Values
**When** completion value contains zero-width characters,  
**Then** display and insertion work correctly.

**Steps:**
1. Value contains zero-width joiner
2. Tab shows value
3. Accept works correctly

### TC-35.6: Maximum Buffer Length
**When** input buffer reaches maximum length,  
**Then** completion still functions.

**Steps:**
1. Type 1000 characters
2. Press Tab
3. Verify completion works or graceful limit

---

## Summary Statistics

| Category | Test Case Count |
|----------|-----------------|
| Ghost Text Behavior | 16 |
| Menu Display & Navigation | 18 |
| Menu Filtering | 15 |
| Input Editing | 10 |
| Command & Group Completion | 4 |
| Argument Name & Alias Completion | 10 |
| Argument Value Completion | 10 |
| Positional Argument Completion | 11 |
| File Path Completion | 12 |
| Viewport Scrolling | 5 |
| Ghost & Menu Interaction | 3 |
| Multi-Step Workflows | 4 |
| History Navigation | 4 |
| Edge Cases & Error Handling | 27 |
| Visual Rendering | 5 |
| Submission Behavior | 3 |
| Remote Completion | 12 |
| Caching Behavior | 7 |
| Provider & Attribute Configuration | 20 |
| Match Ranking & Ordering | 5 |
| Result Limiting & Truncation | 5 |
| Terminal & Environment Edge Cases | 6 |
| Keyboard Variations | 8 |
| Context Sensitivity | 6 |
| Concurrent & Async Behavior | 5 |
| Quoting & Escaping Behavior | 8 |
| Accessibility & Screen Reader Behavior | 4 |
| Multi-Command & Pipeline Completion | 4 |
| Fuzzy & Advanced Matching | 4 |
| State Persistence & Recovery | 5 |
| Completion Source Interactions | 5 |
| VirtualConsole Integration Testing | 6 |
| Configuration & Settings | 5 |
| Error Messages & User Feedback | 5 |
| Boundary Value Testing | 6 |
| **Total** | **283** |

---

## Notes

- Test cases are derived from the existing Visual/UX test suite, specification documents, and industry CLI best practices (Fish shell, Zsh, PowerShell, VS Code, Bash)
- Each test case should be verifiable through the VirtualConsole infrastructure for true functional/UX validation
- Test case IDs (TC-X.Y) can be referenced in test implementations for traceability
- Original sections (1-24) cover core functionality documented in spec documents (005-autocomplete-redesign, 010-menu-filter, 004-positional-arguments)
- New sections (25-35) added based on:
  - **Concurrent & Async (25)**: Race conditions, debounce behavior, and state consistency during async operations
  - **Quoting & Escaping (26)**: Quote handling patterns from Bash/Zsh shells
  - **Accessibility (27)**: Screen reader compatibility per WCAG guidelines
  - **Multi-Command & Pipeline (28)**: Complex command line patterns from Unix shells
  - **Fuzzy Matching (29)**: Advanced matching patterns from Fish shell and fzf
  - **State Persistence (30)**: Session management and undo capabilities
  - **Provider Interactions (31)**: Provider priority and fallback logic
  - **VirtualConsole Testing (32)**: Specific tests for the VirtualConsole testing infrastructure
  - **Configuration (33)**: User-configurable behavior settings
  - **Error Feedback (34)**: User-facing error messages and logging
  - **Boundary Values (35)**: Edge case inputs at system limits
- Edge cases significantly outnumber happy path scenarios (~3:1 ratio), reflecting real-world CLI usage
- VirtualConsole project enables full end-to-end UX testing without manual interaction
- Test categories align with ANSI terminal capabilities and cross-platform considerations

### Test Priority Recommendations

| Priority | Categories | Rationale |
|----------|------------|-----------|
| P0 (Critical) | 1, 2, 5, 6, 7, 8 | Core completion flows users depend on daily |
| P1 (High) | 3, 4, 9, 14, 17 | Important functionality and common edge cases |
| P2 (Medium) | 10, 11, 12, 13, 15, 16, 18, 19, 20, 24, 25, 26 | Enhancement features and async behavior |
| P3 (Low) | 21, 22, 23, 27, 28, 29, 30, 31, 33, 34, 35 | Specialized scenarios and configuration |
| P4 (Future) | 32 | VirtualConsole infrastructure tests (when available) |

### VirtualConsole Integration Readiness

The following test categories are particularly suited for VirtualConsole-based testing:

1. **Visual Rendering (15)** - Screen buffer assertions for menu and ghost text display
2. **Viewport Scrolling (10)** - Scroll indicator position and visibility
3. **Ghost & Menu Interaction (11)** - Coordinated visual state changes
4. **Edge Cases (14)** - Rapid input and state consistency verification
5. **VirtualConsole Testing (32)** - Direct infrastructure validation
