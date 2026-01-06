# Research: Menu Filtering While Typing

**Date**: 2026-01-03  
**Feature**: 010-menu-filter

## Executive Summary

Research validates that all required infrastructure **already exists** in the codebase. Implementation is primarily a wiring exercise with minimal new code. Key findings:

1. **CompletionMatcher** already supports `MatchMode.ContainsCaseInsensitive` (substring matching)
2. **CompletionItem.MatchRanges** already provides match highlighting data
3. **HandleCharacterWhileMenuOpenAsync()** exists but isn't wired in production InputBuilder
4. **MenuState.FilterText** exists but is never populated

## Existing Infrastructure Analysis

### 1. CompletionMatcher - Substring Matching (READY)

**Location**: `BitPantry.CommandLine/AutoComplete/CompletionMatcher.cs`

**Decision**: Use existing `MatchMode.ContainsCaseInsensitive`  
**Rationale**: Already implemented with match range tracking  
**Alternatives considered**: Implement custom substring matcher (rejected - duplication)

```csharp
// Already exists - just need to change mode from PrefixCaseInsensitive to ContainsCaseInsensitive
public static IEnumerable<CompletionItem> Match(
    IEnumerable<CompletionItem> items,
    string query,
    MatchMode mode = MatchMode.Prefix)
{
    // ... existing implementation handles ContainsCaseInsensitive
}

private static MatchResult MatchContains(CompletionItem item, string query, bool caseSensitive)
{
    var index = text.IndexOf(query, comparison);
    if (index >= 0)
    {
        return MatchResult.Contains(item, index, query.Length, caseSensitive);
    }
    return MatchResult.NoMatch(item);
}
```

### 2. CompletionItem.MatchRanges - Highlighting Data (READY)

**Location**: `BitPantry.CommandLine/AutoComplete/CompletionItem.cs`

**Decision**: Use existing `MatchRanges` property  
**Rationale**: Already populated by CompletionMatcher.Match()  
**Alternatives considered**: None - infrastructure exists

```csharp
// Already exists
public IReadOnlyList<Range> MatchRanges { get; internal set; } = Array.Empty<Range>();
```

### 3. AutoCompleteController.HandleCharacterWhileMenuOpenAsync (READY)

**Location**: `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs` (lines 400-437)

**Decision**: Wire existing method into InputBuilder  
**Rationale**: Fully implemented, tested in StepwiseTestRunner  
**Alternatives considered**: None - just needs wiring

```csharp
// Already exists - complete implementation
public async Task HandleCharacterWhileMenuOpenAsync(Input.ConsoleLineMirror inputLine, char character)
{
    if (_orchestrator == null || !_isEngaged)
        return;

    var action = await _orchestrator.HandleCharacterAsync(
        character, inputLine.Buffer, inputLine.BufferPosition);

    switch (action.Type)
    {
        case CompletionActionType.SelectionChanged:
            ClearMenu(inputLine);
            _currentMenuState = action.MenuState;
            if (_currentMenuState?.Items.Count > 0)
                RenderMenu(inputLine);
            else
                _isEngaged = false;
            break;
        // ...
    }
}
```

### 4. InputBuilder Default Handler - The Wiring Gap (NEEDS FIX)

**Location**: `BitPantry.CommandLine/Input/InputBuilder.cs` (lines 174-193)

**Problem**: Calls `_acCtrl.End()` which closes menu instead of filtering

**Current (broken)**:
```csharp
.AddDefaultHandler(async ctx =>
{
    if (_acCtrl.IsEngaged)
        _acCtrl.End(ctx.InputLine);  // ← CLOSES MENU - BUG!
    
    if (!char.IsControl(ctx.KeyInfo.KeyChar))
    {
        ctx.InputLine.Write(ctx.KeyInfo.KeyChar);
        await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
        return true;
    }
    return false;
})
```

**Required fix**:
```csharp
.AddDefaultHandler(async ctx =>
{
    if (!char.IsControl(ctx.KeyInfo.KeyChar))
    {
        ctx.InputLine.Write(ctx.KeyInfo.KeyChar);
        
        if (_acCtrl.IsEngaged)
        {
            // Filter menu while typing
            await _acCtrl.HandleCharacterWhileMenuOpenAsync(ctx.InputLine, ctx.KeyInfo.KeyChar);
        }
        else
        {
            await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
        }
        return true;
    }
    return false;
})
```

### 5. CompletionOrchestrator.HandleCharacterAsync - Match Mode (NEEDS UPDATE)

**Location**: `BitPantry.CommandLine/AutoComplete/CompletionOrchestrator.cs` (lines 185-215)

**Problem**: Uses `MatchMode.PrefixCaseInsensitive` instead of `MatchMode.ContainsCaseInsensitive`

**Decision**: Change to ContainsCaseInsensitive per spec FR-002  
**Rationale**: Spec requires substring matching, not prefix matching

### 6. MenuState.FilterText - Unused Property (NEEDS WIRING)

**Location**: `BitPantry.CommandLine/AutoComplete/MenuState.cs` (line 23)

**Problem**: Property exists but never set during filtering

**Decision**: Populate when filtering for display purposes  
**Rationale**: Enables "(no matches)" message and potential filter text display

### 7. AutoCompleteMenuRenderable - Match Highlighting (NEEDS UPDATE)

**Location**: `BitPantry.CommandLine/AutoComplete/Rendering/AutoCompleteMenuRenderable.cs`

**Problem**: Renders plain strings, doesn't use MatchRanges for highlighting

**Decision**: Accept CompletionItem[] instead of string[], use MatchRanges with Spectre markup  
**Rationale**: Spec FR-008 requires match highlighting

**Required change**: Render highlighted matches using Spectre styles:
```csharp
// For each item with MatchRanges, apply highlight style to matched portions
// Example: "config" with match "fig" → "con[yellow]fig[/]"
```

## New Code Required

### 1. IsInsideQuotes() Helper (NEW)

**Location**: `BitPantry.CommandLine/StringExtensions.cs`

**Decision**: Simple quote counting (odd count = inside)  
**Rationale**: Consistent with existing parser behavior (no escape handling)

```csharp
public static bool IsInsideQuotes(this string buffer, int position)
{
    int quoteCount = 0;
    for (int i = 0; i < position && i < buffer.Length; i++)
    {
        if (buffer[i] == '"')
            quoteCount++;
    }
    return (quoteCount % 2) == 1;
}
```

### 2. Space Key Handler (NEW)

**Location**: `BitPantry.CommandLine/Input/InputBuilder.cs`

**Decision**: Add specific handler for Space when menu is open  
**Rationale**: Spec FR-006/FR-007 require context-aware space handling

```csharp
.AddCharHandler(' ', async ctx =>
{
    if (_acCtrl.IsEngaged)
    {
        if (ctx.InputLine.Buffer.IsInsideQuotes(ctx.InputLine.BufferPosition))
        {
            // Inside quotes - treat as filter character
            ctx.InputLine.Write(' ');
            await _acCtrl.HandleCharacterWhileMenuOpenAsync(ctx.InputLine, ' ');
        }
        else
        {
            // Outside quotes - close menu, insert space
            _acCtrl.End(ctx.InputLine);
            ctx.InputLine.Write(' ');
            await _acCtrl.UpdateGhostAsync(ctx.InputLine.Buffer, ctx.InputLine.BufferPosition);
        }
        return true;
    }
    return false;  // Let default handler process
})
```

### 3. Menu Trigger Position Tracking (NEW)

**Location**: `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs`

**Decision**: Track buffer position when menu opens  
**Rationale**: Spec FR-005 requires closing menu when backspace goes before trigger point

```csharp
private int _menuTriggerPosition = -1;

// In method that opens menu:
_menuTriggerPosition = inputLine.BufferPosition;

// In backspace handler:
if (position < _menuTriggerPosition)
{
    End(inputLine);  // Close menu
}
```

### 4. "(no matches)" Message Rendering (UPDATE)

**Location**: `BitPantry.CommandLine/AutoComplete/Rendering/AutoCompleteMenuRenderable.cs`

**Decision**: Handle empty items list with message  
**Rationale**: Spec FR-003 requires "(no matches)" message

```csharp
// In Render method:
if (Items.Count == 0)
{
    yield return new Segment("  (no matches)", DimStyle);
    yield return Segment.LineBreak;
    yield break;
}
```

## Test Infrastructure Validation

### StepwiseTestRunner - Fully Capable

The existing `StepwiseTestRunner` provides all assertions needed:

| Assertion | Purpose | Status |
|-----------|---------|--------|
| `HaveMenuVisible()` | Verify menu is open | ✅ Exists |
| `HaveMenuHidden()` | Verify menu is closed | ✅ Exists |
| `HaveMenuItemCount(n)` | Verify filter reduced items | ✅ Exists |
| `HaveBuffer(text)` | Verify filter text in buffer | ✅ Exists |
| `HaveSelectedMenuItem(text)` | Verify selection | ✅ Exists |
| `HaveMenuSelectedIndex(i)` | Verify selection resets | ✅ Exists |

### Existing Filter Tests (Partial)

Tests already exist in `MenuBehaviorTests.cs` that demonstrate filtering works in StepwiseTestRunner:
- `TypingWhileMenuOpen_ShouldFilterMenuResults()` - basic filter test
- These tests pass because StepwiseTestRunner has correct wiring

**Gap**: Production InputBuilder doesn't have the same wiring.

## Trailing Space Removal (UPDATE)

**Location**: `BitPantry.CommandLine/AutoComplete/AutoCompleteController.cs`

**Problem**: `InsertCompletion()` always appends trailing space

**Decision**: Remove trailing space to match ghost text behavior  
**Rationale**: Spec FR-009 requires no trailing space

```csharp
// Current:
inputLine.Write(completionText + " ");

// Required:
inputLine.Write(completionText);
```

## Summary of Changes

| File | Change Type | Description |
|------|-------------|-------------|
| `InputBuilder.cs` | UPDATE | Wire HandleCharacterWhileMenuOpenAsync in default handler |
| `InputBuilder.cs` | ADD | Space key handler with quote detection |
| `StringExtensions.cs` | ADD | IsInsideQuotes() helper method |
| `CompletionOrchestrator.cs` | UPDATE | Use ContainsCaseInsensitive match mode |
| `CompletionOrchestrator.cs` | UPDATE | Populate MenuState.FilterText |
| `AutoCompleteController.cs` | ADD | Track menu trigger position |
| `AutoCompleteController.cs` | UPDATE | Remove trailing space from InsertCompletion |
| `AutoCompleteMenuRenderable.cs` | UPDATE | Handle empty items with "(no matches)" |
| `AutoCompleteMenuRenderable.cs` | UPDATE | Add match highlighting using MatchRanges |
| `MenuFilteringTests.cs` | NEW | Dedicated filtering test class |

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaking existing tests | Low | High | Run full test suite after each change |
| Performance with large lists | Low | Medium | Substring search is O(n*m), acceptable for 100 items |
| Quote detection edge cases | Medium | Low | Simple odd/even count matches parser behavior |
| Backspace position tracking | Medium | Medium | Clear tests for trigger position boundary |

## Conclusion

Implementation is **low risk** and **moderate effort**. Most code exists; primary work is:
1. Wiring InputBuilder default handler correctly
2. Adding Space key context-aware handler
3. Updating AutoCompleteMenuRenderable for highlighting and "(no matches)"
4. Removing trailing space from completion insertion
5. Writing comprehensive TDD tests
