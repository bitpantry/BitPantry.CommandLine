# Research: Input Syntax Highlighting

**Feature**: 010-input-syntax-highlight  
**Date**: 2026-02-05

## Research Tasks

### R1: Can current parsing infrastructure support all-token classification?

**Decision**: Yes - extend existing infrastructure

**Rationale**: 
- `ParsedInput` already tokenizes input into `ParsedCommandElement` objects
- Each element has `CommandElementType` (Command, ArgumentName, ArgumentAlias, ArgumentValue, PositionalValue, etc.)
- Each element has `StartPosition` and `EndPosition` for segment boundaries
- `CursorContextResolver` shows pattern for resolving groups/commands from registry

**Alternatives considered**:
- Build separate tokenizer for highlighting: Rejected - duplication, inconsistency risk
- Modify CursorContextResolver to return all contexts: Rejected - different responsibility

### R2: How to detect unique vs ambiguous partial matches?

**Decision**: Create `TokenMatchResolver` helper class

**Rationale**:
- Need to query registry for all groups/commands starting with partial text
- If exactly one match of type Group → UniqueGroup (cyan)
- If exactly one match of type Command → UniqueCommand (default)
- If multiple matches or mixed types → Ambiguous (default)
- If no matches → NoMatch (default)

**Alternatives considered**:
- Inline logic in SyntaxHighlighter: Rejected - complex, hard to test
- Extend ICommandRegistry: Rejected - registry is read-only data, not logic

### R3: How to render colored text to console?

**Decision**: Extend ConsoleLineMirror with `RenderWithStyles()` method

**Rationale**:
- ConsoleLineMirror already manages cursor position and buffer sync
- Spectre.Console supports styled text via `WriteAnsi()` or markup
- Re-render entire line on each keystroke (simple, reliable)
- Save cursor position, write styled segments, restore cursor

**Alternatives considered**:
- Direct ANSI codes to console: Rejected - Spectre.Console already abstracts this
- Differential updates (only changed chars): Rejected - complexity not justified for input line length

### R4: Color scheme approach

**Decision**: Static `SyntaxColorScheme` class with `Style` properties

**Rationale**:
- Single source of truth for all colors
- Used by SyntaxHighlighter, AutoCompleteMenuRenderer, GhostTextController
- Spectre.Console `Style` objects are immutable and reusable
- Easy to extend for theming in future

**Alternatives considered**:
- Configuration file: Rejected - over-engineering for v1
- DI-injected service: Rejected - colors are static, no runtime variation needed
