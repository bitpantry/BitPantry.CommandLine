# Data Model: Menu Filtering While Typing

**Date**: 2026-01-03  
**Feature**: 010-menu-filter

## Entity Updates

### MenuState (Existing - Minor Update)

**Location**: `BitPantry.CommandLine/AutoComplete/MenuState.cs`

| Property | Type | Status | Notes |
|----------|------|--------|-------|
| Items | `IReadOnlyList<CompletionItem>` | Exists | Filtered items list |
| SelectedIndex | `int` | Exists | Current selection |
| **FilterText** | `string` | **Wire** | Currently unused - populate during filtering |
| IsLoading | `bool` | Exists | Loading state |
| ErrorMessage | `string?` | Exists | Could use for no-matches |
| TotalCount | `int` | Exists | Pre-filter total |
| ViewportStart | `int` | Exists | Scroll position |
| ViewportSize | `int` | Exists | Visible items count |

**Change Required**: Populate `FilterText` when filtering is applied.

### CompletionItem (Existing - No Changes)

**Location**: `BitPantry.CommandLine/AutoComplete/CompletionItem.cs`

| Property | Type | Status | Notes |
|----------|------|--------|-------|
| InsertText | `string` | Exists | Text to insert |
| DisplayText | `string` | Exists | Menu display text |
| Description | `string?` | Exists | Optional description |
| Kind | `CompletionItemKind` | Exists | Item type |
| SortPriority | `int` | Exists | Sort order |
| MatchScore | `int` | Exists | Set by matcher |
| **MatchRanges** | `IReadOnlyList<Range>` | **Use** | Currently set but not used for rendering |

**Change Required**: Use `MatchRanges` in `AutoCompleteMenuRenderable` for highlighting.

### AutoCompleteController (Existing - Add Property)

**Location**: `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs`

| Property | Type | Status | Notes |
|----------|------|--------|-------|
| IsEngaged | `bool` | Exists | Menu is open |
| **_menuTriggerPosition** | `int` | **Add** | Buffer position when menu opened |

**Change Required**: Track trigger position for FR-005 (close on backspace past trigger).

### AutoCompleteMenuRenderable (Existing - Signature Change)

**Location**: `BitPantry.CommandLine/AutoComplete/Rendering/AutoCompleteMenuRenderable.cs`

**Current Constructor**:
```csharp
public AutoCompleteMenuRenderable(
    IReadOnlyList<string> items,
    int selectedIndex,
    int viewportStart,
    int viewportSize)
```

**Updated Constructor**:
```csharp
public AutoCompleteMenuRenderable(
    IReadOnlyList<CompletionItem> items,  // Changed from string to CompletionItem
    int selectedIndex,
    int viewportStart,
    int viewportSize,
    string? filterText = null)  // Added for "(no matches)" display
```

**New Properties**:

| Property | Type | Notes |
|----------|------|-------|
| Items | `IReadOnlyList<CompletionItem>` | Changed from string |
| FilterText | `string?` | For no-matches context |

**Rendering Changes**:
1. Handle empty items with "(no matches)" message
2. Apply highlight style to MatchRanges portions

## New Helper Method

### StringExtensions.IsInsideQuotes (New)

**Location**: `BitPantry.CommandLine/StringExtensions.cs`

```csharp
/// <summary>
/// Determines if the specified position in the buffer is inside a quoted string.
/// Uses simple quote counting (odd count = inside).
/// </summary>
/// <param name="buffer">The input buffer text.</param>
/// <param name="position">The cursor position to check.</param>
/// <returns>True if position is inside quotes, false otherwise.</returns>
public static bool IsInsideQuotes(this string buffer, int position)
```

**Behavior**:
- Count unescaped `"` characters before `position`
- Odd count → inside quotes → return true
- Even count → outside quotes → return false
- No escape sequence handling (consistent with parser)

## State Transitions

### Menu Filter State Machine

```
┌─────────────────┐
│  Menu Closed    │
└────────┬────────┘
         │ Tab (with matches)
         ▼
┌─────────────────┐
│  Menu Open      │◄──────────────────┐
│  FilterText=""  │                   │
└────────┬────────┘                   │
         │                            │
    ┌────┴────┬───────────┐          │
    │         │           │          │
    │ Char    │ Space     │ Backspace│
    │         │ (not in   │ (to      │
    │         │  quotes)  │  trigger)│
    ▼         ▼           ▼          │
┌─────────┐ ┌───────┐ ┌─────────┐    │
│ Filter  │ │ Close │ │  Close  │    │
│ Updated │ │ Menu  │ │  Menu   │    │
└────┬────┘ └───────┘ └─────────┘    │
     │                               │
     │ (matches > 0)                 │
     └───────────────────────────────┘
     │
     │ (matches == 0)
     ▼
┌─────────────────┐
│  No Matches     │
│  "(no matches)" │
└────────┬────────┘
         │ Backspace
         ▼
┌─────────────────┐
│  Menu Open      │
│  (re-filtered)  │
└─────────────────┘
```

### Filter Text Lifecycle

1. **Menu Opens**: `FilterText = ""`, `TriggerPosition = BufferPosition`
2. **Character Typed**: `FilterText += char`, re-filter items
3. **Backspace**: `FilterText = FilterText[..^1]`, re-filter items
4. **Space (outside quotes)**: Close menu, `FilterText` discarded
5. **Enter/Tab Accept**: Close menu, `FilterText` discarded
6. **Escape**: Close menu, `FilterText` discarded

## Validation Rules

### Filter Input Validation

| Rule | Validation |
|------|------------|
| Filter character | Any printable character (not `char.IsControl`) |
| Space inside quotes | Treated as filter character |
| Space outside quotes | Closes menu, not a filter character |
| Backspace with filter | Remove last filter char, keep menu open |
| Backspace without filter | Close menu if at trigger position |

### Match Highlighting Rules

| Rule | Behavior |
|------|----------|
| Single match range | Highlight contiguous matched substring |
| Multiple ranges | Highlight each range (for future fuzzy) |
| No filter | No highlighting (all items shown) |
| Case differences | Match case-insensitively, display original case |

## Acceptance Completion Changes

### InsertCompletion Method

**Current Behavior**:
```csharp
inputLine.Write(completionText + " ");  // Trailing space
```

**New Behavior**:
```csharp
inputLine.Write(completionText);  // No trailing space
```

**Affected Scenarios**:
- Menu selection with Enter
- Tab completion with single match
- Ghost text acceptance (already no trailing space - for consistency)

## Performance Considerations

| Operation | Complexity | Expected Performance |
|-----------|------------|---------------------|
| Substring match | O(n * m) | <1ms for 100 items |
| Filter on keystroke | O(n) | <5ms |
| Render highlighted | O(items * ranges) | <1ms |
| Quote detection | O(position) | <0.1ms |

All operations well under 50ms target (SC-003).
