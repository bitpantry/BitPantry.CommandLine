# Implementation Plan: Input Syntax Highlighting

**Branch**: `010-input-syntax-highlight` | **Date**: 2026-02-05 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/010-input-syntax-highlight/spec.md`

## Summary

Real-time syntax highlighting for command line input that colorizes typed text as the user types. Colors update dynamically with each keystroke based on semantic token classification (group, command, argument name/alias, argument value). The system integrates alongside the existing autocomplete system, sharing parsing infrastructure.

## Technical Context

**Language/Version**: C# / .NET 8.0  
**Primary Dependencies**: Spectre.Console (console rendering with ANSI color support)  
**Storage**: N/A (in-memory only)  
**Testing**: MSTest with FluentAssertions, Moq  
**Target Platform**: Cross-platform console applications (Windows, Linux, macOS)
**Project Type**: Single solution with multiple projects  
**Performance Goals**: N/A (removed from spec)  
**Constraints**: Must not break existing autocomplete functionality  
**Scale/Scope**: Single input line highlighting

## Research Findings

### Q1: Can the current infrastructure support syntax highlighting?

**Decision**: Partially - enhancement required

**What exists:**
- `CursorContextType` enum already classifies element types: `GroupOrCommand`, `CommandOrSubgroupInGroup`, `ArgumentName`, `ArgumentAlias`, `ArgumentValue`, `PositionalValue`
- `CursorContextResolver` determines semantic context at cursor position
- `ParsedCommandElement.CommandElementType` classifies tokens during parsing
- Comprehensive tests in `CursorContextResolverTests.cs` (1154 lines)

**What's missing:**
1. **All-token classification**: `CursorContextResolver` only classifies the element AT cursor. Syntax highlighting needs to classify ALL tokens in the line
2. **Unique match detection**: Need to determine if a partial (e.g., "ser") uniquely matches a group/command or is ambiguous
3. **Color centralization**: Colors are scattered across `AutoCompleteMenuRenderer.cs` (`Style.Parse("cyan")`) and `GhostTextController.cs` (`[dim]` markup)
4. **Styled input rendering**: `ConsoleLineMirror` only writes plain text

### Q2: Does the parsing know element types for all scenarios?

**Decision**: Yes - `CommandElementType` covers all:
- `Command` - command name token
- `ArgumentName` - `--name` tokens
- `ArgumentAlias` - `-n` tokens  
- `ArgumentValue` - values after arguments
- `PositionalValue` - positional parameter values
- `Empty` - whitespace
- `Unexpected` - unrecognized tokens

**Gap for highlighting**: Parsing identifies WHAT a token is, but not WHETHER it uniquely resolves. For example, "ser" is parsed as a `Command` element, but we need to know:
- Does "ser" uniquely match "server" (group) → cyan
- Does "ser" match both "server" (group) and "service" (command) → default (ambiguous)

### Q3: Color centralization approach

**Decision**: Create `SyntaxColorScheme` class

**Current scattered locations:**
- `AutoCompleteMenuRenderer.cs` line 27: `Style.Parse("invert")`
- `AutoCompleteMenuRenderer.cs` line 32: `Style.Parse("cyan")` 
- `GhostTextController.cs` line 108: `[dim]` markup

**New approach:**
```csharp
public static class SyntaxColorScheme
{
    public static Style Group { get; } = Style.Parse("cyan");
    public static Style Command { get; } = Style.Parse("default");
    public static Style ArgumentName { get; } = Style.Parse("yellow");
    public static Style ArgumentAlias { get; } = Style.Parse("yellow");
    public static Style ArgumentValue { get; } = Style.Parse("purple");
    public static Style GhostText { get; } = Style.Parse("dim");
    public static Style MenuHighlight { get; } = Style.Parse("invert");
    public static Style MenuGroup { get; } = Style.Parse("cyan");
    public static Style Default { get; } = Style.Parse("default");
}
```

### Q4: Testing Infrastructure Gap Analysis (CRITICAL)

**Decision**: Extend `VirtualConsoleAssertions` with color-aware assertions (PREREQUISITE)

**Current state in `BitPantry.VirtualConsole.Testing`:**
- `CellStyle` struct correctly tracks colors: `ForegroundColor`, `BackgroundColor`, `Foreground256`, `ForegroundRgb`
- `VirtualConsoleAssertions.cs` has assertions for:
  - `HaveCellWithStyle(row, col, CellAttributes)` - checks ATTRIBUTES only (dim, bold, reverse)
  - `HaveRangeWithStyle(row, startCol, length, CellAttributes)` - checks ATTRIBUTES only
  - `HaveDimCellAt`, `HaveReverseCellAt` - attribute helpers
  - `ContainText`, `HaveTextAt`, `HaveCursorAt` - content/position

**GAP (Blocker for validation):**
- **NO assertions for foreground/background COLOR values**
- Cannot write tests that validate "server" displays in **cyan** (FR-001)
- Cannot validate "--host" displays in **yellow** (FR-003)
- Cannot validate "localhost" displays in **purple** (FR-004)

**Required new assertions:**
```csharp
// Check single cell foreground color
HaveCellWithForegroundColor(int row, int col, ConsoleColor? expected)

// Check range of cells have same foreground color
HaveRangeWithForegroundColor(int row, int startCol, int length, ConsoleColor? expected)

// Check 256-color palette
HaveCellWithForeground256(int row, int col, byte expected)
HaveRangeWithForeground256(int row, int startCol, int length, byte expected)

// Full style comparison (color + attributes)
HaveCellWithFullStyle(int row, int col, CellStyle expected)
```

**Impact**: This is a **prerequisite task** - must be completed before syntax highlighting integration tests can be written.

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| TDD (tests first) | ✅ PASS | Will write failing tests before implementation |
| Dependency Injection | ✅ PASS | New components use constructor injection |
| Follow Existing Patterns | ✅ PASS | Follows AutoComplete architecture patterns |
| Security | ✅ N/A | No security implications |
| Integration Tests | ✅ PASS | Will test integration with InputBuilder |

## Project Structure

### Documentation (this feature)

```text
specs/010-input-syntax-highlight/
├── spec.md              # Feature specification
├── plan.md              # This file
├── research.md          # Phase 0 output (inline above)
├── test-cases.md        # Test case definitions
└── tasks.md             # Implementation tasks
```

### Source Code (affected files)

```text
BitPantry.CommandLine/
├── AutoComplete/
│   ├── SyntaxColorScheme.cs           # NEW: Centralized color definitions
│   ├── GhostTextController.cs         # MODIFY: Use SyntaxColorScheme
│   ├── Rendering/
│   │   └── AutoCompleteMenuRenderer.cs # MODIFY: Use SyntaxColorScheme
│   └── Context/
│       └── CursorContextResolver.cs    # EXISTING: Provides context types
├── Input/
│   ├── InputBuilder.cs                 # MODIFY: Add highlighting hook
│   ├── ConsoleLineMirror.cs            # MODIFY: Support styled re-render
│   └── SyntaxHighlighter.cs            # NEW: Main highlighting component
└── Processing/
    └── Parsing/
        └── ParsedCommandElement.cs     # EXISTING: Provides element types

BitPantry.CommandLine.Tests/
└── Input/
    └── SyntaxHighlighterTests.cs       # NEW: Unit tests

BitPantry.VirtualConsole.Testing/
└── VirtualConsoleAssertions.cs         # MODIFY: Add color assertions (PREREQUISITE)
```

## Technical Design

### Component Architecture

```
InputBuilder (orchestrates)
    ├── SyntaxHighlighter (NEW)
    │     ├── Classifies all tokens with color info
    │     ├── Uses CursorContextResolver for context
    │     ├── Uses ICommandRegistry for match lookup
    │     └── Returns List<StyledSegment>
    ├── ConsoleLineMirror (MODIFIED)
    │     └── RenderWithStyles(List<StyledSegment>) - NEW method
    └── AutoCompleteController (EXISTING)
          └── Shows ghost text/menu AFTER highlighted text
```

### Key Classes

**1. SyntaxColorScheme (NEW)**
- Static class with `Style` properties for each token type
- Used by SyntaxHighlighter, AutoCompleteMenuRenderer, GhostTextController
- Enables future theming capability

**2. StyledSegment (NEW)**
```csharp
public record StyledSegment(string Text, int Start, int End, Style Style);
```

**3. SyntaxHighlighter (NEW)**
- `Highlight(string input) → List<StyledSegment>`
- Uses `ParsedInput` to tokenize
- For each token, determines:
  - Its `CommandElementType` from parsing
  - Whether it uniquely resolves (for groups/commands)
- Maps to appropriate `SyntaxColorScheme` style

**4. TokenMatchResolver (NEW helper)**
- `ResolveTokenMatch(string partial, GroupInfo? currentGroup) → MatchResult`
- Returns: `UniqueGroup`, `UniqueCommand`, `Ambiguous`, `NoMatch`
- Used to determine if partial text should be cyan vs default

### Integration Flow

```
User types keystroke
    ↓
InputBuilder.OnKeyPressed
    ↓
SyntaxHighlighter.Highlight(line.Buffer)
    ↓
ConsoleLineMirror.RenderWithStyles(segments)
    ↓
AutoCompleteController.UpdateAsync(line)
    ↓
GhostTextController.Show() or MenuController.Show()
```

### ConsoleLineMirror Modification

Current `Write()` appends plain text. New approach:

```csharp
public void RenderWithStyles(List<StyledSegment> segments, int cursorPosition)
{
    // 1. Move cursor to start of line (after prompt)
    // 2. Clear line
    // 3. Write each segment with its style
    // 4. Restore cursor to cursorPosition
}
```

## Rationale

**Why separate SyntaxHighlighter vs extending CursorContextResolver?**
- Single Responsibility: Context resolver focuses on cursor position, highlighter on full-line classification
- Different concerns: Autocomplete needs "what can I type next?", highlighting needs "what color is each token?"
- Testability: Can test highlighting in isolation

**Why centralized SyntaxColorScheme?**
- DRY: Colors defined once
- Maintainability: Easy to change colors
- Future theming: Can extend to support custom themes
- Consistency: Same colors used across menu, ghost text, and input highlighting

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| [e.g., 4th project] | [current need] | [why 3 projects insufficient] |
| [e.g., Repository pattern] | [specific problem] | [why direct DB access insufficient] |
