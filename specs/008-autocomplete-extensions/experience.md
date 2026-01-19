# Autocomplete User Experience Specification

**Purpose**: Define the visual and interaction behavior for the autocomplete system  
**Created**: January 17, 2026  
**Related**: [spec.md](spec.md) (architecture specification)

---

## Overview

The autocomplete system uses a **two-stage model** similar to modern CLIs (PowerShell, fish, VS Code terminal):

1. **Ghost Text**: When the cursor enters an autocomplete-applicable position, the first available suggestion appears automatically as dimmed ghost text.

2. **Selection Menu**: When Tab is pressed and multiple options are available, a dropdown menu appears allowing the user to select from all available options.

This provides immediate feedback while typing (ghost text) with full discoverability when exploring options (menu).

---

## Visual States

### 1. Normal Input (No Autocomplete Available)

Standard input with blinking cursor. No suggestions available for current position:

```
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --target C:\|                                    │
└─────────────────────────────────────────────────────────────────┘
                              ↑ cursor (blinking)
```

### 2. Ghost Text (Auto-Appearing)

When cursor enters an autocomplete-applicable position, ghost text appears **automatically** (no keypress required):

```
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|arget                                        │
└─────────────────────────────────────────────────────────────────┘
                    └──┬──┘
                   ghost text (dim grey)
```

The ghost text shows the **first** available suggestion. It updates dynamically as the user types:

```
Typing progression:

> myapp backup --|target            (first match: --target)
> myapp backup --t|arget            (still --target)
> myapp backup --ta|rget            (still --target)
> myapp backup --ti|meout           (now --timeout matches first)
```

### 3. Selection Menu (Multiple Options)

When Tab is pressed and **multiple options** are available, the ghost text clears and a dropdown menu appears:

```
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
│                   ┌────────────────┐                            │
│                   │ ▸ --target     │ ← selected (highlighted)   │
│                   │   --temp       │                            │
│                   │   --timeout    │                            │
│                   └────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
```

Menu characteristics:
- Appears directly below the cursor position
- First item is pre-selected (highlighted)
- Arrow indicator (▸) shows current selection
- Selected item uses inverted colors (black on silver/white)

### 4. Menu Navigation

Up/Down arrows navigate within the menu:

```
Initial state:           After ↓ Down:            After ↓ Down:
┌────────────────┐       ┌────────────────┐       ┌────────────────┐
│ ▸ --target     │       │   --target     │       │   --target     │
│   --temp       │       │ ▸ --temp       │       │   --temp       │
│   --timeout    │       │   --timeout    │       │ ▸ --timeout    │
└────────────────┘       └────────────────┘       └────────────────┘
```

Menu wraps around: Down on last item goes to first, Up on first goes to last.

### 4a. Menu Scrolling (Many Options)

When more options exist than fit in the visible menu, scroll indicators appear showing the count of hidden items:

**Scrolled to top (more items below):**
```
┌────────────────────┐
│ ▸ --target        │ ← selected
│   --temp          │
│   --threads       │
│   --timeout       │
│   ▼ 3 more...     │ ← dim, shows count
└────────────────────┘
```

**Scrolled to middle (items above and below):**
```
┌────────────────────┐
│   ▲ 2 more...     │ ← dim, shows count
│   --temp          │
│ ▸ --threads       │ ← selected
│   --timeout       │
│   ▼ 1 more...     │ ← dim, shows count
└────────────────────┘
```

**Scrolled to bottom (more items above):**
```
┌────────────────────┐
│   ▲ 3 more...     │ ← dim, shows count
│   --trace         │
│   --truncate      │
│   --type          │
│ ▸ --typeName      │ ← selected
└────────────────────┘
```

Scroll indicators use dim styling and format: `▲ N more...` or `▼ N more...` where N is the count of hidden items.

**Wrap-around behavior:**

Navigation wraps around even in scrolling mode:
- Down on last item → jumps to first item (menu scrolls to top)
- Up on first item → jumps to last item (menu scrolls to bottom)

```
At bottom, press ↓ Down:         Wraps to top:
┌────────────────────┐           ┌────────────────────┐
│   ▲ 6 more...     │           │ ▸ --target        │ ← now selected
│   --type          │    →      │   --temp          │
│ ▸ --typeName      │           │   --threads       │
└────────────────────┘           │   ▼ 4 more...    │
                                 └────────────────────┘
```

### 4b. Type-to-Filter in Menu

When the menu is open, typing filters the visible options in real-time:

```
Step 1: Menu open with many options
┌────────────────────┐
│ ▸ --target        │
│   --temp          │
│   --timeout       │
│   --threads       │
│   ▼ more...       │
└────────────────────┘

Step 2: User types 'i' → menu filters to items containing 'i'
> myapp backup --ti|             ← 'i' added to input
                   ┌────────────────────┐
                   │ ▸ --timeout       │ ← filtered list
                   │   --stdin         │
                   └────────────────────┘

Step 3: User types 'm' → menu filters further
> myapp backup --tim|            ← 'im' now in input
                    ┌────────────────────┐
                    │ ▸ --timeout       │ ← only match
                    └────────────────────┘
```

**Filter behavior:**
- Characters are appended to the current input
- Menu filters to show only matching options
- Selection resets to first matching item
- If no matches remain, menu closes and ghost text clears
- Backspace removes last character and re-filters

### 5. Single Option (Direct Completion)

When Tab is pressed and only **one option** is available, the ghost text is accepted immediately (no menu):

```
Before Tab:                          After Tab:
┌────────────────────────────┐       ┌────────────────────────────┐
│ > myapp bac|kup            │  →    │ > myapp backup|            │
└────────────────────────────┘       └────────────────────────────┘
              ghost text                          cursor at end
```

---

## Keyboard Interactions

### Ghost Text State (Auto-Appearing)

| Key | Action | Result |
|-----|--------|--------|
| **Tab** | Accept / Open menu | Single option: accept ghost text. Multiple: open menu |
| **Right Arrow** | Accept ghost text | Accept and move cursor to end |
| **Escape** | Dismiss ghost text | Clear ghost text, stay at cursor |
| **Any character** | Continue typing | Ghost text updates to new first match |
| **Enter** | Submit command | Ghost text ignored, command submitted as-is |

### Menu State (After Tab with Multiple Options)

| Key | Action | Result |
|-----|--------|--------|
| **↓ Down** | Next item | Move selection down (wraps to top) |
| **↑ Up** | Previous item | Move selection up (wraps to bottom) |
| **Tab** | Next item | Same as Down arrow |
| **Shift+Tab** | Previous item | Same as Up arrow |
| **Enter** | Accept selection | Insert selected option, close menu |
| **Escape** | Cancel | Close menu, restore original text |
| **Alphanumeric** | Type-to-filter | Character added to input, menu filters in real-time |
| **Backspace** | Remove and filter | Remove last character, re-filter menu |
| **Space** | Accept and continue | Accept current selection, insert space, close menu |

### Navigation Keys (No Autocomplete)

| Key | Action |
|-----|--------|
| **Left Arrow** | Move cursor left |
| **Right Arrow** | Move cursor right |
| **Up Arrow** | Command history (previous) |
| **Down Arrow** | Command history (next) |
| **Backspace** | Delete character before cursor |
| **Delete** | Delete character after cursor |

---

## Interaction Flows

### Flow 1: Quick Completion with Ghost Text

User wants to type `backup` command - only one match:

```
Step 1: User types, ghost text appears automatically
┌─────────────────────────────────────────────────────────────────┐
│ > myapp ba|ckup                                                 │
└─────────────────────────────────────────────────────────────────┘
            └─┬─┘ ghost text (auto)

Step 2: User presses Tab → single option, completes immediately
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup|                                                 │
└─────────────────────────────────────────────────────────────────┘
                ↑ cursor at end, ready for more input
```

### Flow 2: Exploring Multiple Options with Menu

User wants to see all arguments starting with `--t`:

```
Step 1: User types, ghost text shows first match
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|arget                                        │
└─────────────────────────────────────────────────────────────────┘
                    ghost text (auto)

Step 2: User presses Tab → multiple options, menu opens
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
│                   ┌────────────────┐                            │
│                   │ ▸ --target     │                            │
│                   │   --timeout    │                            │
│                   │   --temp       │                            │
│                   └────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
                    ghost cleared, menu visible

Step 3: User presses ↓ Down → moves to second option
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
│                   ┌────────────────┐                            │
│                   │   --target     │                            │
│                   │ ▸ --timeout    │                            │
│                   │   --temp       │                            │
│                   └────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘

Step 4: User presses Enter → accepts selection
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --timeout|                                       │
└─────────────────────────────────────────────────────────────────┘
                          ↑ cursor at end
```

### Flow 3: Type-Through (Continue Typing)

User sees ghost text but continues typing to narrow down:

```
Step 1: Ghost text appears
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|arget                                        │
└─────────────────────────────────────────────────────────────────┘

Step 2: User types 'i' → ghost text updates
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --ti|meout                                       │
└─────────────────────────────────────────────────────────────────┘
                      ghost updates to new first match

Step 3: User presses Tab → only one match now, completes
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --timeout|                                       │
└─────────────────────────────────────────────────────────────────┘
```

### Flow 4: Cancel with Escape

User dismisses ghost text or menu:

```
Ghost text scenario:
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|arget                                        │
└─────────────────────────────────────────────────────────────────┘
                    ↓ Escape
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
└─────────────────────────────────────────────────────────────────┘
                    ghost text cleared

Menu scenario:
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
│                   ┌────────────────┐                            │
│                   │ ▸ --target     │                            │
│                   └────────────────┘                            │
└─────────────────────────────────────────────────────────────────┘
                    ↓ Escape
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|                                             │
└─────────────────────────────────────────────────────────────────┘
                    menu closed, original text preserved
```

### Flow 5: Right Arrow Accepts Ghost Text

Alternative to Tab for accepting ghost text:

```
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --t|arget                                        │
└─────────────────────────────────────────────────────────────────┘
                    ↓ Right Arrow
┌─────────────────────────────────────────────────────────────────┐
│ > myapp backup --target|                                        │
└─────────────────────────────────────────────────────────────────┘
                         cursor at end (no menu, direct accept)
```

---

## Autocomplete Contexts

### Command/Group Names

When cursor is at command position, ghost text appears automatically:

```
> myapp |backup              ghost text auto-appears (first command)
                             Tab → single match completes, OR multiple opens menu
```

With multiple commands:
```
> myapp |                    cursor enters position
> myapp |backup              ghost text appears (first: backup)
        ↓ Tab (multiple options)
> myapp |
        ┌────────────────┐
        │ ▸ backup       │
        │   config       │
        │   restore      │
        └────────────────┘
```

### Command Aliases

Aliases are included in command suggestions:

```
> myapp b|ackup              ghost text shows full command for alias 'b'
```

### Argument Names (with --)

When cursor follows `--`, ghost text appears:

```
> myapp backup --|target     ghost text auto-appears
                 ↓ Tab (multiple --t options)
> myapp backup --|
                 ┌────────────────┐
                 │ ▸ --target     │
                 │   --temp       │
                 │   --timeout    │
                 └────────────────┘
```

### Argument Aliases (with -)

When cursor follows single `-`:

```
> myapp backup -|t           ghost text auto-appears
                ↓ Tab (multiple options)
> myapp backup -|
                ┌────────────────┐
                │ ▸ -t           │
                │   -v           │
                │   -q           │
                └────────────────┘
```

### Argument Values (Explicit Provider)

When cursor is at argument value position with explicit autocomplete attribute:

```
> myapp backup --format |json     ghost text auto-appears
                        ↓ Tab (multiple formats)
> myapp backup --format |
                        ┌────────────────┐
                        │ ▸ json         │
                        │   xml          │
                        │   csv          │
                        │   yaml         │
                        └────────────────┘
```

### Argument Values (Implicit - Enum Type)

Enum arguments get automatic suggestions:

```
> myapp backup --level |Debug     ghost text (first enum value)
                       ↓ Tab (multiple enum values)
> myapp backup --level |
                       ┌────────────────┐
                       │ ▸ Debug        │
                       │   Info         │
                       │   Warning      │
                       │   Error        │
                       └────────────────┘
```

### Argument Values (Implicit - Boolean Type)

Boolean arguments get true/false (only 2 options):

```
> myapp backup --force |false     ghost text (first: false, alphabetical)
                       ↓ Tab (2 options)
> myapp backup --force |
                       ┌────────────────┐
                       │ ▸ false        │
                       │   true         │
                       └────────────────┘
```

### Positional Parameter Values

Positional parameters (arguments without `--name`) also support autocomplete based on their type. The system tracks which positional parameter the cursor is at based on position.

**Example command signature:**
```
myapp backup <level> <target> [--force]
             ↑       ↑
             pos 0   pos 1
             (enum)  (string)
```

**Autocomplete at first positional (enum type):**

```
Step 1: User types command, cursor at first positional position
> myapp backup |Debug         ghost text (first enum value for pos 0)

Step 2: User types partial filter
> myapp backup W|arning       ghost text updates (W matches Warning)

Step 3: Tab with multiple W-matches (if Warning, Watch, etc.)
> myapp backup W|
                ┌────────────────────────────────────────┐
                │ ▸ Warning  Potential issues            │
                │   Watch    Watch mode enabled          │
                └────────────────────────────────────────┘
```

**Autocomplete at second positional (after first is provided):**

```
> myapp backup Warning |      cursor at second positional position
                              (no implicit provider for string type)
                              no ghost text appears
```

**Multiple positional enums:**

```
Command: myapp convert <inputFormat> <outputFormat>

> myapp convert |json         ghost text for inputFormat (pos 0)
                ↓ Tab, accept
> myapp convert json |xml     ghost text for outputFormat (pos 1)
                     ↓ Tab, accept
> myapp convert json xml|     both positionals completed
```

**Positional with explicit provider:**

If a positional parameter has an explicit autocomplete attribute, that provider is used:

```
Command: myapp open <filename>   // filename has [FileAutocomplete] attribute

> myapp open |readme.md       ghost text from file provider
             ↓ Tab (multiple files)
> myapp open |
             ┌────────────────────┐
             │ ▸ readme.md        │
             │   package.json     │
             │   tsconfig.json    │
             └────────────────────┘
```

### Positional Parameter Rules

Positional parameters have special autocomplete behavior based on how values are provided:

**Rule 1: Positional parameters can also be set by name**

A positional parameter can be specified either positionally or by its argument name:

```
Command: myapp backup <level> [--target] [--force]

# These are equivalent:
> myapp backup Debug --target C:\           # level set positionally
> myapp backup --level Debug --target C:\   # level set by name
```

**Rule 2: Set positional values filter out argument names**

When a positional value is provided, that argument name is excluded from autocomplete:

```
Command: myapp backup <level> [--target] [--force]

> myapp backup Debug --|      level already set positionally
                      ↓ Tab
                      ┌────────────────┐
                      │ ▸ --target     │   ← --level NOT shown
                      │   --force      │
                      └────────────────┘
```

**Rule 3: Satisfied positional position has no autocomplete**

If a positional value is already set (by position or name), no autocomplete at that position:

```
Command: myapp backup <level> [--target]

> myapp backup --level Debug|             level set by name
               ↓ move cursor back
> myapp backup | --level Debug            no ghost text (level is satisfied)
                                          Tab → no action
```

**Rule 4: Unsatisfied positional with later named args**

If a named argument is set but the positional parameter is NOT satisfied by it, autocomplete works:

```
Command: myapp backup <level> [--target]

> myapp backup --target C:\sav.bak|       target set, but NOT level
               ↓ move cursor back
> myapp backup | --target C:\sav.bak      ghost text for level (unsatisfied)
> myapp backup |Debug --target C:\sav.bak
```

**Rule 5: Named arguments end positional parsing**

Once any named argument (`--name`) appears, the autocomplete system assumes all positional parameters before it are consumed. No positional values can appear after named arguments:

```
Command: myapp backup <level> <path> [--force]

# Valid - positionals before named:
> myapp backup Debug C:\path --force

# After first named arg, only named args available:
> myapp backup --force |                  cursor after --force
                                          no positional autocomplete
                                          only --level, --path available as named
> myapp backup --force --|                
                       ┌────────────────┐
                       │ ▸ --level      │   ← unsatisfied positionals as named
                       │   --path       │
                       └────────────────┘
```

**Summary table:**

| Scenario | Autocomplete Behavior |
|----------|----------------------|
| Positional position, value not set | Show provider suggestions |
| Positional position, value set (positionally) | No autocomplete |
| Positional position, value set (by name) | No autocomplete |
| `--` position, positional already set | Exclude that arg name |
| After first `--arg`, at space | Only named args available |
| Unsatisfied positional as named arg | Include in `--` suggestions |

---

## Edge Cases

### No Matches Available

When no suggestions match, no ghost text appears and Tab does nothing:

```
> myapp backup --xyz|        no ghost text (no matches)
                             Tab → no action (nothing to complete)
                             cursor remains unchanged
```

No audible or visual feedback for "no matches" - the absence of ghost text indicates no suggestions are available.

### Single Match

When only one suggestion exists, Tab completes immediately (no menu):

```
> myapp backup --targ|et     ghost text (only match: --target)
                     ↓ Tab
> myapp backup --target|     completed, cursor at end
```

### Empty Query

At an autocomplete position with no typed filter, ghost text shows first available option. The context determines what options are available:

**At named argument position (after command):**
```
> myapp backup |--force      ghost text (first argument name, alphabetical)
               ↓ Tab (multiple arguments available)
> myapp backup |
               ┌────────────────┐
               │ ▸ --force      │
               │   --target     │
               │   --timeout    │
               └────────────────┘
```

**At positional parameter position (enum type):**
```
Command: myapp backup <level> [--target]

> myapp backup |Debug        ghost text (first enum value for positional)
               ↓ Tab (multiple enum values)
> myapp backup |
               ┌────────────────┐
               │ ▸ Debug        │
               │   Info         │
               │   Warning      │
               │   Error        │
               └────────────────┘
```

**At positional with no implicit provider (e.g., string type):**
```
Command: myapp backup <path>   // no autocomplete attribute, string type

> myapp backup |              no ghost text (no provider for string type)
                              Tab → no action
```

**Mixed positional and named arguments:**
```
Command: myapp backup <level> [--target] [--force]

> myapp backup |Debug        ghost text for positional (level)
> myapp backup Debug |       positional filled, now at argument position
                              if next positional exists and has provider → show it
                              otherwise → show named arguments
> myapp backup Debug --|target    ghost text for named argument
```

### Mid-Line Editing

Autocomplete works at cursor position, not just end of line:

```
> myapp backup | --quiet     cursor mid-line
> myapp backup |--target --quiet
               ghost text inserted at cursor
               ↓ Tab
> myapp backup |
               ┌────────────────┐
               │ ▸ --target     │
               │   --timeout    │
               └────────────────┘ --quiet   (menu appears, rest of line preserved)
```

### Menu Scrolling (Many Options)

When more options exist than can fit in the menu, scrolling is enabled:

```
> myapp |
        ┌────────────────┐
        │ ▸ backup       │
        │   config       │
        │   restore      │
        │   status       │
        │   ▼ 4 more...  │ ← dim, shows count (all alphabetical)
        └────────────────┘
```

Navigation continues past visible items, scrolling the list.

### History Navigation Dismisses Ghost Text

Up/Down arrows dismiss ghost text and access history:

```
> myapp |backup              ghost text visible
        ↓ Up Arrow
> previous-command|          history shown, ghost text dismissed
```

### Values with Spaces (Auto-Quoting)

When a completion value contains spaces, the system automatically wraps it in double quotes:

**Provider returns:** `My Documents`  
**Inserted value:** `"My Documents"`

```
Step 1: User at value position
> myapp backup --target |

Step 2: Ghost text appears (auto-quoted)
> myapp backup --target |"My Documents"
                        ghost text includes quotes

Step 3: Tab or Enter accepts
> myapp backup --target "My Documents"|
                        quotes preserved
```

**Menu display vs. insertion:**
```
┌──────────────────────┐
│ ▸ My Documents       │  ← displayed without quotes
│   Program Files      │
│   Users              │
└──────────────────────┘

Selected → inserts: "My Documents"
```

**Behavior rules:**
- Provider returns raw values (no quotes)
- Autocomplete engine adds quotes if value contains spaces
- Display in menu shows unquoted value for readability
- Ghost text and inserted value include quotes
- Values without spaces are inserted without quotes

**Already in quotes context:**

If user has already typed an opening quote, completion continues within the quote:

```
> myapp backup --target "|           user typed opening quote
> myapp backup --target "|My Documents"
                         ghost text completes the value and closing quote
```

### Filter Removes All Matches

When typing in menu filters out all options:

```
Step 1: Menu open
> myapp backup --|
               ┌────────────────┐
               │ ▸ --target     │
               │   --timeout    │
               └────────────────┘

Step 2: User types 'xyz' (no matches)
> myapp backup --xyz|
               (menu closes, no ghost text - no matches)

Step 3: User presses Backspace to remove 'z'
> myapp backup --xy|
               (still no matches, no ghost text)

Step 4: User presses Backspace repeatedly to get back to 't'
> myapp backup --t|arget
               ghost text reappears (matches restored)
```

**Backspace restores matching:** When the user presses Backspace and the resulting text has matches, ghost text reappears with the first matching option.

### Command Alias Behavior

Command aliases are separate suggestions in the autocomplete list. An alias can be any string and does not need to share characters with the command name.

**Example command:**
```csharp
[Command(Alias = "up")]
public class BackupCommand : CommandBase { }
```

**Autocomplete behavior:**

```
Step 1: User types 'u', alias 'up' matches
> myapp u|p              ghost text shows 'up' (the alias)
         ↓ Tab
> myapp up|              alias completed

Step 2: Alternatively, user types 'b', command name matches
> myapp b|ackup          ghost text shows 'backup' (the command)
         ↓ Tab  
> myapp backup|          command name completed
```

**Both appear in menu:**
```
> myapp |
        ┌────────────────────────────────────┐
        │ ▸ backup       Backup your data    │
        │   up           Alias for backup    │
        │   config       Configuration...    │
        └────────────────────────────────────┘
```

Note: The alias `up` and command `backup` are separate entries. Typing matches against both independently. Menu shows description indicating it's an alias.

---

## Visual Styling

### Ghost Text Appearance

| Element | Color | Style |
|---------|-------|-------|
| Typed text | Default (white/light gray) | Normal |
| Ghost text | Dark gray | Dim attribute |
| Cursor | Default | Blinking, at end of typed text |

### Menu Appearance

| Element | Color | Style |
|---------|-------|-------|
| Menu border | Default | Box-drawing characters |
| Unselected items | Default | Normal |
| Selected item | Black on Silver | Inverted, with ▸ indicator |
| Scroll indicator | Dim | Format: `▲ N more...` or `▼ N more...` |

### Mockup Legend

```
Typed text:   ▓▓▓▓▓ (bright/white)
Ghost text:   ░░░░░ (dim/grey)
|             cursor position
▸             selection indicator
```

---

## Timing Considerations

### Ghost Text is Automatic

- Ghost text appears **automatically** when cursor is in an autocomplete-applicable position
- No keypress required for ghost text to appear
- Ghost text updates dynamically as user continues typing
- Menu navigation should feel **instantaneous** to the user

---

## Acceptance Criteria

These scenarios define the expected behavior for testing:

| ID | Scenario | Expected Behavior |
|----|----------|-------------------|
| EX-001 | Cursor enters autocomplete position | Ghost text appears automatically with first match |
| EX-002 | Tab with single option | Ghost text accepted, cursor moves to end |
| EX-003 | Tab with multiple options | Ghost text clears, menu opens with first item selected |
| EX-004 | Right Arrow on ghost text | Ghost text accepted (same as Tab single) |
| EX-005 | Down arrow in menu | Selection moves to next item (wraps) |
| EX-006 | Up arrow in menu | Selection moves to previous item (wraps) |
| EX-007 | Enter in menu | Selected option inserted, menu closes |
| EX-008 | Escape on ghost text | Ghost text clears, cursor stays |
| EX-009 | Escape on menu | Menu closes, original text preserved |
| EX-010 | Type character with ghost text | Character inserted, ghost text updates to new first match |
| EX-011 | Type character with menu open | Character added to input, menu filters in real-time |
| EX-012 | Up arrow with ghost text | Ghost text dismissed, command history shown |
| EX-013 | No matching suggestions | No ghost text, Tab does nothing |
| EX-014 | Enum argument position | Enum values appear as options (implicit provider) |
| EX-015 | Boolean argument position | true/false appear as options (implicit provider) |
| EX-016 | Explicit attribute present | Attribute provider used instead of implicit |
| EX-017 | Positional enum parameter | Ghost text shows first enum value at correct position |
| EX-018 | Multiple positional parameters | Each position tracks independently, correct provider used |
| EX-019 | Positional with no provider | No ghost text at that position, Tab does nothing |
| EX-020 | Many options (scroll down) | Menu shows ▼ N more... indicator at bottom |
| EX-021 | Scrolled menu (middle) | Menu shows ▲ N more... and ▼ N more... indicators |
| EX-022 | Scrolled menu (bottom) | Menu shows ▲ N more... indicator at top |
| EX-023 | Scrolled menu wrap (bottom→top) | Down on last item jumps to first, scrolls to top |
| EX-024 | Scrolled menu wrap (top→bottom) | Up on first item jumps to last, scrolls to bottom |
| EX-025 | Backspace in menu | Last character removed, menu re-filters |
| EX-026 | Space in menu | Current selection accepted, space inserted, menu closes |
| EX-027 | Filter removes all matches | Menu closes, no ghost text |
| EX-028 | Backspace restores matches | Ghost text reappears when filter matches again |
| EX-029 | Value with spaces | Inserted value auto-wrapped in double quotes |
| EX-030 | Value without spaces | Inserted without quotes |
| EX-031 | Already in quotes context | Completion continues within existing quotes |
| EX-032 | Positional set positionally | Arg name excluded from -- autocomplete |
| EX-033 | Positional set by name | No autocomplete at that positional position |
| EX-034 | Named arg set, positional unsatisfied | Positional autocomplete still works |
| EX-035 | After first named arg | Only named args available, no positional |
| EX-036 | Unsatisfied positional after named | Appears in -- suggestions as named arg |
| EX-037 | Command alias match | Alias appears as separate suggestion with "Alias for X" |
| EX-038 | Alias different from command name | Typing alias prefix shows alias, not command |

---

## Document History

| Date | Author | Change |
|------|--------|--------|
| 2026-01-17 | - | Initial creation based on existing implementation patterns |
| 2026-01-17 | - | Updated to two-stage model: auto ghost text + Tab opens menu |
| 2026-01-18 | - | Added scrolling wireframes, type-to-filter, values with spaces |
| 2026-01-18 | - | Added positional parameter rules and interaction scenarios |
| 2026-01-18 | - | Added positional parameter support, clarified no-match behavior |
| 2026-01-18 | - | Fixed menu mockups to show alphabetical ordering per FR-055 |
| 2026-01-18 | - | Documented backspace/filter restore, alias behavior, removed timing metrics |
