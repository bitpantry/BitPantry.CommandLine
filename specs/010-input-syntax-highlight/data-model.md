# Data Model: Input Syntax Highlighting

**Feature**: 010-input-syntax-highlight  
**Date**: 2026-02-05

## Entities

### ColoredSegment

Represents a segment of text with associated styling.

| Field | Type | Description |
|-------|------|-------------|
| Text | string | The text content of the segment |
| Start | int | Start position in input (0-based) |
| End | int | End position in input (0-based, exclusive) |
| Style | Spectre.Console.Style | Color/style to apply |

**Notes**: Immutable record type. Created by SyntaxHighlighter, consumed by rendering.

### TokenMatchResult

Result of attempting to match partial text against registry.

| Value | Description |
|-------|-------------|
| UniqueGroup | Text uniquely matches exactly one group |
| UniqueCommand | Text uniquely matches exactly one command |
| Ambiguous | Text matches multiple items or mixed types |
| NoMatch | Text doesn't match any registered group/command |

**Notes**: Enum type used by TokenMatchResolver.

### SyntaxColorScheme

Static mapping of semantic token types to Spectre.Console styles.

| Property | Style | Use Case |
|----------|-------|----------|
| Group | cyan | Group names (resolved or uniquely partial) |
| Command | default | Command names |
| ArgumentName | yellow | --name style arguments |
| ArgumentAlias | yellow | -a style aliases |
| ArgumentValue | purple | Values after arguments |
| PositionalValue | purple | Positional parameter values |
| GhostText | dim | Autocomplete ghost text |
| MenuHighlight | invert | Selected menu item |
| MenuGroup | cyan | Group items in menu |
| Default | default | Unrecognized/ambiguous text |

**Notes**: Static class, all properties are `Style` objects.

## State Transitions

### Input Line Highlighting State

```
Empty → Typing (keystroke)
Typing → Typing (keystroke) [re-highlight full line]
Typing → Editing (backspace/cursor move) [re-highlight full line]
Editing → Typing (keystroke) [re-highlight full line]
Any → Submitted (Enter)
```

**Note**: Each keystroke triggers full line re-highlight. No incremental state.

## Relationships

```
InputBuilder
    ├── uses → SyntaxHighlighter
    │             ├── uses → ParsedInput (existing)
    │             ├── uses → TokenMatchResolver
    │             │             └── uses → ICommandRegistry
    │             └── produces → List<ColoredSegment>
    ├── uses → ConsoleLineMirror
    │             └── uses → List<ColoredSegment> (RenderWithStyles)
    └── uses → AutoCompleteController (existing)
                  └── uses → SyntaxColorScheme (for consistency)
```

## Validation Rules

- ColoredSegment.Start must be >= 0
- ColoredSegment.End must be > Start
- ColoredSegment.Text.Length must equal (End - Start)
- Segments must not overlap
- Segments should cover entire input (no gaps, use Default style for whitespace)
